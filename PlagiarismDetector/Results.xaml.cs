using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Xml;
using PlagiarismDetector.Providers;

namespace PlagiarismDetector
{
    /// <summary>
    /// Interaction logic for Results.xaml
    /// </summary>
    public partial class Results : Window
    {
        public Results(List<DetectionResult> result)
        {
            InitializeComponent();
            //CalculatePlagPercent(result);
            langTabs.ItemsSource = result;
        }

        private void TabControl_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {

        }

        private void SelectAllBtn_Click(object sender, RoutedEventArgs e)
        {
            var selectedLangResult = langTabs.SelectedItem as DetectionResult;

            var myRichTextBox = FindVisualChildren<RichTextBox>(this).FirstOrDefault(x => x.Name == "resultTextBox" && x.Tag.ToString() == selectedLangResult.LanguageShortName);

            if (myRichTextBox != null)
            {
                Select(myRichTextBox, 1, selectedLangResult.TranslatedText.Length, Colors.White);

                foreach (var listItem in selectedLangResult.SearchResultList)
                    foreach (var shingle in listItem.Shingles)
                    {
                        this.Select(myRichTextBox, selectedLangResult.TranslatedText.IndexOf(shingle, StringComparison.InvariantCultureIgnoreCase), shingle.Length, Colors.Yellow);
                    }
            }
        }

        private void UnderlineText(string source, string pageText, string lang)
        {
            var myRichTextBox = FindVisualChildren<RichTextBox>(this).FirstOrDefault(x => x.Name == "resultTextBox" && x.Tag.ToString() == lang);
            Select(myRichTextBox, 1, source.Length, Colors.White);

            var union = source.WordShingles().Intersect(pageText.WordShingles());
            foreach (var da in union)
            {
                this.Select(myRichTextBox, source.IndexOf(da, StringComparison.InvariantCultureIgnoreCase), da.Length, Colors.Yellow);
            }
        }

        private void ListViewItem_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (sender is ListViewItem item)
            {
                var selectedListItem = item.Content as SearchResult;
                var selectedLangResult = langTabs.SelectedItem as DetectionResult;

                Label titleLabel = FindVisualChildren<Label>(this).FirstOrDefault(x => x.Name == "titleLabel" && x.Tag.ToString() == selectedLangResult.LanguageShortName);


                UnderlineText(selectedLangResult.TranslatedText, selectedListItem.TextOfPage, selectedLangResult.LanguageShortName);
                CalcPlagPercent(selectedListItem);


                titleLabel.Content = selectedListItem.Title;

                var hyperlink = FindVisualChildren<TextBlock>(this).FirstOrDefault(x => x.Name == "linkBlock" && x.Tag.ToString() == selectedLangResult.LanguageShortName).Inlines.First() as Hyperlink;
                hyperlink.NavigateUri = new Uri(selectedListItem.Link);
                hyperlink.Inlines.Clear();
                hyperlink.Inlines.Add(selectedListItem.DisplayLink);

                Label jaccardLabel = FindVisualChildren<Label>(this).FirstOrDefault(x => x.Name == "jaccardLabel" && x.Tag.ToString() == selectedLangResult.LanguageShortName);
                jaccardLabel.Content = (int)selectedListItem.Percent + " %";
            }
        }

        public void CalcPlagPercent(SearchResult selectedListItem)
        {
            var selectedLangResult = langTabs.SelectedItem as DetectionResult;
            var shingles = selectedLangResult.TranslatedText.WordShingles();
            var shingText = selectedListItem.TextOfPage.WordShingles();
            double index = shingles.OverlapCoefficient(shingText);
        }

        public void CalculatePlagPercent(List<DetectionResult> results)
        {
            try
            {
                Parallel.ForEach(results, res =>
                {
                    var shingles = res.TranslatedText.WordShingles();
                    foreach (var searchItem in res.SearchResultList)
                    {
                        searchItem.TextOfPage = GetTextOfPage(searchItem.Link);
                        searchItem.Percent = shingles.JaccardDistance(searchItem.TextOfPage.WordShingles()) * 100;
                    }
                    res.PlafiarismPercent = (int)res.SearchResultList.Select(x => x.Percent).Max();
                    res.SearchResultList.OrderByDescending(x => x.Count).OrderByDescending(x => x.Percent);
                });
            }
            catch { }
        }

        private string GetTextOfPage(string url)
        {
            try
            {
                HtmlAgilityPack.HtmlWeb web = new HtmlAgilityPack.HtmlWeb();
                HtmlAgilityPack.HtmlDocument doc = web.Load(url);

                doc.DocumentNode.Descendants()
                .Where(n => n.Name == "script" || n.Name == "style")
                .ToList()
                .ForEach(n => n.Remove());

                return doc.DocumentNode.InnerText;
            }catch
            {
                return "";
            }           
        }

        private void Hyperlink_RequestNavigate(object sender, RequestNavigateEventArgs e)
        {
            Process.Start(new ProcessStartInfo(e.Uri.AbsoluteUri));
            e.Handled = true;
        }

        private static TextPointer GetTextPointAt(TextPointer from, int pos)
        {
            TextPointer ret = from;
            int i = 0;

            while ((i < pos) && (ret != null))
            {
                if ((ret.GetPointerContext(LogicalDirection.Backward) == TextPointerContext.Text) || (ret.GetPointerContext(LogicalDirection.Backward) == TextPointerContext.None))
                    i++;

                if (ret.GetPositionAtOffset(1, LogicalDirection.Forward) == null)
                    return ret;

                ret = ret.GetPositionAtOffset(1, LogicalDirection.Forward);
            }

            return ret;
        }

        internal void Select(RichTextBox rtb, int offset, int length, Color color)
        {
            // Get text selection:
            TextSelection textRange = rtb.Selection;

            // Get text starting point:
            TextPointer start = rtb.Document.ContentStart;

            // Get begin and end requested:
            TextPointer startPos = GetTextPointAt(start, offset);
            TextPointer endPos = GetTextPointAt(start, offset + length);

            // New selection of text:
            textRange.Select(startPos, endPos);

            // Apply property to the selection:
            textRange.ApplyPropertyValue(TextElement.BackgroundProperty, new SolidColorBrush(color));

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

        public IEnumerable<object> FindVisualChildren(DependencyObject depObj)
        {
            if (depObj != null)
            {
                for (int i = 0; i < VisualTreeHelper.GetChildrenCount(depObj); i++)
                {
                    DependencyObject child = VisualTreeHelper.GetChild(depObj, i);

                    if (child != null)
                        yield return child;

                    foreach (var childOfChild in FindVisualChildren(child))
                        yield return childOfChild;
                }
            }
        }
    }

    public static class Helper
    {
        public static T FindChild<T>(this DependencyObject parent, string childName)
         where T : DependencyObject
        {
            if (parent == null) return null;

            T foundChild = null;

            var childrenCount = VisualTreeHelper.GetChildrenCount(parent);
            for (var i = 0; i < childrenCount; i++)
            {
                var child = VisualTreeHelper.GetChild(parent, i);
                var childType = child as T;
                if (childType == null)
                {
                    foundChild = FindChild<T>(child, childName);
                    if (foundChild != null) break;
                }
                else if (!string.IsNullOrEmpty(childName))
                {
                    var frameworkElement = child as FrameworkElement;
                    if (frameworkElement != null && frameworkElement.Name == childName)
                    {
                        foundChild = (T)child;
                        break;
                    }
                }
                else
                {
                    foundChild = (T)child;
                    break;
                }
            }

            return foundChild;
        }
    }

}
