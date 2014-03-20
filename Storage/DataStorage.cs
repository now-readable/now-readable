using Newtonsoft.Json;
using Newtonsoft.Json.Bson;
using NowReadable.ViewModels;
using ReadabilityApi;
using ReadabilityApi.Models;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Windows.Storage;

namespace NowReadable.Storage
{
    public class DataStorage
    {
        /// <summary>
        /// Loads the use from the currently saved file. Seems to have issues with handling when the file doesn't exist.
        /// </summary>
        /// <returns>The user.</returns>
        public async Task<User> LoadUser()
        {
            try
            {
                StorageFolder local = Windows.Storage.ApplicationData.Current.LocalFolder;
                var file = await local.OpenStreamForReadAsync("user");
                using (var bson = new BsonReader(new BinaryReader(file)))
                {
                    var serializer = new JsonSerializer();
                    var user = serializer.Deserialize<User>(bson);
                    return user == null ? new User() : user;
                }
            }
            catch (FileNotFoundException)
            {
                // This is to avoid returning null which could choke some properties.
                return new User();
            }
        }

        /// <summary>
        /// Saves a user to the file.
        /// </summary>
        /// <param name="user">The user to be saved.</param>
        public async void SaveUser(User user)
        {
            StorageFolder local = Windows.Storage.ApplicationData.Current.LocalFolder;
            var file = await local.CreateFileAsync("user", CreationCollisionOption.ReplaceExisting);

            var fileStream = await file.OpenStreamForWriteAsync();
            using (var writer = new BsonWriter(new BinaryWriter(fileStream)))
            {
                var serializer = new JsonSerializer();
                serializer.Serialize(writer, user);
            }
        }

        /// <summary>
        /// Loads the current BookmarkList from disk. Returns bookmarks.
        /// </summary>
        /// <returns>*All* bookmarks.</returns>
        public async Task<BookmarkList> LoadBookmarkList()
        {
            try
            {
                StorageFolder local = Windows.Storage.ApplicationData.Current.LocalFolder;
                var file = await local.OpenStreamForReadAsync("bookmarks");
                using (var bson = new BsonReader(new System.IO.BinaryReader(file)))
                {
                    var serializer = new JsonSerializer();
                    var bookmarkList = serializer.Deserialize<BookmarkList>(bson);
                    bookmarkList.Bookmarks.OrderByDescending(bookmark => bookmark.DateUpdated);
                    return bookmarkList;
                }
            }
            catch (FileNotFoundException)
            {
                return new BookmarkList();
            }
        }

        /// <summary>
        /// Saves the given bookmark list to a file and downloads all the articles in those bookmarks.
        /// </summary>
        /// <param name="readabilityClient">The readability client to use to request the documents.</param>
        /// <param name="bookmarkList">The bookmark list to be saved.</param>
        /// <returns>A simple task to make this awaitable.</returns>
        public async Task SaveBookmarkList(ReadabilityClient readabilityClient, BookmarkList masterList)
        {
            StorageFolder local = Windows.Storage.ApplicationData.Current.LocalFolder;

            var file = await local.CreateFileAsync("bookmarks", CreationCollisionOption.ReplaceExisting);
            var fileStream = await file.OpenStreamForWriteAsync();
            using (var writer = new BsonWriter(new BinaryWriter(fileStream)))
            {
                var serializer = new JsonSerializer();
                serializer.Serialize(writer, masterList);
            }
        }

        /// <summary>
        /// Merges the two lists and downloads their articles.
        /// </summary>
        /// <param name="readabilityClient">The readabilty client to use to request the documents.</param>
        /// <param name="masterList">The list that will contain the final list of bookmarks.</param>
        /// <param name="bookmarkList">The bookmark list to be saved.</param>
        /// <returns>An awaitable task.</returns>
        public async Task MergeSaveBookmarkList(MainViewModel mainViewModel, BookmarkList bookmarkList)
        {
            mainViewModel.ProgressValue = 0;
            mainViewModel.ProgressIndeterminate = false;
            mainViewModel.ProgressText = "Syncing...";

            StorageFolder local = Windows.Storage.ApplicationData.Current.LocalFolder;

            var tasks = new List<Task>();
            foreach (var bookmark in bookmarkList.Bookmarks)
            {
                var saveCompleteAction = new Action(() =>
                {
                    int position = mainViewModel.BookmarkList.Bookmarks.IndexOf(bookmark);
                    mainViewModel.BookmarkList.Bookmarks.Remove(bookmark);
                    if (position == -1)
                    {
                        mainViewModel.BookmarkList.Bookmarks.Add(bookmark);
                    }
                    else
                    {
                        mainViewModel.BookmarkList.Bookmarks.Insert(position, bookmark);
                    }
                    mainViewModel.ProgressValue += ((double)1 / (double)bookmarkList.Bookmarks.Count);
                });
                tasks.Add(SaveArticle(bookmark, mainViewModel.ReadabilityClient, local, saveCompleteAction));
            }

            await Task.WhenAll(tasks);

            mainViewModel.ProgressIndeterminate = true;
            mainViewModel.ProgressText = "Almost done...";
            await SaveBookmarkList(mainViewModel.ReadabilityClient, mainViewModel.BookmarkList);
        }

        /// <summary>
        /// Deletes the given BookmarkList and all contained articles from disk.
        /// </summary>
        /// <param name="bookmarkList">The bookmark list to be deleted.</param>
        public async Task DeleteBookmarkList(BookmarkList masterList, BookmarkList bookmarkList)
        {
            StorageFolder local = Windows.Storage.ApplicationData.Current.LocalFolder;
            foreach (var bookmark in bookmarkList.Bookmarks)
            {
                if (masterList.Bookmarks.Contains(bookmark))
                {
                    var targetBookmark = masterList.Bookmarks.First(mlBookmark => bookmark.Id == mlBookmark.Id);
                    var folder = await local.CreateFolderAsync(targetBookmark.Article.Id, CreationCollisionOption.OpenIfExists);

                    masterList.Bookmarks.Remove(bookmark);
                    await folder.DeleteAsync();
                }
            }

            
            var file = await local.CreateFileAsync("bookmarks", CreationCollisionOption.ReplaceExisting);
            var fileStream = await file.OpenStreamForWriteAsync();
            using (var writer = new BsonWriter(new BinaryWriter(fileStream)))
            {
                var serializer = new JsonSerializer();
                serializer.Serialize(writer, masterList);
            }
        }

        /// <summary>
        /// Gets a whole article from the saved version.
        /// </summary>
        /// <param name="bookmark">The bookmark object has has details on the article.</param>
        /// <returns>The article that was requested.</returns>
        public async Task<Article> LoadArticle(Bookmark bookmark)
        {
            StorageFolder local = Windows.Storage.ApplicationData.Current.LocalFolder;
            var folder = await local.GetFolderAsync(bookmark.Article.Id);
            var file = await folder.OpenStreamForReadAsync(bookmark.Article.Id);
            using (var bson = new BsonReader(new BinaryReader(file)))
            {
                var serializer = new JsonSerializer();
                return serializer.Deserialize<Article>(bson);
            }
        }

        /// <summary>
        /// Saves a single article and updates the progress indicator when done.
        /// </summary>
        /// <param name="bookmark">The bookmark that has information about the article.</param>
        /// <param name="readabilityClient">The client to use for connection.</param>
        /// <param name="local">The folder representing the user's local storage.</param>
        /// <param name="saveCompleteAction">An callback when save is complete.</param>
        /// <returns>An awaitable task.</returns>
        async Task SaveArticle(Bookmark bookmark, ReadabilityClient readabilityClient, StorageFolder local, Action saveCompleteAction)
        {
            var article = await readabilityClient.ArticleEndpoint.GetArticle(bookmark);
            var folder = await local.CreateFolderAsync(bookmark.Article.Id, CreationCollisionOption.OpenIfExists);
            try
            {
                var articleFile = await folder.CreateFileAsync(bookmark.Article.Id, CreationCollisionOption.FailIfExists);
                var articleStream = await articleFile.OpenStreamForWriteAsync();
                using (var innerWriter = new BinaryWriter(articleStream))
                {
                    var bson = new BsonWriter(innerWriter);
                    var innerSerializer = new JsonSerializer();
                    innerSerializer.Serialize(bson, article);
                }
            }
            catch
            {
                //swallow exception?
            }

            if (null != saveCompleteAction)
                saveCompleteAction();
        }

        /// <summary>
        /// Syncs the all the users data from a given DateTime.
        /// </summary>
        /// <param name="readabilityClient">The client to use to make the requests.</param>
        /// <param name="syncDateTime">The date and time from when to sync.</param>
        public async Task Sync(MainViewModel mainViewModel, DateTime syncDateTime)
        {
            mainViewModel.ProgressText = "Starting sync...";
            mainViewModel.ProgressIndeterminate = true;
            mainViewModel.ProgressVisible = true;

            var conditions = new Conditions { UpdatedSince = syncDateTime };
            var updateList = await mainViewModel.ReadabilityClient.BookmarkListEndpoint.GetAllBookmarks(conditions);
            await MergeSaveBookmarkList(mainViewModel, updateList);

            conditions.OnlyDeleted = 1;
            var deleteList = await mainViewModel.ReadabilityClient.BookmarkListEndpoint.GetAllBookmarks(conditions);
            if (!syncDateTime.Equals(new DateTime()))
            {
                await DeleteBookmarkList(mainViewModel.BookmarkList, deleteList);
            }

            mainViewModel.ProgressVisible = false;
        }
    }
}
