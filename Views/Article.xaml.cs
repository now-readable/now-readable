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
using System.Windows.Media;
using System.Windows.Controls.Primitives;

namespace NowReadable.Views
{
    public partial class Article : PhoneApplicationPage
    {
        public Article()
        {
            InitializeComponent();
            ArticleViewModel = new ArticleViewModel();

            DataContext = ArticleViewModel;
            (ApplicationBar.Buttons[0] as ApplicationBarIconButton).Text = ArticleViewModel.FavoriteText;
            (ApplicationBar.Buttons[0] as ApplicationBarIconButton).IconUri = ArticleViewModel.FavoriteIconUri;

            ArticleView.ScriptNotify += ArticleView_ScriptNotify;
        }

        private int _visibleHeight = 0;
        private int _scrollHeight = 0;
 
        void ArticleView_ScriptNotify(object sender, NotifyEventArgs e)
        {
            // split 
            var parts = e.Value.Split('=');
            if (parts.Length != 2)
            {
                return;
            }

            // parse
            int number = 0;
            if (!int.TryParse(parts[1], out number))
            {
                return;
            }

            // decide what to do
            if (parts[0] == "scrollHeight")
            {
                _scrollHeight = number;
                if (_visibleHeight > 0)
                {
                    DisplayScrollBar.Maximum = _scrollHeight - _visibleHeight;
                }
            }
            else if (parts[0] == "clientHeight")
            {
                _visibleHeight = number;
                if (_scrollHeight > 0)
                {
                    DisplayScrollBar.Maximum = _scrollHeight - _visibleHeight;
                }
            }
            else if (parts[0] == "scrollTop")
            {
                DisplayScrollBar.Value = number;
            }
        }
        
        /// <summary>
        /// Completes user authentication if it had been started.
        /// </summary>
        protected override async void OnNavigatedTo(NavigationEventArgs e)
        {
            string bookmarkId = "";
            if (App.MainViewModel.IsDataLoaded && (!ArticleViewModel.IsDataLoaded || App.SettingsViewModel.IsUpdated))
            {
                if (NavigationContext.QueryString.TryGetValue("bookmarkId", out bookmarkId))
                {
                    //DisplayScrollBar.Foreground = ColorToBrush(255, App.SettingsViewModel.Themes[App.SettingsViewModel.CurrentTheme].ForegroundColor);
                    LayoutRoot.Background = ColorToBrush(255, App.SettingsViewModel.Themes[App.SettingsViewModel.CurrentTheme].BackgroundColor);
                    DisplayScrollBar.Background = ColorToBrush(255, App.SettingsViewModel.Themes[App.SettingsViewModel.CurrentTheme].ForegroundColor);

                    await ArticleViewModel.LoadData(bookmarkId);

                    ArticleView.LoadCompleted += ArticleView_LoadCompleted;
                    ArticleView.NavigateToString(Head + 
                        "<body>" + 
                        "<h1>" + ArticleViewModel.Title + "</h1>" +
                        "<h3>" + ArticleViewModel.Author + "</h3>" + 
                        ArticleViewModel.Content + 
                        Script +
                        "</body>");
                }
            }
        }

        private string Head
        {
            get
            {
                var settingsViewModel = App.SettingsViewModel;
                var backgroundColor = settingsViewModel.Themes[settingsViewModel.CurrentTheme].BackgroundColor;
                var foregroundColor = settingsViewModel.Themes[settingsViewModel.CurrentTheme].ForegroundColor;
                var fontFamily = settingsViewModel.Typefaces[settingsViewModel.CurrentTypeface];
                var fontSize = settingsViewModel.CurrentFontSize;
                return
                "<head>" +
                "<meta name='viewport' content='width=device-width, initial-scale=1, user-scalable=0'>" +
                "<style type='text/css'>" +
                "@-ms-viewport {width:auto!important}" + 
                "body { background-color: " + backgroundColor + "; color: " + foregroundColor + "; font-family: " + fontFamily + "; font-size: " + (float)fontSize/(10) + "em; line-height: 1.5; word-wrap: break-word; overflow: scroll; }" +
                "li { margin-left: -20px; margin-bottom: 8px; }" +
                "h1 { font-size: 1.6em; font-weight: normal; line-height: 1 }" +
                "h2 { font-size: 1.5em; font-weight: normal; line-height: 1 }" +
                "h3 { font-size: 1.4em; font-weight: normal; line-height: 1 }" +
                "h4 { font-size: 1.3em; font-weight: normal; line-height: 1 }" +
                "h5 { font-size: 1.3em; font-weight: normal; line-height: 1 }" +
                "h6 { font-size: 1.3em; font-weight: normal; line-height: 1 }" +
                "a, a:link, a:visited, a:hover, a:active { color: " + foregroundColor + "; }" +
                "img { width: 100%; }" +
                "</style>" +
                "</head>";
            }
        }

        private string Script
        {
            get
            {
                return 
                "<script type=text/javascript>" +
                "function initialize() {" +
                    "window.external.notify('scrollHeight=' + document.body.scrollHeight.toString());" +
                    "window.external.notify('clientHeight=' + document.body.clientHeight.toString());" +
                    "window.onscroll = onScroll;" +
                "}" +
                "function onScroll(e) {" +
                    "var scrollPosition = document.body.scrollTop;" +
                    "window.external.notify('scrollTop=' + scrollPosition.toString());" +
                "}" +
                "window.onload = initialize;" +
                "</script>";
            }
        }
        
        private ArticleViewModel ArticleViewModel { get; set; }

        private void ArticleView_LoadCompleted(object sender, NavigationEventArgs e)
        {
            ArticleView.Visibility = Visibility.Visible;
        }

        private void Favorite_Click(object sender, EventArgs e)
        {
            App.MainViewModel.ToggleBookmarkFavoriteCommand.Execute(ArticleViewModel.Bookmark);
            (ApplicationBar.Buttons[0] as ApplicationBarIconButton).Text = ArticleViewModel.FavoriteText;
            (ApplicationBar.Buttons[0] as ApplicationBarIconButton).IconUri = ArticleViewModel.FavoriteIconUri;
        }

        private void Archive_Click(object sender, EventArgs e)
        {
            App.MainViewModel.ToggleBookmarkArchiveCommand.Execute(ArticleViewModel.Bookmark);
            (ApplicationBar.Buttons[1] as ApplicationBarIconButton).Text = ArticleViewModel.ArchiveText;
            (ApplicationBar.Buttons[1] as ApplicationBarIconButton).IconUri = ArticleViewModel.ArchiveIconUri;
        }

        private void Delete_Click(object sender, EventArgs e)
        {
            var result = MessageBox.Show("Are you sure you want to delete this article?", "Delete Article?", MessageBoxButton.OKCancel);
            if (result == MessageBoxResult.OK)
            {
                App.MainViewModel.DeleteBookmarkCommand.Execute(ArticleViewModel.Bookmark);
                NavigationService.Navigate(new Uri("/Views/BookmarkLists.xaml?id=0", UriKind.Relative));
                if (NavigationService.CanGoBack)
                {
                    NavigationService.GoBack();
                }
                else
                {
                    NavigationService.Navigate(new Uri("/Views/BookmarkLists.xaml?id=0", UriKind.Relative));
                }
            }
        }

        private void Settings_Click(object sender, EventArgs e)
        {
            NavigationService.Navigate(new Uri("/Views/Settings.xaml", UriKind.Relative));
        }
        
        private void AppBar_Shown(object sender, ApplicationBarStateChangedEventArgs e)
        {
            SystemTray.IsVisible = e.IsMenuVisible;
            (ApplicationBar.Buttons[0] as ApplicationBarIconButton).Text = ArticleViewModel.FavoriteText;
            (ApplicationBar.Buttons[0] as ApplicationBarIconButton).IconUri = ArticleViewModel.FavoriteIconUri;
        }

        public static SolidColorBrush ColorToBrush(byte transparency, string color) // color = "#E7E44D"
        {
            color = color.Replace("#", "");
            return new SolidColorBrush(Color.FromArgb(
                transparency,
                byte.Parse(color.Substring(0, 2), System.Globalization.NumberStyles.HexNumber),
                byte.Parse(color.Substring(2, 2), System.Globalization.NumberStyles.HexNumber),
                byte.Parse(color.Substring(4, 2), System.Globalization.NumberStyles.HexNumber)));
        }

    }
}