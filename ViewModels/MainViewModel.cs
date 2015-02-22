using NowReadable.Utilities.Commands;
using NowReadable.Storage;
using ReadabilityApi;
using ReadabilityApi.Models;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO.IsolatedStorage;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using Microsoft.Phone.Shell;
using System.Reactive;
using System.Reactive.Linq;
using System.Windows.Data;

namespace NowReadable.ViewModels
{
    public partial class MainViewModel : INotifyPropertyChanged
    {
        #region ViewModel Properties
        /// <summary>
        /// True when the MainViewModel has finished loading Data.
        /// </summary>
        public bool IsDataLoaded
        {
            get;
            private set;
        }

        /// <summary>
        /// True when using the verifier key method of authenitication, null when we don't know which method yet.
        /// </summary>
        public bool? IsManualAuth
        {
            get;
            private set;
        }

        /// <summary>
        /// True when a user has logged in.
        /// </summary>
        public bool IsUserLoggedIn
        {
            get
            {
                IsolatedStorageSettings isss = IsolatedStorageSettings.ApplicationSettings;
                return isss.Contains("access_token");
            }
        }
        
        private string _verifierKey;
        /// <summary>
        /// The verification key that readability provided to the user.
        /// </summary>
        public string VerifierKey
        {
            get
            {
                return _verifierKey;
            }
            set
            {
                _verifierKey = value;
                NotifyPropertyChanged("VerifierKey");
            }
        }

        private User _user;
        /// <summary>
        /// The user that is currently signed in.
        /// </summary>
        public User User
        {
            get
            {
                return _user;
            }
            set
            {
                if (value != _user)
                {
                    _user = value;
                    NotifyPropertyChanged("User");
                    NotifyPropertyChanged("IsUserLoggedIn");
                }
            }
        }

        private string _newArticleUrl;
        /// <summary>
        /// The url for the article to be added.
        /// </summary>
        public string NewArticleUrl
        {
            get
            {
                return _newArticleUrl;
            }
            set
            {
                if (value != _newArticleUrl)
                {
                    _newArticleUrl = value;
                    NotifyPropertyChanged("NewArticleUrl");
                }
            }
        }

        /// <summary>
        /// List of all bookmarks in the application.
        /// </summary>
        public BookmarkList BookmarkList { get; set; }

        /// <summary>
        /// Count of how many items are in the reading list.
        /// </summary>
        public int ReadingListCount
        {
            get
            {
                return BookmarkList.Bookmarks.Count(bookmark => !bookmark.Archive);
            }
        }
        
        /// <summary>
        /// Count of how many items are in the favorites.
        /// </summary>
        public int FavoritesCount
        {
            get
            {
                return BookmarkList.Bookmarks.Count(bookmark => bookmark.Favorite);
            }
        }

        /// <summary>
        /// Count of how many items are in the archives.
        /// </summary>
        public int ArchiveCount
        {
            get
            {
                return BookmarkList.Bookmarks.Count(bookmark => bookmark.Archive);
            }
        }

        private bool _progressVisible;
        /// <summary>
        /// True when the progress bar is being displayed.
        /// </summary>
        public bool ProgressVisible
        {
            get
            {
                return _progressVisible;
            }
            set
            {
                _progressVisible = value;
                NotifyPropertyChanged("ProgressVisible");
            }
        }

        private bool _progressIndeterminate;
        /// <summary>
        /// True when the progress bar has indeterminate value.
        /// </summary>
        public bool ProgressIndeterminate
        {
            get
            {
                return _progressIndeterminate;
            }
            set
            {
                _progressIndeterminate = value;
                NotifyPropertyChanged("ProgressIndeterminate");
            }
        }

        private double _progressValue;
        /// <summary>
        /// The progress bar's current value.
        /// </summary>
        public double ProgressValue
        {
            get
            {
                return _progressValue;
            }
            set
            {
                _progressValue = value;
                NotifyPropertyChanged("ProgressValue");
            }
        }

        private string _progressText;
        /// <summary>
        /// The text to be displayed in the progress bar.
        /// </summary>
        public string ProgressText
        {
            get
            {
                return _progressText;
            }
            set
            {
                _progressText = value;
                NotifyPropertyChanged("ProgressText");
            }
        }

        /// <summary>
        /// The readability client for talking to the readability service.
        /// </summary>
        public ReadabilityClient ReadabilityClient { get; set; }
        
        /// <summary>
        /// The data storage object, for syncing and saving.
        /// </summary>
        public DataStorage DataStorage { get; set; }

        private string ApiBaseUrl;
        private string ConsumerKey;
        private string ConsumerSecret;
        #endregion

        #region Collections

        /// <summary>
        /// The items that should appear in the reading list view.
        /// </summary>
        public ObservableCollection<Bookmark> ReadingList
        {
            get
            {
                return new ObservableCollection<Bookmark>(BookmarkList.Bookmarks.Where(bookmark => !bookmark.Archive)
                    .OrderBy(bookmark => bookmark.DateUpdated));
            }
        }

        private CollectionViewSource _readingListView;
        public CollectionViewSource ReadingListView
        {
            get
            {
                if (_readingListView == null)
                {
                    _readingListView = new CollectionViewSource();
                    _readingListView.Source = BookmarkList.Bookmarks;
                    _readingListView.SortDescriptions.Add(new SortDescription("DateAdded", ListSortDirection.Descending));
                    _readingListView.View.Filter = bookmark =>
                        {
                            return !(bookmark as Bookmark).Archive;
                        };
                }
                return _readingListView;
            }
        }

        private CollectionViewSource _favoritesView;
        public CollectionViewSource FavoritesView
        {
            get
            {
                if (_favoritesView == null)
                {
                    _favoritesView = new CollectionViewSource();
                    _favoritesView.Source = BookmarkList.Bookmarks;
                    _favoritesView.SortDescriptions.Add(new SortDescription("DateFavorited", ListSortDirection.Descending));
                    _favoritesView.View.Filter = bookmark =>
                        {
                            return (bookmark as Bookmark).Favorite;
                        };
                }
                return _favoritesView;
            }
        }

        private CollectionViewSource _archiveView;
        public CollectionViewSource ArchiveView
        {
            get
            {
                if (_archiveView == null)
                {
                    _archiveView = new CollectionViewSource();
                    _archiveView.Source = BookmarkList.Bookmarks;
                    _archiveView.SortDescriptions.Add(new SortDescription("DateArchived", ListSortDirection.Descending));
                    _archiveView.View.Filter = bookmark =>
                        {
                            return (bookmark as Bookmark).Archive;
                        };
                }
                return _archiveView;
            }
        }
        #endregion
     
        #region Bindable Commands
        /// <summary>
        /// Favorites the given bookmark.
        /// </summary>
        private NetworkEnabledCommand _toggleBookmarkFavoriteCommand;
        public NetworkEnabledCommand ToggleBookmarkFavoriteCommand {
            get
            {
                if (_toggleBookmarkFavoriteCommand == null)
                {
                    _toggleBookmarkFavoriteCommand = new NetworkEnabledCommand(async (object parameter) =>
                    {
                        Bookmark bookmark = parameter as Bookmark;
                        await RunBookmarkAction("Favoriting...", async () =>
                        {
                            await ReadabilityClient.BookmarkEndpoint.ToggleFavorite(bookmark);
                            BookmarkList.Bookmarks.First(newBookmark => newBookmark.Id == bookmark.Id).Favorite = !bookmark.Favorite;
                            await DataStorage.SaveBookmarkList(ReadabilityClient, BookmarkList);
                            NotifyReadingListUpdated();
                        });
                    });
                }
                return _toggleBookmarkFavoriteCommand;
            }
        }

        /// <summary>
        /// Archives the given bookmark.
        /// </summary>
        private NetworkEnabledCommand _toggleBookmarkArchiveCommand;
        public NetworkEnabledCommand ToggleBookmarkArchiveCommand {
            get
            {
                if (_toggleBookmarkArchiveCommand == null)
                {
                    _toggleBookmarkArchiveCommand = new NetworkEnabledCommand(async (object parameter) =>
                    {
                        Bookmark bookmark = parameter as Bookmark;
                        await RunBookmarkAction("Archiving...", async () =>
                        {
                            await ReadabilityClient.BookmarkEndpoint.ToggleArchive(bookmark);
                            BookmarkList.Bookmarks.First(newBookmark => newBookmark.Id == bookmark.Id).Archive = !bookmark.Archive;
                            await DataStorage.SaveBookmarkList(ReadabilityClient, BookmarkList);
                            NotifyReadingListUpdated();
                        });
                    });
                }
                return _toggleBookmarkArchiveCommand;
            }
        }

        /// <summary>
        /// Adds the given bookmark.
        /// </summary>
        private NetworkEnabledCommand _addBookmarkCommand;
        public NetworkEnabledCommand AddBookmarkCommand {
            get
            {
                if (_addBookmarkCommand == null)
                {
                    _addBookmarkCommand = new NetworkEnabledCommand(async (object parameter) =>
                    {
                        Bookmark bookmark = parameter as Bookmark;
                        await RunBookmarkAction("Adding...", async () =>
                        {
                            await ReadabilityClient.BookmarkEndpoint.AddBookmark(NewArticleUrl);
                            SyncCommand.Execute(null);
                            NewArticleUrl = "";
                            NotifyReadingListUpdated();
                        });
                    });
                    _addBookmarkCommand.IsEnabled = false;
                }
                return _addBookmarkCommand;
            }
        }

        /// <summary>
        /// Deletes the given bookmark.
        /// </summary>
        private NetworkEnabledCommand _deleteBookmarkCommand;
        public NetworkEnabledCommand DeleteBookmarkCommand {
            get
            {
                if (_deleteBookmarkCommand == null)
                {
                    _deleteBookmarkCommand = new NetworkEnabledCommand(async (object parameter) =>
                    {
                        Bookmark bookmark = parameter as Bookmark;
                        await RunBookmarkAction("Deleting...", async () =>
                        {
                            await ReadabilityClient.BookmarkEndpoint.Delete(bookmark);
                            BookmarkList.Bookmarks.Remove(bookmark);
                            await DataStorage.SaveBookmarkList(ReadabilityClient, BookmarkList);
                            NotifyReadingListUpdated();
                        });
                    });
                }
                return _deleteBookmarkCommand;
            }
        }

        /// <summary>
        /// Begins syncing.
        /// </summary>
        private NetworkEnabledCommand _syncCommand;
        public NetworkEnabledCommand SyncCommand {
            get
            {
                if (_syncCommand == null)
                {
                    _syncCommand = new NetworkEnabledCommand(async (object parameter) =>
                    {
                        IsolatedStorageSettings isss = IsolatedStorageSettings.ApplicationSettings;

                        SyncCommand.IsEnabled = false;
                        BookmarkList.Bookmarks.CollectionChanged += Bookmarks_CollectionChanged;

                        DateTime lastSync;
                        isss.TryGetValue<DateTime>("last_sync", out lastSync);
                        await DataStorage.Sync(this, lastSync);
                        SyncCommand.IsEnabled = true;
                        isss.Remove("last_sync");
                        isss.Add("last_sync", DateTime.Now);
                        isss.Save();

                        BookmarkList.Bookmarks.CollectionChanged -= Bookmarks_CollectionChanged;
                        Bookmarks_CollectionChanged(null, null);
                        NotifyReadingListUpdated();
                    });
                }
                return _syncCommand;
            }
        }
        
        /// <summary>
        /// Handles starting the auth by launching a browser to readability.
        /// </summary>
        private NetworkEnabledCommand _beginAuthCommand;
        public NetworkEnabledCommand BeginAuthCommand
        {
            get
            {
                if (_beginAuthCommand == null)
                {
                    _beginAuthCommand = new NetworkEnabledCommand((object parameter) =>
                    {
                        ReadabilityClient.UserEndpoint.BeginAuth();
                    });
                }
                return _beginAuthCommand;
            }
        }

        /// <summary>
        /// Handles finishing the auth when the user enters their verification key.
        /// </summary>
        private NetworkEnabledCommand _manualAuthCommand;
        public NetworkEnabledCommand ManualAuthCommand
        {
            get
            {
                if (_manualAuthCommand == null)
                {
                    _manualAuthCommand = new NetworkEnabledCommand(async (object parameter) =>
                    {
                        var user = await ReadabilityClient.UserEndpoint.CompleteAuth(VerifierKey);
                        if (user == null)
                        {
                            MessageBox.Show("Verifier key appears to be incorrect. Please try again.");
                        }
                        else
                        {
                            User = user;
                            DataStorage.SaveUser(user);

                            if (SyncCommand.CanExecute(null))
                            {
                                SyncCommand.Execute(null);
                            }
                        }
                    });
                    _manualAuthCommand.IsEnabled = false;
                }
                return _manualAuthCommand;
            }
        }
        
        /// <summary>
        /// Restarts the auth process (in case something went wrong)
        /// </summary>
        private SimpleCommand _restartAuthCommand;
        public SimpleCommand RestartAuthCommand
        {
            get
            {
                if (_restartAuthCommand == null)
                {
                    _restartAuthCommand = new SimpleCommand((object parameter) =>
                    {
                        IsolatedStorageSettings isss = IsolatedStorageSettings.ApplicationSettings;
                        isss.Remove("oauth_token");
                        isss.Remove("oauth_token_secret");
                        isss.Remove("access_token");
                        isss.Remove("access_token_secret");
                        isss.Save();
                        IsManualAuth = false;
                        NotifyPropertyChanged("User");
                        NotifyPropertyChanged("IsManualAuth");
                        NotifyPropertyChanged("IsUserLoggedIn");
                    });
                }
                return _restartAuthCommand;
            }
        }
        #endregion

        /// <summary>
        /// Initialize the ApiKeys. This is done in a seperate partial class that is removed from SourceControl.
        /// </summary>
        partial void InitSecretProperties();

        public MainViewModel()
        {
            ApiBaseUrl = "";
            ConsumerKey = "";
            ConsumerSecret = "";

            ProgressVisible = false;
            DataStorage = new DataStorage();
            BookmarkList = new BookmarkList();

            ReadabilityClient = new ReadabilityApi.ReadabilityClient(ApiBaseUrl, ConsumerKey, ConsumerSecret);

            Observable.Buffer(Observable.FromEventPattern(this, "ReadingListUpdated").Throttle(TimeSpan.FromSeconds(1)), 1)
                .Subscribe(e =>
                {
                    ShellTile tile = ShellTile.ActiveTiles.First();
                    if (tile != null && ReadingList.Count > 0) //Do nothing if there's no tile pinned or there are no items in the list.
                    {
                        var firstArticleInReadingList = ReadingList.First().Article;

                        IconicTileData TileData = new IconicTileData()
                        {
                            Title = "Now Readable",
                            Count = ReadingListCount,
                            WideContent1 = firstArticleInReadingList.Title,
                            WideContent2 = firstArticleInReadingList.Excerpt.Substring(0, 100),
                            WideContent3 = firstArticleInReadingList.Author,
                            SmallIconImage = new Uri("Assets/Tiles/SmallIconImage.png", UriKind.Relative),
                            IconImage = new Uri("Assets/Tiles/IconImage.png", UriKind.Relative),
                            BackgroundColor = System.Windows.Media.Colors.Red
                        };

                        tile.Update(TileData);
                    }
                });
        }

        /// <summary>
        /// Complete the authentication process if we're redirected by the oauth callback.
        /// </summary>
        /// <param name="uri"></param>
        public async void CompleteAuth(Uri uri)
        {
            // Only run this function if we don't have an access token.
            IsolatedStorageSettings isss = IsolatedStorageSettings.ApplicationSettings;
            if (!isss.Contains("access_token"))
            {
                SyncCommand.IsEnabled = false;
                /*
                 * Set the auth method to null (neither manual nor automatic if we are on a callback.
                 * The reason is that i don't want the page to flash the non-auth when you're redirected from the login page.
                 * This makes sure that neither enter verifier nor start auth process is shown when you just started.
                 * This also means that if getting an access token fails, the user won't have any recourse. I need to fix that.
                 * Potentially by having the auth method return true/false? I can do that now that I can await Executes.
                */
                if (uri.OriginalString.Contains("oauth_callback"))
                {
                    IsManualAuth = null;
                    var user = await ReadabilityClient.UserEndpoint.CompleteAuth(uri);
                    if (user != null)
                    {
                        User = user;
                        DataStorage.SaveUser(user);
                        await LoadData();

                        SyncCommand.IsEnabled = true;
                        if (SyncCommand.CanExecute(null))
                        {
                            SyncCommand.Execute(null);
                        }
                    }
                }
                else
                {
                    // If we are not in the callback, then either we're just starting auth or we're doing a manual auth. 
                    IsManualAuth = isss.Contains("oauth_token");
                }
                NotifyPropertyChanged("IsManualAuth");
            }
        }

        /// <summary>
        /// Load the users data.
        /// </summary>
        public async Task LoadData()
        {
            IsolatedStorageSettings isss = IsolatedStorageSettings.ApplicationSettings;
            if (isss.Contains("access_token"))
            {
                // When the application launches, there's no User object, so we have to go and fetch it.
                if (User == null)
                    User = await DataStorage.LoadUser();

                BookmarkList = await DataStorage.LoadBookmarkList();
                Bookmarks_CollectionChanged(null, null);
            }
            this.IsDataLoaded = true;
        }

        private void Bookmarks_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            NotifyPropertyChanged("ReadingList");
            NotifyPropertyChanged("Favorites");
            NotifyPropertyChanged("Archive");

            NotifyPropertyChanged("ReadingListCount");
            NotifyPropertyChanged("FavoritesCount");
            NotifyPropertyChanged("ArchiveCount");
        }

        /// <summary>
        /// Run an action that manages the indicators around bookmark actions.
        /// </summary>
        /// <param name="notificationText">The text to show in the progress indicator.</param>
        /// <param name="func">The function to run.</param>
        /// <returns>An awaitable task.</returns>
        private async Task RunBookmarkAction(string notificationText, Func<Task> func)
        {
            ProgressText = notificationText;
            ProgressIndeterminate = true;
            ProgressVisible = true;
            await func();
            Bookmarks_CollectionChanged(null, null);
            ProgressVisible = false;
        }

        public event EventHandler ReadingListUpdated;

        private void NotifyReadingListUpdated()
        {
            EventHandler handler = ReadingListUpdated;
            if (null != handler)
            {
                handler(this, null);
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        private void NotifyPropertyChanged(String propertyName)
        {
            PropertyChangedEventHandler handler = PropertyChanged;
            if (null != handler)
            {
                handler(this, new PropertyChangedEventArgs(propertyName));
            }
        }
    }
}