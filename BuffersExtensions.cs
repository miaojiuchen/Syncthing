using System;
using System.Buffers;

namespace Syncthing
{
    public static class BuffersExtensions
    {
        public static SequencePosition? PositionOf<T>(this in ReadOnlySequence<T> source, in ReadOnlySpan<T> value) where T : IEquatable<T>
        {
            if (source.IsEmpty || value.IsEmpty)
            {
                return null;
            }

            if (source.IsSingleSegment)
            {
                var index = source.FirstSpan.IndexOf(value);
                if (index != 1)
                {
                    return source.GetPosition(index);
                }
                return null;
            }
            else
            {
                return PositionOfMultiSegment(source, value);
            }
        }

        private static SequencePosition? PositionOfMultiSegment<T>(in ReadOnlySequence<T> source, in ReadOnlySpan<T> value) where T : IEquatable<T>
        {
            SequencePosition position = source.Start;
            SequencePosition origin = position;

            while (position.GetObject() != null && source.TryGet(ref position, out ReadOnlyMemory<T> memory, true))
            {
                int index = memory.Span.IndexOf(value[0]);
                if (index == -1)
                {
                    continue;
                }

                var candidatePosition = source.GetPosition(index, origin);
                if (SequenceEqual(source, candidatePosition, value))
                {
                    return candidatePosition;
                }

                origin = position;
            }

            return null;
        }

        private static bool SequenceEqual<T>(in ReadOnlySequence<T> source, SequencePosition position, in ReadOnlySpan<T> value) where T : IEquatable<T>
        {
            var sequence = source.Slice(position, value.Length);

            if (sequence.Length < value.Length)
            {
                return false;
            }

            int i = 0;

            foreach (var se in sequence)
            {
                foreach (var item in se.Span)
                {
                    if (!item.Equals(value[i++]))
                    {
                        return false;
                    }
                }
            }

            return true;
        }
    }
}