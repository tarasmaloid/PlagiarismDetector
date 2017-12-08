using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace PlagiarismDetector.Providers
{
    static class ShingleProvider
    {
        private const int DefaultShingleSize = 3;
        private const int DefaultShingleOverlap = 1;

        public static HashSet<string> CharacterShingles(this string value, int shingleSize = DefaultShingleSize, int shingleOverlap = DefaultShingleOverlap)
        {
            if (shingleOverlap >= shingleSize) throw new ArgumentException("Shingle overlap cannot be bigger than the shingle size");
            var result = new HashSet<string>(StringComparer.InvariantCultureIgnoreCase);
            if (!string.IsNullOrEmpty(value))
            {
                var loopCount = value.Length - shingleSize;
                for (int i = 0; i <= loopCount; i = i + shingleSize - shingleOverlap) result.Add(value.Substring(i, shingleSize));
            }
            return result;
        }

        public static HashSet<string> WordShingles(this string value, int shingleSize = DefaultShingleSize, int shingleOverlap = DefaultShingleOverlap)
        {
            if (shingleOverlap >= shingleSize) throw new ArgumentException("Shingle overlap cannot be bigger than the shingle size");
            var result = new HashSet<string>(StringComparer.InvariantCultureIgnoreCase);
            if (!string.IsNullOrEmpty(value))
            {
                var words = Regex.Split(value, @"[\s\!\?\.\,\-]+", RegexOptions.CultureInvariant);
                var loopCount = words.Length - shingleSize;
                for (int i = 0; i <= loopCount; i = i + shingleSize - shingleOverlap) result.Add(string.Join(" ", words, i, shingleSize));
            }
            return result;
        }
    }
}
