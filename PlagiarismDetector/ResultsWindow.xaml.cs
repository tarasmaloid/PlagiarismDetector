using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;

namespace PlagiarismDetector
{
    /// <summary>
    /// Interaction logic for ResultsWindow.xaml
    /// </summary>
    public partial class ResultsWindow : Window
    {
        public ResultsWindow(List<DetectionResult> resultList)
        {
            InitializeComponent();
            InitView(resultList);
        }

        private void ListViewItem_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            var item = sender as ListViewItem;
            if (item != null && item.IsSelected)
            {
                var content = item.Content as SearchResult;
                //Do your stuff
            }
        }
        private void InitView(List<DetectionResult> resultList)
        {
            foreach (DetectionResult resultItem in resultList)
            {
                var grid = new Grid();
                grid.ColumnDefinitions.Add(new ColumnDefinition()
                {
                    Width = new GridLength(1, GridUnitType.Star)
                });
                grid.ColumnDefinitions.Add(new ColumnDefinition()
                {
                    Width = new GridLength(1, GridUnitType.Star)
                });

                var listGrid = new GridView();
                listGrid.Columns.Add(new GridViewColumn()
                {
                    Header = "Title",
                    DisplayMemberBinding = new Binding("Title")
                });
                listGrid.Columns.Add(new GridViewColumn()
                {
                    Header = "Link",
                    DisplayMemberBinding = new Binding("Link")
                });
                listGrid.Columns.Add(new GridViewColumn()
                {
                    Header = "Count",
                    DisplayMemberBinding = new Binding("Count")
                });


                var style = new Style();
                style.Setters.Add(new EventSetter()
                {
                    Event = UIElement.PreviewMouseLeftButtonUpEvent,
                    Handler = new MouseButtonEventHandler(this.ListViewItem_PreviewMouseLeftButtonDown)
                });

                var list = new ListView()
                {
                    HorizontalAlignment = HorizontalAlignment.Stretch,
                    VerticalAlignment = VerticalAlignment.Stretch,
                    Name = "resultList",
                    Margin = new Thickness(5),
                    View = listGrid,
                    ItemContainerStyle = style                    
                };


                Grid.SetColumn(list, 0);
                grid.Children.Add(list);

                langTabs.Items.Add(new TabItem()
                {
                    Header = resultItem.LanguageName,
                    Content = grid
                });



                Binding bind = new Binding();
                list.DataContext = resultItem.SearchResultList;
                list.SetBinding(ListView.ItemsSourceProperty, bind);
            }
        }
    }
}
