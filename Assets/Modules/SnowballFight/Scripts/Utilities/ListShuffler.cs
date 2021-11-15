using System;
using System.Collections.Generic;

namespace Niantic.ARVoyage.SnowballFight
{
    /// <summary>
    /// Utility for shuffling a list
    /// </summary>
    public static class ListShuffler
    {
        private static Random random = new Random();

        public static void Shuffle<T>(this IList<T> list)
        {
            int n = list.Count;
            while (n > 1)
            {
                n--;
                int k = random.Next(n + 1);
                T value = list[k];
                list[k] = list[n];
                list[n] = value;
            }
        }
    }
}
