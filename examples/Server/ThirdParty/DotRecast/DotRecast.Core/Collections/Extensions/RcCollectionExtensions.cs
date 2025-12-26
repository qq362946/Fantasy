using System;
using System.Collections.Generic;

namespace DotRecast.Core.Collections.Extensions
{
    public static class RcCollectionExtensions
    {
        /// Sorts the given data in-place using insertion sort.
        ///
        /// @param	data		The data to sort
        /// @param	dataLength	The number of elements in @p data
        public static void InsertSort(this int[] data)
        {
            for (int valueIndex = 1; valueIndex < data.Length; valueIndex++)
            {
                int value = data[valueIndex];
                int insertionIndex;
                for (insertionIndex = valueIndex - 1; insertionIndex >= 0 && data[insertionIndex] > value; insertionIndex--)
                {
                    // Shift over values
                    data[insertionIndex + 1] = data[insertionIndex];
                }

                // Insert the value in sorted order.
                data[insertionIndex + 1] = value;
            }
        }

        public static void ForEach<T>(this IEnumerable<T> list, Action<T> action)
        {
            foreach (var item in list)
            {
                action.Invoke(item);
            }
        }

        public static void ForEach<T>(this RcImmutableArray<T> list, Action<T> action)
        {
            for (int i = 0; i < list.Count; ++i)
            {
                action.Invoke(list[i]);
            }
        }

        public static void Shuffle<T>(this IList<T> list)
        {
            Random random = new Random();
            int n = list.Count;
            while (n > 1)
            {
                n--;
                int k = random.Next(n + 1);
                (list[k], list[n]) = (list[n], list[k]);
            }
        }

        public static void AddRange<T>(this List<T> list, Span<T> span)
        {
            foreach (var i in span)
            {
                list.Add(i);
            }
        }
    }
}