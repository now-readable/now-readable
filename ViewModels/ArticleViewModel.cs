using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;

using System.IO.IsolatedStorage;
using System.IO;
using System.Collections.Generic;
using System.Linq;

using ReadabilityApi.Models;
using ReadabilityApi.Endpoints;
using ReadabilityApi;

using NowReadable.Storage;
using System.Collections.ObjectModel;
using System.Threading.Tasks;

namespace NowReadable.ViewModels
{
    public class ArticleViewModel : INotifyPropertyChanged
    {
        public ArticleViewModel()
        {
            Bookmark= new Bookmark();
            DataStorage = new DataStorage();
        }

        public bool IsDataLoaded
        {
            get;
            private set;
        }

        public string FavoriteText
        {
            get
            {
                return Bookmark.Favorite ? "unfavorite" : "favorite";
            }
        }
        
        public Uri FavoriteIconUri
        {
            get
            {
                return Bookmark.Favorite ? new Uri("/Assets/favs.removefrom.png", UriKind.Relative) : new Uri("/Assets/favs.addto.png", UriKind.Relative) ;
            }
        }
        
        public string ArchiveText
        {
            get
            {
                return Bookmark.Archive ? "unarchive" : "archive";
            }
        }

        public Uri ArchiveIconUri
        {
            get
            {
                return Bookmark.Archive ? new Uri("/Assets/folder.removefrom.png", UriKind.Relative) : new Uri("/Assets/folder.addto.png", UriKind.Relative) ;
            }
        }

        private Bookmark bookmark;
        public Bookmark Bookmark
        {
            get
            {
                return bookmark;
            }
            set
            {
                bookmark = value;
                NotifyPropertyChanged("Bookmark");
            }
        }

        private Article article;
        public Article Article
        {
            get
            {
                return article;
            }
            set
            {
                article = value;
                NotifyPropertyChanged("Article");
            }
        }

        public MainViewModel MainViewModel
        {
            get
            {
                return App.MainViewModel;
            }
        }

        public string Title
        {
            get
            {
                return Article.Title;
            }
        }

        public string Author
        {
            get
            {
                return Article.Author;
            }
        }

        public string Content
        {
            get
            {
                return Article.Content;
            }
        }
        public DataStorage DataStorage { get; set; }

        /// <summary>
        /// Load the users data.
        /// </summary>
        public async Task LoadData(string bookmarkId)
        {
            Bookmark = MainViewModel.BookmarkList.Bookmarks.First(bookmark => bookmark.Id == bookmarkId);
            IsolatedStorageSettings isss = IsolatedStorageSettings.ApplicationSettings;
            if (isss.Contains("access_token"))
            {
                Article = await DataStorage.LoadArticle(Bookmark);
            }
            this.IsDataLoaded = true;
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