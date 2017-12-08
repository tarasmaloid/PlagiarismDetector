using Microsoft.Win32;
using System.IO;
using System.Windows;
using System;
using Microsoft.Office.Interop.Word;
using System.Collections.Generic;
using System.Windows.Controls;
using System.Xml.Linq;
using System.Xml.Serialization;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Linq;
using System.Windows.Media;
using System.Threading;
using System.Text;
using System.Windows.Data;
using PlagiarismDetector.Providers;

namespace PlagiarismDetector
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : System.Windows.Window
    {

        CancellationTokenSource cts;

        public MainWindow()
        {
            InitializeComponent();
            InitLanguageCheckBoxPanel();
        }

        private void InitLanguageCheckBoxPanel()
        {
            foreach (var lang in Constants.languageCollection)
            {
                var wrapPanel = new WrapPanel()
                {
                    Margin = new Thickness(5.0)
                };

                var checkBox = new System.Windows.Controls.CheckBox()
                {
                    Content = lang.Key,
                    Margin = new Thickness(5.0),
                    Height = 20,
                    Width = 100

                };
                checkBox.Click += SearchOptionsChanged;

                var progressBar = new ProgressBar()
                {
                    Minimum = 1,
                    Maximum = 100,
                    Name = "pb" + lang.Key,
                    Height = 20,
                    Width = 300
                };
                progressBar.SetValue(Grid.ColumnProperty, 2);

                wrapPanel.Children.Add(checkBox);
                wrapPanel.Children.Add(progressBar);

                checkBoxPanel.Children.Add(wrapPanel);
            }
        }

        private void SearchOptionsChanged(object sender, RoutedEventArgs e)
        {
            ChangeSearchBtn();
        }

        private void ChangeSearchBtn()
        {
            Search.IsEnabled = GetSelectedLanguages().Count > 0 && !string.IsNullOrEmpty(TxtEditor.Text) ? true : false;
        }

        private void btnOpenFile_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog()
            {
                Filter = "Text files |*.docx;*.doc;*.txt|All files (*.*)|*.*"
            };
            if (openFileDialog.ShowDialog() == true)
            {
                var ext = Path.GetExtension(openFileDialog.FileName);
                if (ext == ".doc" || ext == ".docx")
                {
                    Microsoft.Office.Interop.Word.Application word = new Microsoft.Office.Interop.Word.Application();
                    object miss = System.Reflection.Missing.Value;
                    object path = openFileDialog.FileName;
                    object readOnly = true;
                    Document docs = word.Documents.Open(ref path, ref miss, ref readOnly, ref miss, ref miss, ref miss, ref miss, ref miss, ref miss, ref miss, ref miss, ref miss, ref miss, ref miss, ref miss, ref miss);
                    TxtEditor.Text = docs.Content.Text;

                    docs.Close();
                    word.Quit();
                }
                else
                {
                    TxtEditor.Text = File.ReadAllText(openFileDialog.FileName, Encoding.GetEncoding(1251));
                }
            }
        }

        private async void Search_ClickAsync(object sender, RoutedEventArgs e)
        {
            Search.Visibility = Visibility.Hidden;
            Cancel.Visibility = Visibility.Visible;
            cts = new CancellationTokenSource();

            List<DetectionResult> detectResult = new List<DetectionResult>();
            List<ProgressBar> progressBarsList = FindVisualChildren<ProgressBar>(checkBoxPanel).ToList();

            string sourceText = TxtEditor.Text;

            foreach (var lang in GetSelectedLanguages())
            {
                detectResult.Add(new DetectionResult(lang, Constants.languageCollection[lang], sourceText));
            }

            await System.Threading.Tasks.Task.WhenAll(detectResult.Select(async det =>
            {
                ProgressBar pBar = progressBarsList.FirstOrDefault(x => x.Name == "pb" + det.LanguageName);
                IProgress<int> progress = new Progress<int>(n => pBar.Value = n);

                await det.RunDetection(progress, (int)shingleValue.Value, (int)overlapValue.Value, cts.Token);
                await CalculatePlagPercent(det, progress, cts.Token);
            }));


            //  Serialization.SerializeObject<List<DetectionResult>>(detectResult, "file.xml");

            //detectResult = Serialization.DeSerializeObject<List<DetectionResult>>("file.xml");
            //await System.Threading.Tasks.Task.WhenAll(detectResult.Select(async det =>
            //{
            //    ProgressBar pBar = progressBarsList.FirstOrDefault(x => x.Name == "pb" + det.LanguageName);
            //    IProgress<int> progress = new Progress<int>(n => pBar.Value = n);

            //    await CalculatePlagPercent(det, progress, cts.Token);
            //}));


            Results resultsWindow = new Results(detectResult);
            resultsWindow.Show();
        }

        public async Task<int> CalculatePlagPercent(DetectionResult results, IProgress<int> progress, CancellationToken ct)
        {
            int totalCount = results.SearchResultList.Count;

            int processCount = await System.Threading.Tasks.Task.Run(() =>
            {
                int tempCount = 0;
                var shingles = results.TranslatedText.WordShingles(3, 2);

                System.Threading.Tasks.Task.WhenAll(results.SearchResultList.Select(async searchItem =>
                {
                    searchItem.TextOfPage = await GetTextOfPage(searchItem.Link);
                    searchItem.Percent = shingles.OverlapCoefficient(searchItem.TextOfPage.WordShingles(3, 2)) * 100;

                    progress.Report((tempCount * 100 / totalCount));
                    tempCount++;
                }));

                results.PlafiarismPercent = (int)results.SearchResultList.Select(x => x.Percent).Max();
                results.SearchResultList.OrderByDescending(x => x.Count).OrderByDescending(x => x.Percent);

                return tempCount;
            }, ct);

            return processCount;
        }

        private Task<string> GetTextOfPage(string url)
        {
            try
            {
                HtmlAgilityPack.HtmlWeb web = new HtmlAgilityPack.HtmlWeb();
                HtmlAgilityPack.HtmlDocument doc = web.Load(url);

                doc.DocumentNode.Descendants()
                .Where(n => n.Name == "script" || n.Name == "style")
                .ToList()
                .ForEach(n => n.Remove());

                return System.Threading.Tasks.Task.Run(() => doc.DocumentNode.InnerText);
            }
            catch
            {
                return System.Threading.Tasks.Task.Run(() => "");
            }
        }

        private List<string> GetSelectedLanguages()
        {
            List<string> langList = new List<string>();
            var checkBoxesList = FindVisualChildren<System.Windows.Controls.CheckBox>(checkBoxPanel);

            foreach (var c in checkBoxesList)
            {
                if (c.IsChecked == true)
                {
                    langList.Add(c.Content.ToString());
                }
            }
            return langList;
        }

        public IEnumerable<T> FindVisualChildren<T>(DependencyObject depObj) where T : DependencyObject
        {
            if (depObj != null)
            {
                for (int i = 0; i < VisualTreeHelper.GetChildrenCount(depObj); i++)
                {
                    DependencyObject child = VisualTreeHelper.GetChild(depObj, i);

                    if (child != null && child is T)
                        yield return (T)child;

                    foreach (T childOfChild in FindVisualChildren<T>(child))
                        yield return childOfChild;
                }
            }
        }



        private void TxtEditor_TextChanged(object sender, TextChangedEventArgs e)
        {
            ChangeSearchBtn();
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            Cancel.Visibility = Visibility.Hidden;
            Search.Visibility = Visibility.Visible;
            cts.Cancel();
        }
    }
}








//foreach (var det in detectResult)
//{
//    //  ProgressBar pBar = FindVisualChildren<ProgressBar>(this).FirstOrDefault(x => x.Name == "pb" + det.LanguageName);

//    ProgressBar pBar = progressBarsList.FirstOrDefault(x => x.Name == "pb" + det.LanguageName);
//    IProgress<int> progress = new Progress<int>(n => pBar.Value = n);


//    await det.RunDetection(progress, (int)shingleValue.Value, (int)overlapValue.Value);
//    //await Dispatcher.BeginInvoke(new ThreadStart(() => det.RunDetection(progress, pBar, (int)shingleValue.Value, (int)overlapValue.Value)));
//}


//Serialization.SerializeObject<List<DetectionResult>>(detectResult, "file4.xml");

//detectResult = Serialization.DeSerializeObject<List<DetectionResult>>("file2.xml");
//foreach (var res in detectResult)
//{
//    ProgressBar pBar = FindVisualChildren<ProgressBar>(this).FirstOrDefault(x => x.Name == "pb" + res.LanguageName);
//    IProgress<int> progress = new Progress<int>(n => pBar.Value = n);

//    res.TranslatedText = Regex.Replace(Regex.Replace(res.TranslatedText, @"[\!\?\.\,]+", ""), @"[\n\r\-/\s\s/]+", " ");
//    await System.Threading.Tasks.Task.Run(() => res.SimulateWork(progress, pBar));
//}