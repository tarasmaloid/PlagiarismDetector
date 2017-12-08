using Google.Apis.Customsearch.v1;
using Google.Apis.Customsearch.v1.Data;
using Google.Apis.Services;
using Google.Apis.Translate.v2;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace PlagiarismDetector.Providers
{
    static class GoogleCloudProvider
    {
        const string apiKey = "yourApiKey";
        const string searchEngineId = "yourSearchEngineId";
        const string applicationName = "yourApplicationName";

        private static BaseClientService.Initializer GetClientService()
        {
            return new BaseClientService.Initializer()
            {
                ApiKey = apiKey,
                ApplicationName = applicationName
            };
        }

        public static string TranslateText(string sourceText, string targetLang)
        {
            var service = new TranslateService(GetClientService());
            string[] srcText = new[] { sourceText };
            var response = service.Translations.List(srcText, targetLang).Execute();

            return response.Translations[0].TranslatedText;
        }

        public static Task<List<Result>> SearchText(string searchQuery, int pages = 1)
        {
            try
            {
                var customSearchService = new CustomsearchService(GetClientService());
                var listRequest = customSearchService.Cse.List(searchQuery);
                listRequest.Cx = searchEngineId;
                listRequest.Num = 10;

                List<Result> results = new List<Result>();

                for (int i = 0; i < pages; i++)
                {
                    listRequest.Start = i * 10 + 1;
                    var temp = listRequest.Execute().Items;
                    if (temp != null)
                        results.AddRange(temp);
                }
                return Task.Run(() => results);
            }
            catch 
            {
                return Task.Run(() => new List<Result>());
            }
        }

        //public static Dictionary<string, int> GetSearchResultDictionary(IEnumerable<string> textShingles)
        //{
        //    var searchResults = new SortedDictionary<string, int>(StringComparer.InvariantCultureIgnoreCase);

        //    Parallel.ForEach(textShingles, shingle =>
        //    {
        //        var resultsForShingle = SearchText(shingle);

        //        foreach (var result in resultsForShingle)
        //        {
        //            if (searchResults.ContainsKey(result.Link))
        //            {
        //                lock (searchResults)
        //                    searchResults[result.Link]++;
        //            }
        //            else lock (searchResults)
        //                    searchResults[result.Link] = 1;
        //        }
        //    });

        //    return searchResults.OrderByDescending(x => x.Value).ToDictionary(pair => pair.Key, pair => pair.Value);
        //}


    }
}
