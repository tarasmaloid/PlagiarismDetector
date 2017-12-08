using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PlagiarismDetector.Providers
{
    public static class SimilarityAlgorithms
    {
        public static double JaccardIndex<T>(this ISet<T> value, ISet<T> shingles)
        {
            var unionCount = value.Union(shingles).Count();
            var intersectCount = value.Intersect(shingles).Count();
            if (unionCount == 0) return 0.0;
            return (double)intersectCount / (double) unionCount;
        }

        public static double JaccardDistance<T>(this ISet<T> value, ISet<T> shingles)
        {
            return 1.0 - value.JaccardIndex(shingles);
        }

        public static double OverlapCoefficient<T>(this ISet<T> value, ISet<T> shingles)
        {
            var minLength = Math.Min(value.Count(), shingles.Count());
            if (minLength == 0) return 0;
            var intersectCount = value.Intersect(shingles).Count();
            return intersectCount / (double)minLength;
        }


        public static int LevenshteinDistance(this string value, string text)
        {
            if (string.Equals(value, text)) return 0;
            if (String.IsNullOrEmpty(value) || String.IsNullOrEmpty(text)) return (value ?? String.Empty).Length + (text ?? String.Empty).Length;

            if (value.Length > text.Length)
            {
                var tmp = value;
                value = text;
                text = tmp;
            }

            if (text.Contains(value)) return text.Length - value.Length;
            int[,] distance = new int[value.Length + 1, text.Length + 1];
            for (int i = 0; i <= value.Length; distance[i, 0] = i++) ;
            for (int j = 0; j <= text.Length; distance[0, j] = j++) ;
            for (int i = 1; i <= value.Length; i++)
            {
                for (int j = 1; j <= text.Length; j++)
                {
                    int cost = (text[j - 1] == value[i - 1]) ? 0 : 1;

                    distance[i, j] = Math.Min(
                        Math.Min(distance[i - 1, j] + 1, distance[i, j - 1] + 1),
                        distance[i - 1, j - 1] + cost);
                }
            }
            // Step 7
            return distance[value.Length, text.Length];
        }


        private const int NumberOfBitsInAByte = 8;

        public static int SimHash<T>(this ISet<T> value, Func<T, int> hashFunction)
        {
            var hashVector = new int[sizeof(int) * NumberOfBitsInAByte];
            var simHash = 0;
            foreach (var shingle in value)
            {
                var tempHash = hashFunction(shingle);
                var mask = 1;
                for (int j = 0; j < hashVector.Length; j++)
                {
                    if ((tempHash & mask) > 0)
                    {
                        hashVector[j]++;
                    }
                    else
                    {
                        hashVector[j]--;
                    }
                    mask <<= 1;
                }
            }
            for (int i = 0; i < hashVector.Length; i++)
            {
                if (hashVector[i] > 0)
                {
                    simHash |= 1 << i;
                }
            }
            return simHash;
        }

        public static int CountSetBits(this int value)
        {
            var count = ((value & 0xfff) * 0x1001001001001 & 0x84210842108421) % 0x1f;
            count += (((value & 0xfff000) >> 12) * 0x1001001001001 & 0x84210842108421) % 0x1f;
            count += ((value >> 24) * 0x1001001001001 & 0x84210842108421) % 0x1f;
            return (int)count;
        }

        public static int HammingDistance(this int value, int number)
        {
            var dist = value ^ number;
            return dist.CountSetBits();
        }

        private const int HashSize = 32;

        public static float SimHashSimilarityIndex(this int value, int number)
        {
            return (HashSize - value.HammingDistance(number)) / (float)HashSize;
        }
    }
}
