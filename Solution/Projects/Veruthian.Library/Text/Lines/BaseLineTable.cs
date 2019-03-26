using System;
using System.Collections;
using System.Collections.Generic;
using Veruthian.Library.Text.Encodings;
using Veruthian.Library.Text.Runes;

namespace Veruthian.Library.Text.Lines
{
    public abstract class BaseLineTable<I, S> : IEnumerable<(int LineNumber, int Position, int Length, LineEnding Ending)>
        where S : IEnumerable<I>
    {
        private struct LineSegment
        {
            public int LineNumber;

            public int Position;

            public int Length;

            public LineEnding Ending;


            public LineSegment(int lineNumber, int position, int length, LineEnding ending)
            {
                this.LineNumber = lineNumber;
                this.Position = position;
                this.Length = length;
                this.Ending = ending;
            }

            public (int LineNumber, int Position, int Length, LineEnding Ending) ToTuple()
            {
                return (LineNumber, Position, Length, Ending);
            }



            public static implicit operator (int LineNumber, int Position, int Length, LineEnding Ending) (LineSegment value)
            {
                return value.ToTuple();
            }

            public static implicit operator LineSegment((int LineNumber, int Position, int Length, LineEnding Ending) value)
            {
                return new LineSegment(value.LineNumber, value.Position, value.Length, value.Ending);
            }


            public override string ToString()
            {
                return $"{nameof(LineNumber)}: {LineNumber}; {nameof(Position)}: {Position}; {nameof(Length)}: {Length}; {nameof(Ending)}: {Ending};";
            }
        }


        List<LineSegment> segments;

        int length;

        LineEnding endingType;


        public BaseLineTable() : this(LineEnding.None) { }

        public BaseLineTable(LineEnding endingType)
        {
            this.segments = new List<LineSegment>();

            this.segments.Add((0, 0, 0, LineEnding.None));

            this.length = 0;

            this.endingType = endingType;
        }


        public int Count => segments[segments.Count - 1].LineNumber + 1;


        public LineEnding EndingType => endingType;


        public (int LineNumber, int Position, int Length, LineEnding Ending) GetLine(int lineNumber)
        {
            if (lineNumber < 0 || lineNumber >= Count)
                throw new ArgumentOutOfRangeException(nameof(lineNumber));

            if (endingType != LineEnding.CrLf)
            {
                var line = this.segments[lineNumber];

                return line;
            }
            else
            {
                var lineIndex = GetIndexFromNumber(lineNumber);

                var segments = GetLineSegments(lineIndex, lineNumber);

                return JoinSegments(segments);
            }
        }

        public (int LineNumber, int Position, int Length, LineEnding Ending) GetLineFromPosition(int position)
        {
            if (position < 0 && position > length)
                throw new ArgumentOutOfRangeException(nameof(position));

            if (endingType != LineEnding.CrLf)
            {
                return segments[GetLineNumber(position)];
            }
            else
            {
                var lineIndex = GetIndexFromPosition(position);

                var lineNumber = this.segments[lineIndex].LineNumber;

                var segments = GetLineSegments(lineIndex, lineNumber);

                return JoinSegments(segments);
            }
        }

        public TextLocation GetTextLocation(int position)
        {
            var line = GetLineFromPosition(position);

            return new TextLocation(position, line.LineNumber, position - line.Position);
        }

        public int GetLineNumber(int position)
        {
            var index = GetIndexFromPosition(position);

            return index != -1 ? segments[index].LineNumber : -1;
        }


        private LineSegment JoinSegments((int first, int last) segments)
        {
            var segment = this.segments[segments.first];

            for (int i = segments.first + 1; i <= segments.last; i++)
            {
                var nextSegment = this.segments[i];

                segment = (segment.LineNumber, segment.Position, segment.Length + nextSegment.Length, nextSegment.Ending);
            }

            return segment;
        }

        private (int first, int last) GetLineSegments(int lineIndex, int lineNumber)
        {
            int first = lineIndex;

            int last = lineIndex;

            for (int i = lineIndex; i >= 0; i--)
            {
                if (segments[i].LineNumber == lineNumber)
                    first = i;
                else
                    break;
            }
            for (int i = lineIndex; i < segments.Count; i++)
            {
                if (segments[i].LineNumber == lineNumber)
                    last = i;
                else
                    break;
            }

            return (first, last);
        }

        private int GetIndexFromNumber(int lineNumber)
        {
            var low = 0;

            var high = segments.Count - 1;

            while (low <= high)
            {
                var middle = (low + high) / 2;

                var line = segments[middle];

                if (lineNumber < line.LineNumber)
                    high = middle - 1;
                else if (lineNumber >= line.LineNumber)
                    low = middle + 1;
                else
                    return middle;
            }

            return -1;
        }

        private int GetIndexFromPosition(int position)
        {
            var low = 0;

            var high = segments.Count - 1;

            while (low <= high)
            {
                var middle = (low + high) / 2;

                var line = segments[middle];

                if (position < line.Position)
                    high = middle - 1;
                else if (position >= line.Position + line.Length)
                    low = middle + 1;
                else
                    return middle;
            }

            return -1;
        }


        private void Adjust(int lineOffset, int positionOffset, int index)
        {
            for (int i = index; i < segments.Count; i++)
            {
                var line = segments[i];

                line.Position += positionOffset;

                line.LineNumber += lineOffset;

                segments[i] = line;
            }
        }


        // Append
        public void Append(I value) => Append(null, value);

        public void Append(IEnumerable<I> values) => Append(values, default(I));

        private void Append(IEnumerable<I> values, I value)
        {
            var segment = segments[segments.Count - 1];

            var added = 0;

            void UpdateSegment()
            {
                segments[segments.Count - 1] = segment;

                if (segment.Ending != LineEnding.None)
                {
                    var isNewLine = (endingType != LineEnding.CrLf || segment.Ending == LineEnding.CrLf ? 1 : 0);

                    var lineNumber = segment.LineNumber + isNewLine;

                    segment = new LineSegment(lineNumber, segment.Position + segment.Length, 0, LineEnding.None);

                    segments.Add(segment);
                }
            }

            void ProcessItem(I item)
            {
                var utf32 = ConvertToUtf32(item);

                // Cr
                if (endingType != LineEnding.Lf && LineEnding.IsCarriageReturn(utf32))
                {
                    if (segment.Ending != LineEnding.None)
                        UpdateSegment();

                    segment.Ending = LineEnding.Cr;

                    segment.Length++;
                }
                // Lf
                else if (endingType != LineEnding.Cr && LineEnding.IsLineFeed(utf32))
                {
                    // If allows CrLf
                    if (endingType != LineEnding.Lf)
                    {
                        // If <...Lf> + <Cr>
                        if (segment.Ending == LineEnding.Cr)
                        {
                            segment.Length++;

                            segment.Ending = LineEnding.CrLf;
                        }
                        // On <...Lf> + <Cr> when first character of entire append is Cr
                        else if (segment.Length == 0 && segments.Count > 1 && segments[segments.Count - 2].Ending == LineEnding.Cr)
                        {
                            var lastsegment = segments[segments.Count - 2];

                            lastsegment.Length++;

                            lastsegment.Ending = LineEnding.CrLf;

                            segments[segments.Count - 2] = lastsegment;

                            segment.Position++;
                        }
                        // Lf
                        else
                        {
                            if (segment.Ending != LineEnding.None)
                                UpdateSegment();

                            segment.Ending = LineEnding.Lf;

                            segment.Length++;
                        }
                    }
                    // Lf
                    else
                    {
                        if (segment.Ending != LineEnding.None)
                            UpdateSegment();

                        segment.Ending = LineEnding.Lf;

                        segment.Length++;
                    }
                }
                // None
                else
                {
                    if (segment.Ending != LineEnding.None)
                        UpdateSegment();

                    segment.Length++;
                }

                added++;
            }

            if (values != null)
                foreach (var item in values)
                    ProcessItem(item);
            else
                ProcessItem(value);

            UpdateSegment();

            length += added;
        }


        // Prepend
        public void Prepend(I value) => Prepend(null, value);

        public void Prepend(IEnumerable<I> values) => Prepend(values, default(I));

        private void Prepend(IEnumerable<I> values, I value)
        {
            if (length == 0)
                Append(values, value);
            else
                Insert(0, 0, values, value);
        }


        // Insert
        public void Insert(int position, I value) => RoutedInsert(position, null, value);

        public void Insert(int position, IEnumerable<I> values) => RoutedInsert(position, values, default(I));

        private void RoutedInsert(int position, IEnumerable<I> values, I value)
        {
            if (position == length)
                Append(values, value);
            else if (position == 0)
                Prepend(values, value);
            else if (position < 0 || position > length)
                throw new ArgumentOutOfRangeException(nameof(position));
            else
                Insert(position, values, value);
        }

        private void Insert(int position, IEnumerable<I> values, I value)
        {
            var index = GetIndexFromPosition(position);

            var segment = this.segments[index];

            var column = position - segment.Position;

            Insert(index, column, values, value);
        }

        private void Insert(int index, int column, IEnumerable<I> values, I value)
        {
            var segment = this.segments[index];

            var lastsegment = index > 0 ? this.segments[index - 1] : new LineSegment();

            var split = false;

            var columnOffset = 0;

            var lineOffset = 0;

            var positionOffset = 0;


            void SplitSegment(LineEnding ending)
            {
                // Is either a newline or a \r|\n note for future inserts
                var isNewLine = (endingType != LineEnding.CrLf || ending == LineEnding.CrLf ? 1 : 0);


                lastsegment = new LineSegment(segment.LineNumber, segment.Position, column + columnOffset, ending);

                segment = new LineSegment(segment.LineNumber + isNewLine, segment.Position + lastsegment.Length, segment.Length - lastsegment.Length, segment.Ending);


                segments[index++] = lastsegment;

                segments.Insert(index, segment);


                lineOffset += isNewLine;

                positionOffset += columnOffset;

                column = columnOffset = 0;
            }

            void MergeSegment(LineEnding ending)
            {
                segment.Ending = ending;

                segment.Length++;

                segments.RemoveAt(index + 1);
            }


            void ProcessInitial()
            {
                // Push off <...[?]Lf> processing until end
                if (column == segment.Length - 1)
                {
                    // Split a <..Lf> into <..> + <Lf>
                    if (segment.Ending == LineEnding.Lf && endingType != LineEnding.Lf)
                    {
                        segment.Ending = LineEnding.None;

                        split = true;
                    }
                    // Split a <..CrLf> into <..Cr> + <Lf>
                    else if (segment.Ending == LineEnding.CrLf)
                    {
                        segment.Ending = LineEnding.Cr;

                        split = true;
                    }

                    if (split)
                    {
                        segments.Insert(index + 1, new LineSegment(segment.LineNumber, segment.Position + segment.Length, 1, LineEnding.Lf));

                        segment.Length--;
                    }
                }
            }

            void ProcessItem(I item)
            {
                var utf32 = ConvertToUtf32(item);

                // Cr
                if (endingType != LineEnding.Lf && LineEnding.IsCarriageReturn(utf32))
                {
                    segment.Length++;

                    columnOffset++;

                    SplitSegment(LineEnding.Cr);
                }
                // Lf
                else if (endingType != LineEnding.Cr && LineEnding.IsLineFeed(utf32))
                {
                    // CrLf <..Cr> + [Lf]
                    if (endingType != LineEnding.Lf && lastsegment.Ending == LineEnding.Cr && column + columnOffset == 0)
                    {
                        lastsegment.Length++;

                        lastsegment.Ending = LineEnding.CrLf;

                        segments[index - 1] = lastsegment;

                        segment.Position++;

                        positionOffset++;

                        if (endingType == LineEnding.CrLf)
                        {
                            segment.LineNumber++;

                            lineOffset++;
                        }
                    }
                    // Lf
                    else
                    {
                        segment.Length++;

                        columnOffset++;

                        SplitSegment(LineEnding.Lf);
                    }
                }
                // None
                else
                {
                    segment.Length++;

                    columnOffset++;
                }
            }

            void ProcessFinal()
            {
                // Check for merge from a split                
                if (split)
                {
                    // <..?> + <> + <Lf>
                    if (segment.Length == 0)
                    {
                        segments.RemoveAt(index--);
                        segment = segments[index];
                    }

                    if (segment.Ending == LineEnding.None)
                        MergeSegment(LineEnding.Lf);
                    else if (segment.Ending == LineEnding.Cr)
                        MergeSegment(LineEnding.CrLf);
                }

                segments[index++] = segment;

                positionOffset += columnOffset;
            }


            ProcessInitial();

            if (values != null)
                foreach (var item in values)
                    ProcessItem(item);
            else
                ProcessItem(value);

            ProcessFinal();


            if (positionOffset != 0)
                Adjust(lineOffset, positionOffset, index);

            length += positionOffset;
        }


        // Remove
        public void Remove(int position, int amount)
        {
            if (position < 0 || position > length)
                throw new ArgumentOutOfRangeException(nameof(position));

            if (amount < 0 || position + amount > length)
                throw new ArgumentOutOfRangeException(nameof(amount));

            var lineNumber = GetLineNumber(position);
        }


        // Extract
        private S ExtractLine(S value, int position, int length)
        {
            var valueLength = GetLength(value);

            if (position <= valueLength)
            {
                var end = position + length;

                if (end > valueLength)
                    end = valueLength - position;

                return Slice(value, position, length);
            }
            else
            {
                return default(S);
            }
        }

        public S ExtractLine(S value, int lineNumber, bool includeEnd = true)
        {
            if (lineNumber < 0 || lineNumber >= Count)
                throw new ArgumentOutOfRangeException(nameof(lineNumber));

            var line = GetLine(lineNumber);

            return ExtractLine(value, line.Position, line.Length - (includeEnd ? 0 : line.Ending.Size));
        }

        public IEnumerable<S> ExtractLines(S value, bool includeEnd = true)
        {
            foreach (var line in this)
                yield return ExtractLine(value, line.Position, line.Length - (includeEnd ? 0 : line.Ending.Size));
        }


        // Enumerator
        public IEnumerator<(int LineNumber, int Position, int Length, LineEnding Ending)> GetEnumerator()
        {
            if (endingType != LineEnding.CrLf)
            {
                foreach (var line in segments)
                    yield return line;
            }
            else
            {
                var lineNumber = 0;

                var segment = segments[0];

                for (int i = 1; i < segments.Count; i++)
                {
                    var nextSegment = segments[i];

                    if (nextSegment.LineNumber == lineNumber)
                    {
                        segment.Length += nextSegment.Length;

                        segment.Ending = nextSegment.Ending;
                    }
                    else
                    {
                        yield return segment;

                        lineNumber++;

                        segment = nextSegment;
                    }
                }

                yield return segment;
            }
        }

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();


        // Abstract
        protected abstract uint ConvertToUtf32(I value);

        protected abstract int GetLength(S value);

        protected abstract S Slice(S value, int start, int length);
    }
}