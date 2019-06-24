using System;
using System.Collections.Generic;

namespace Veruthian.Library.Readers
{
    public abstract class BaseVariableLookaheadReader<T> : BaseLookaheadReader<T>
    {
        List<T> cache = new List<T>();

        int index;


        public BaseVariableLookaheadReader() { }


        protected List<T> Cache => cache;

        protected int CacheIndex { get => index; set => index = value; }

        protected int CacheSize => cache.Count;

        protected virtual bool CanReset => true;


        protected override void Initialize()
        {
            cache.Clear();

            index = 0;

            Position = 0;
        }


        protected void EnsureIndex(int index)
        {
            int available = (CacheSize - index);

            // Prefetch (d + 1) items
            if (available <= 0)
                Prefetch(available + 1);
        }

        protected override void EnsureLookahead(int lookahead = 0) => EnsureIndex(index + lookahead);


        private void Prefetch(int amount)
        {
            if (!EndFound)
            {
                int lastPosition = Position - index;

                for (int i = 0; i < amount; i++)
                {
                    bool success = GetNext(out T next);

                    if (success)
                        cache.Add(next);
                    else
                        EndPosition = lastPosition + CacheSize;
                }
            }
        }

        protected T RawPeekByIndex(int index)
        {
            // We don't want to add infinite end items, just return the cached end item
            if (EndFound && index >= cache.Count)
                return LastItem;
            else
                return cache[index];
        }

        protected override T RawLookahead(int lookahead = 0) => RawPeekByIndex(index + lookahead);


        protected override void MoveNext()
        {
            if (!IsEnd)
            {
                Position++;

                index++;

                if (index == CacheSize && CanReset)
                    Reset();

                EnsureLookahead(1);
            }
        }

        protected override void TryPreload(int amount) => EnsureLookahead(amount);

        protected override void SkipAhead(int amount)
        {
            if (index + amount >= CacheSize)
            {
                var delta = (CacheSize - index);

                Position += delta;

                amount -= delta;

                index = CacheSize;

                if (CanReset)
                    Reset();

                for (int i = 0; i < amount; i++)
                {
                    MoveNext();

                    if (IsEnd)
                        break;
                }
            }
            else
            {
                index += amount;

                Position += amount;
            }
        }

        protected virtual void Reset()
        {
            index = 0;

            cache.Clear();
        }
    }
}