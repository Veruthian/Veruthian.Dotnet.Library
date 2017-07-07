using System;
using Soedeum.Dotnet.Library.Numerics;

namespace Soedeum.Dotnet.Library.Text.Encodings
{
    public static class Utf8
    {
        // Leading and Trailing Code Units
        // Encoding of Utf8 CodePoints
        // 1) U+0000  to U+007F (ASCII)             -> [0] 0xxx-xxxx;
        // 2) U+0080  to U+07FF                     -> [0] 110y-yyyy; [1]: 10xx-xxxx
        // 3) U+0800  to U+D7FF, U+E000 to U+FFFF   -> [0] 1110-zzzz; [1]: 10yy-yyyy; [2]: 10xx-xxxx
        // 4) U+10000 to U+10FFFF                   -> [0] 1111-0uuu; [1]: 10zz-zzzz; [2]: 10xx-xxxx; [3]: 10xx-xxxx
        //
        // Converted
        // 1) 0000-0000 0000-0000 0xxx-xxxx
        // 2) 0000-0000 0000-yyyy yyxx-xxxx
        // 3) 0000-00zz zzzz-yyyy yyxx-xxxx
        // 4) 000u-uuzz zzzy-yyyy yyxx-xxxx
        public const byte OneByteSequencePrefix = 0b0000_0000;
        public const byte TwoByteSequencePrefix = 0b1100_0000;
        public const byte ThreeByteSequencePrefix = 0b1110_0000;
        public const byte FourByteSequencePrefix = 0b1111_0000;

        public const byte TrailingUnitPrefix = 0b1000_0000;

        private const int TrailingUnitOffset = 6;


        public struct ByteEncoder : ITransformer<CodePoint, Bits64>
        {
            public bool TryProcess(CodePoint value, out Bits64 result)
            {
                throw new NotImplementedException();
            }
        }

        public struct ByteDecoder : ITransformer<byte, uint>
        {
            uint state;

            int bytesRemaining;


            public bool TryProcess(byte value, out uint result)
            {

                if (bytesRemaining == 0)
                {
                    ProcessLeading(value);
                }
                else
                {
                    ProcessTrailing(value);
                }

                if (bytesRemaining == 0)
                {
                    result = state;

                    state = 0;

                    return true;
                }
                else
                {
                    result = default(uint);

                    return false;
                }
            }

            // Heading

            // Use 32 bit state to discover amount
            static readonly uint[] masks = new uint[4] { 0b0111_1111, 0b0001_1111, 0b0000_1111, 0b0000_0111 };

            static readonly int[] sequenceLengths = new int[32]{
            // 0xxxx = 1  Byte Sequence (0000-0..0111-1)
                0, 0, 0, 0,
                0, 0, 0, 0,
                0, 0, 0, 0,
                0, 0, 0, 0,

            // 10xxx = Invalid Sequence (1000-0..10111)
                -1, -1, -1, -1,
                -1, -1, -1, -1,

            // 110xx = 2  Byte Sequence (1100-0..1101-1)
                1, 1, 1, 1,

            // 1110x = 3  Byte Sequence (1110-0..1110-1)
                2, 2, 

            // 11110 = 4  Byte Sequence (1111-0)
                3,

            // 11111 = Invalid Sequence (1111-1)
                -1
            };

            private void ProcessLeading(byte value)
            {
                // 0xxx-xxxx, 110x-xxxx, 1110-xxxx, 1111-0xxx

                // First 5 bits tell the sequence length
                int length = sequenceLengths[(value >> 3)];

                if (length == -1)
                    throw InvalidCodeUnitSequence(value);

                uint mask = masks[length];

                state = value & mask;

                bytesRemaining = length;
            }

            // Extension

            private const byte TrailingHeaderMask = 0b1100_0000;

            private const byte TrailingMask = 0b0011_1111;


            private void ProcessTrailing(byte value)
            {
                // Prefix MUST be 0b10xx_xxxx
                if ((value & TrailingHeaderMask) != TrailingUnitPrefix)
                {
                    Reset();

                    throw InvalidCodeUnitSequence(value);
                }

                uint bits = (uint)(value & TrailingMask);

                // Bits cannot be 0 (Utf8 must be encoded in smallest possible form)
                // if (bits == 0)
                // {
                //     Reset();

                //     throw CodePointNotSmallestSequence(value);
                // }

                // shift old bits down
                state <<= TrailingUnitOffset;

                state |= bits;

                bytesRemaining--;
            }

            private void Reset()
            {
                state = 0;

                bytesRemaining = 0;
            }

            private Exception CodePointNotSmallestSequence(byte value)
            {
                string message = "Invalid UTF8 code unit (0:X4), CodePoint must be represented in the smallest possible byte sequence.";
                return new InvalidCodePointException(string.Format(message, value.ToString()));
            }

            private static Exception InvalidCodeUnitSequence(byte value)
            {
                string message = "Invalid UTF8 code unit sequence (0:X4), invalid prefix.";
                return new InvalidCodePointException(string.Format(message, value.ToString()));
            }
        }
    }
}