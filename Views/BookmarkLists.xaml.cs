using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Navigation;
using Microsoft.Phone.Controls;
using Microsoft.Phone.Shell;
using NowReadable.ViewModels;
using ReadabilityApi.Models;
using System.Windows.Data;
using System.Threading.Tasks;
using System.ComponentModel;

namespace NowReadable.Views
{
    public partial class BookmarkLists : PhoneApplicationPage
    {
        public BookmarkLists()
        {
            InitializeComponent();

            DataContext = App.MainViewModel;
        }
        
        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            if (!App.MainViewModel.IsDataLoaded)
            {
                var task = App.MainViewModel.LoadData();
            }
            
            string pivotIndex = "";
            if (NavigationContext.QueryString.TryGetValue("id", out pivotIndex))
            {
                BookmarkListsPivots.SelectedIndex = Convert.ToInt32(pivotIndex);
            }
        }

        private void Bookmark_Selected(object sender, SelectionChangedEventArgs e)
        {
            if ((sender as ListBox).SelectedIndex != -1)
            {
                var selectedItem = (sender as ListBox).SelectedItem as Bookmark;
                NavigationService.Navigate(new System.Uri(string.Format("/Views/Article.xaml?bookmarkId={0}", selectedItem.Id), System.UriKind.Relative));
                (sender as ListBox).SelectedIndex = -1;
            }
        }

        private void BookmarkListsPivots_LoadedPivotItem(object sender, PivotItemEventArgs e)
        {
            var pivot = sender as Pivot;
            if (e.Item == pivot.Items[0])
            {
                ReadingListSortOrder.Visibility = Visibility.Visible;
                FavoritesSortOrder.Visibility = Visibility.Collapsed;
                ArchiveSortOrder.Visibility = Visibility.Collapsed;
            }
            else if (e.Item == pivot.Items[1])
            {
                ReadingListSortOrder.Visibility = Visibility.Collapsed;
                FavoritesSortOrder.Visibility = Visibility.Visible;
                ArchiveSortOrder.Visibility = Visibility.Collapsed;
            }
            else if (e.Item == pivot.Items[2])
            {
                ReadingListSortOrder.Visibility = Visibility.Collapsed;
                FavoritesSortOrder.Visibility = Visibility.Collapsed;
                ArchiveSortOrder.Visibility = Visibility.Visible;
            }
        }

        private void BookmarkListsPivots_LoadingPivotItem(object sender, PivotItemEventArgs e)
        {
            var pivot = sender as Pivot;
            var listBox = (e.Item.Content as ListBox);

            if (listBox.Items.Count != 0) return;

            Binding binding = new Binding();
            if (e.Item == pivot.Items[0])
            {
                binding = new Binding("ReadingListView.View");
            }
            else if (e.Item == pivot.Items[1])
            {
                binding = new Binding("FavoritesView.View");
            }
            else if (e.Item == pivot.Items[2])
            {
                binding = new Binding("ArchiveView.View");
            }

            listBox.SelectedIndex = -1; //Setting SelectedIndex so that it doesn't navigate to the first article automatically.
            binding.Source = App.MainViewModel;
            listBox.SetBinding(ListBox.ItemsSourceProperty, binding);
            listBox.SelectionChanged += Bookmark_Selected;
            listBox.SelectedIndex = -1; //Setting SelectedIndex so that it doesn't highlight the first article.
        }

        private void Sort_Newest(object sender, EventArgs e)
        {
            if (BookmarkListsPivots.SelectedIndex == 0)
            {
                ReadingListSortOrder.Text = "recently added first";
                App.MainViewModel.ReadingListView.SortDescriptions.Clear();
                App.MainViewModel.ReadingListView.SortDescriptions.Add(new SortDescription("DateAdded", ListSortDirection.Descending));
            }
            else if (BookmarkListsPivots.SelectedIndex == 1)
            {
                FavoritesSortOrder.Text = "recently favorited first";
                App.MainViewModel.FavoritesView.SortDescriptions.Clear();
                App.MainViewModel.FavoritesView.SortDescriptions.Add(new SortDescription("DateFavorited", ListSortDirection.Descending));
            }
            else if (BookmarkListsPivots.SelectedIndex == 2)
            {
                ArchiveSortOrder.Text = "recently archived first";
                App.MainViewModel.ArchiveView.SortDescriptions.Clear();
                App.MainViewModel.ArchiveView.SortDescriptions.Add(new SortDescription("DateArchived", ListSortDirection.Descending));
            }
        }
       
        private void Sort_Oldest(object sender, EventArgs e)
        {
            if (BookmarkListsPivots.SelectedIndex == 0)
            {
                ReadingListSortOrder.Text = "oldest added first";
                App.MainViewModel.ReadingListView.SortDescriptions.Clear();
                App.MainViewModel.ReadingListView.SortDescriptions.Add(new SortDescription("DateAdded", ListSortDirection.Ascending));
            }
            else if (BookmarkListsPivots.SelectedIndex == 1)
            {
                FavoritesSortOrder.Text = "oldest favorited first";
                App.MainViewModel.FavoritesView.SortDescriptions.Clear();
                App.MainViewModel.FavoritesView.SortDescriptions.Add(new SortDescription("DateFavorited", ListSortDirection.Ascending));
            }
            else if (BookmarkListsPivots.SelectedIndex == 2)
            {
                ArchiveSortOrder.Text = "oldest archived first";
                App.MainViewModel.ArchiveView.SortDescriptions.Clear();
                App.MainViewModel.ArchiveView.SortDescriptions.Add(new SortDescription("DateArchived", ListSortDirection.Ascending));
            }
        }
    }
}