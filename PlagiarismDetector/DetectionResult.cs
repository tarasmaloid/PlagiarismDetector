using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using PlagiarismDetector.Providers;
using System.Xml.Serialization;
using System.Threading;
using System.Windows.Controls;
using System.Windows.Threading;
using System.Text.RegularExpressions;
using System.Collections.Concurrent;

namespace PlagiarismDetector
{
    public class DetectionResult
    {
        [XmlElement("TranslatedText")]
        public string TranslatedText { get; set; }
        [XmlElement("LanguageName")]
        public string LanguageName { get; set; }
        [XmlElement("LanguageShortName")]
        public string LanguageShortName { get; set; }
        [XmlElement("SearchResultList")]
        public List<SearchResult> SearchResultList { get; set; }
        public int PlafiarismPercent { get; set; }

        public DetectionResult(string langName, string langShortName, string sourceText)
        {
            LanguageName = langName;
            LanguageShortName = langShortName;
            TranslatedText = NormalizeText(GoogleCloudProvider.TranslateText(sourceText, langShortName));
        }

        public DetectionResult() { }

        //public void RunDetection(IProgress<int> progress, ProgressBar pBar, int sningleSize, int shingleOverlap)
        //{
        //    SearchResultList = GetSearchResultListAsync(ShingleProvider.WordShingles(TranslatedText, sningleSize, shingleOverlap), progress, pBar);
        //}
        public async Task<int> RunDetection(IProgress<int> progress, int sningleSize, int shingleOverlap, CancellationToken ct)
        {
            return await GetSearchResultListAsync(ShingleProvider.WordShingles(TranslatedText, sningleSize, shingleOverlap), progress, ct);
        }

        public static string NormalizeText(string sourceText)
        {
            return Regex.Replace(Regex.Replace(sourceText, @"[\!\?\.\,]+", ""), @"[\n\r\-/\s\s/]+", " ");
        }


        public async Task<int> GetSearchResultListAsync(HashSet<string> shingles, IProgress<int> progress, CancellationToken ct)
        {
            ConcurrentDictionary<string, SearchResult> resultDictionary = new ConcurrentDictionary<string, SearchResult>(StringComparer.InvariantCultureIgnoreCase);
            
            int totalCount = shingles.Count;                       
            int processCount = await Task.Run(() =>
            {
                int tempCount = 0;
                Task.WhenAll(shingles.Select(async shingle =>
                {
                    var resultsForShingle = await GoogleCloudProvider.SearchText(shingle, 3);

                    foreach (var result in resultsForShingle)
                    {
                        if (resultDictionary.ContainsKey(result.FormattedUrl))
                        {
                            resultDictionary[result.FormattedUrl].Count++;
                            resultDictionary[result.FormattedUrl].Shingles.Add(shingle);
                        }
                        else
                        {
                            resultDictionary[result.FormattedUrl] = new SearchResult(result.Link, result.Title, result.FormattedUrl, result.Snippet, shingle);
                        }
                    }
                    progress.Report((tempCount * 100 / totalCount));
                    tempCount++;
                }));

                return tempCount;
            }, ct);

            SearchResultList = resultDictionary
                .Values
                .OrderByDescending(o => o.Count)
                .Take(50)
                .ToList();

            return processCount;
        }
    }
}

    //private static async Task<List<SearchResult>> GetSearchResultListAsync(HashSet<string> shingles, IProgress<int> progress, ProgressBar pBar)
    //{
    //    // List<SearchResult> resultList = new List<SearchResult>();

    //    Dictionary<string, SearchResult> resultDictionary = new Dictionary<string, SearchResult>(StringComparer.InvariantCultureIgnoreCase);

    //    int iterations = 0;
    //    int count = shingles.Count;
    //    object sync = new Object();


    //    //await Task.Run(() => Parallel.ForEach(shingles, shingle =>

    //    foreach (var shingle in shingles)
    //    {
    //        var resultsForShingle = GoogleCloudProvider.SearchText(shingle, 3);
    //        // var resultItem = new SearchResult();

    //        //foreach (var result in resultsForShingle)
    //        //{
    //        //    lock (sync)
    //        //    {
    //        //        resultItem = resultList.FirstOrDefault(st => String.Equals(st.DisplayLink, result.DisplayLink, StringComparison.InvariantCultureIgnoreCase));
    //        //        if (resultItem != null)
    //        //        {
    //        //            resultItem.Count++;
    //        //            resultItem.Shingles.Add(shingle);
    //        //        }
    //        //        else
    //        //        {
    //        //            resultList.Add(new SearchResult(result.Link, result.Title, result.FormattedUrl, result.Snippet, shingle));
    //        //        }
    //        //    }
    //        //}


    //        foreach (var result in resultsForShingle)
    //        {
    //            lock (sync)
    //            {
    //                if (resultDictionary.ContainsKey(result.FormattedUrl))
    //                {
    //                    resultDictionary[result.FormattedUrl].Count++;
    //                    resultDictionary[result.FormattedUrl].Shingles.Add(shingle);
    //                }
    //                else
    //                {
    //                    resultDictionary[result.FormattedUrl] = new SearchResult(result.Link, result.Title, result.FormattedUrl, result.Snippet, shingle);
    //                }
    //            }
    //        }

    //        lock (sync)
    //        {
    //            Interlocked.Increment(ref iterations);
    //            int pr = Convert.ToInt32(((double)iterations / (double)count) * 100);
    //             progress.Report(pr);
    //        }

    //    }


    //    return resultDictionary
    //        .Values
    //        .OrderByDescending(o => o.Count)
    //        .Take(50)
    //        .ToList();

    //    //return resultList
    //    //    .OrderByDescending(o => o.Count)
    //    //    .Take(50)
    //    //    .ToList();
    //}


    public class SearchResult
{
    [XmlElement("Link")]
    public string Link { get; set; }
    [XmlElement("Title")]
    public string Title { get; set; }
    [XmlElement("DisplayLink")]
    public string DisplayLink { get; set; }
    [XmlElement("Snippet")]
    public string Snippet { get; set; }
    [XmlElement("Count")]
    public int Count { get; set; }
    [XmlElement("Shingles")]
    public List<string> Shingles { get; set; }
    public string TextOfPage { get; set; }
    public double Percent { get; set; }
    public SearchResult() { }
    public SearchResult(string link, string title, string displayLink, string snippet, string shingle)
    {
        Link = link;
        Title = title;
        DisplayLink = displayLink;
        Snippet = snippet;

        Shingles = new List<string>
            {
                shingle
            };
        Count = 1;
    }
}

