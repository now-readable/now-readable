using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Navigation;
using Microsoft.Phone.Controls;
using Microsoft.Phone.Shell;
using Microsoft.Phone.Tasks;
using Windows.System;

namespace NowReadable.Views
{
    public partial class Settings : PhoneApplicationPage
    {
        public Settings()
        {
            InitializeComponent();

            DataContext = App.SettingsViewModel;
            App.SettingsViewModel.IsUpdated = false;
            App.SettingsViewModel.SaveCompleted += SettingsViewModel_SaveCompleted;
        }

        void SettingsViewModel_SaveCompleted(object sender, ViewModels.SaveEventArgs e)
        {
            if (NavigationService.CanGoBack)
            {
                NavigationService.GoBack();
            }
            else
            {
                NavigationService.Navigate(new System.Uri("/Views/Home.xaml", UriKind.Relative));
            }
        }

        private void ReviewApp_Click(object sender, System.Windows.Input.GestureEventArgs e)
        {
            var task = new MarketplaceReviewTask();
            task.Show();
        }

        private void SendFeedback_Click(object sender, System.Windows.Input.GestureEventArgs e)
        {
            var task = new EmailComposeTask();
            task.To = "nowreadable@outlook.com";
            task.Subject = "Feedback for now readable";
            task.Body = "Salutations, Now Readable!";

            task.Show();
        }

        private async void OpenWebsite_Click(object sender, System.Windows.Input.GestureEventArgs e)
        {
            await Launcher.LaunchUriAsync(new Uri("http://www.chumannaji.com"));

        }
    }
}