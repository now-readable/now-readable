using System;
using System.Net;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Navigation;
using Microsoft.Phone.Controls;
using Microsoft.Phone.Shell;

using ReadabilityApi;
using System.Windows.Data;
using System.Globalization;
using System.Windows.Media;
using System.Windows.Controls.Primitives;
using NowReadable.Views;

namespace NowReadable.Views
{
    public partial class Home : PhoneApplicationPage
    {
        public Home()
        {
            InitializeComponent();

            DataContext = App.MainViewModel;
        }

        /// <summary>
        /// Completes user authentication if it had been started.
        /// </summary>
        protected async override void OnNavigatedTo(NavigationEventArgs e)
        {
            App.MainViewModel.CompleteAuth(e.Uri);

            if (!App.MainViewModel.IsDataLoaded)
            {
                await App.MainViewModel.LoadData();
            }
        }
        
        private void ReadingList_Navigate(object sender, System.Windows.Input.GestureEventArgs e)
        {
            NavigationService.Navigate(new Uri("/Views/BookmarkLists.xaml?id=0", UriKind.Relative));
        }

        private void Favorites_Navigate(object sender, System.Windows.Input.GestureEventArgs e)
        {
            NavigationService.Navigate(new Uri("/Views/BookmarkLists.xaml?id=1", UriKind.Relative));
        }

        private void Archive_Navigate(object sender, System.Windows.Input.GestureEventArgs e)
        {
            NavigationService.Navigate(new Uri("/Views/BookmarkLists.xaml?id=2", UriKind.Relative));
        }
        
        /// <summary>
        /// This is to work around the issue where focusing on a text box sets for background back to white.
        /// </summary>
        private void VerifierKey_GotFocus(object sender, RoutedEventArgs e)
        {
            VerifierKey.Background = Resources["PhoneBackgroundBrush"] as Brush;
        }

        private void NewArticleUrl_GotFocus(object sender, RoutedEventArgs e)
        {
            NewArticleUrl.Background = Resources["PhoneBackgroundBrush"] as Brush;
        }

        /// <summary>
        /// Intercepts the enter key so I can submit their verifier key.
        /// </summary>
        private void VerifierKey_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key == System.Windows.Input.Key.Enter)
            {
                if (App.MainViewModel.ManualAuthCommand.CanExecute(null))
                {
                    App.MainViewModel.ManualAuthCommand.Execute(null);
                }
            }
        }

        private void NewArticleUrl_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key == System.Windows.Input.Key.Enter)
            {
                if (App.MainViewModel.AddBookmarkCommand.CanExecute(null))
                {
                    App.MainViewModel.AddBookmarkCommand.Execute(null);
                }
            }
        }

        private void VerfierKey_TextChanged(object sender, TextChangedEventArgs e)
        {
            App.MainViewModel.VerifierKey = VerifierKey.Text;
            if (VerifierKey.Text != null && !VerifierKey.Text.Equals(""))
            {
                App.MainViewModel.ManualAuthCommand.IsEnabled = true;
            }
            else
            {
                App.MainViewModel.ManualAuthCommand.IsEnabled = false;
            }
        }

        private void NewArticleUrl_TextChanged(object sender, TextChangedEventArgs e)
        {
            App.MainViewModel.NewArticleUrl = NewArticleUrl.Text;
            if (App.MainViewModel.IsUserLoggedIn && Uri.IsWellFormedUriString(NewArticleUrl.Text, UriKind.Absolute))
            {
                App.MainViewModel.AddBookmarkCommand.IsEnabled = true;
            }
            else
            {
                App.MainViewModel.AddBookmarkCommand.IsEnabled = false;
            }
        }
    }
    
}