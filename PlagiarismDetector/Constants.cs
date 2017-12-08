using System.Collections.Generic;

namespace PlagiarismDetector
{
    static class Constants
    {
        public static Dictionary<string, string> languageCollection = new Dictionary<string, string>()
        {
            { "English", "en" },
            { "French", "fr" },
            { "German", "de" },
            { "Italian", "it" },
            { "Polish", "pl" },
            { "Russian", "ru" },
            { "Spanish", "es" },
            { "Ukrainian", "uk" }
        };
    }
}
