using QMunicate.Core.Command;
using QMunicate.Core.DependencyInjection;
using QMunicate.Helper;
using QMunicate.Models;
using Quickblox.Sdk;
using Quickblox.Sdk.Modules.ChatModule.Models;
using Quickblox.Sdk.Modules.UsersModule.Responses;
using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Windows.ApplicationModel.Core;
using Windows.UI.Core;
using Windows.UI.Xaml.Navigation;
using QMunicate.Core.AsyncLock;
using QMunicate.Services;
using QMunicate.ViewModels.PartialViewModels;
using Quickblox.Sdk.Modules.ChatXmppModule.Models;

namespace QMunicate.ViewModels
{
    public class SearchViewModel : ViewModel
    {
        #region Fields

        private const int numberOfItemsPerPage = 20;
        private string searchText;
        private bool isInGlobalSeachMode;
        private readonly AsyncLock localResultsLock = new AsyncLock();
        private readonly AsyncLock globalResultsLock = new AsyncLock();
        private CancellationTokenSource cts;
        private uint currentGlobalResultsPage;

        #endregion

        #region Ctor

        public SearchViewModel()
        {
            GlobalResults = new ObservableCollection<UserViewModel>();
            LocalResults = new ObservableCollection<UserViewModel>();
            OpenLocalCommand = new RelayCommand<UserViewModel>(u => OpenLocalCommandExecute(u));
            OpenGlobalCommand = new RelayCommand<UserViewModel>(OpenGlobalCommandExecute);
            LoadMoreGlobalResultsCommand = new RelayCommand(LoadMoreGlobalResultsCommandExecute, () => !IsLoading);
        }

        #endregion

        #region Properties

        public string SearchText
        {
            get { return searchText; }
            set
            {
                if (Set(ref searchText, value))
                    Search(searchText);
            }
        }

        public bool IsInGlobalSeachMode
        {
            get { return isInGlobalSeachMode; }
            set
            {
                if (Set(ref isInGlobalSeachMode, value))
                    Search(SearchText);
            }
        }

        public ObservableCollection<UserViewModel> GlobalResults { get; set; }

        public ObservableCollection<UserViewModel> LocalResults { get; set; }

        public RelayCommand<UserViewModel> OpenLocalCommand { get; set; }

        public RelayCommand<UserViewModel> OpenGlobalCommand { get; set; }

        public RelayCommand LoadMoreGlobalResultsCommand { get; set; }

        #endregion

        #region Navigation

        public override async void OnNavigatedTo(NavigationEventArgs e)
        {
            QuickbloxClient.ChatXmppClient.OnContactsChanged += async (obj, args) => await Helpers.RunOnTheUiThread(ReloadLocalSearchResults);
            await ReloadLocalSearchResults();
        }

        #endregion

        #region Private methods

        private async Task ReloadLocalSearchResults()
        {
            await LocalSearch("");
        }

        private async void Search(string searchQuery)
        {
            if(isInGlobalSeachMode)
                await GlobalSearch(searchQuery);
            else
                await LocalSearch(searchQuery);
        }

        private async Task GlobalSearch(string searchQuery)
        {
            currentGlobalResultsPage = 1;

            if (string.IsNullOrWhiteSpace(searchQuery))
            {
                await ClearGlobalResults();
                return;
            }

            IsLoading = true;

            if (cts != null)
            {
                cts.Cancel();
                cts.Dispose();
            }

            cts = new CancellationTokenSource();
            var token = cts.Token;

            GlobalResults.Clear();
            await LoadGlobalResults(searchQuery, currentGlobalResultsPage, true, token);

            if (string.IsNullOrEmpty(SearchText))
                await ClearGlobalResults();

            await LoadGlobalResultsImages(0);

            IsLoading = false;
        }

        private async Task ClearGlobalResults()
        {
            using (await globalResultsLock.LockAsync())
            {
                GlobalResults.Clear();
            }
        }

        private async Task LocalSearch(string searchQuery)
        {
            IsLoading = true;
            using (await localResultsLock.LockAsync())
            {
                LocalResults.Clear();
                if (string.IsNullOrEmpty(searchQuery))
                {
                    foreach (Contact contact in QuickbloxClient.ChatXmppClient.Contacts)
                    {
                        LocalResults.Add(UserViewModel.FromContact(contact));
                    }
                }
                else
                {
                    foreach (Contact contact in QuickbloxClient.ChatXmppClient.Contacts)
                    {
                        var user = await ServiceLocator.Locator.Get<ICachingQuickbloxClient>().GetUserById(contact.UserId);
                        if (user != null && user.FullName.IndexOf(searchQuery, StringComparison.OrdinalIgnoreCase) >= 0)
                        {
                            LocalResults.Add(UserViewModel.FromContact(contact));
                        }
                    }
                }

                await FixLocalResultsNames();
                await SetPresences();
                await LoadLocalResultsImages();
            }
            IsLoading = false;
        }

        /// <summary>
        /// Loads images of users in Global search. 
        /// </summary>
        /// <param name="startIndex">Index to start loading images. Is used in incremental loading.</param>
        /// <returns></returns>
        private async Task LoadGlobalResultsImages(int startIndex)
        {
            if (startIndex < 0 || startIndex >= GlobalResults.Count) return;

            using (await globalResultsLock.LockAsync())
            {
                for (int i = startIndex; i < GlobalResults.Count; i++)
                {
                    var userVm = GlobalResults[i];
                    if (userVm.ImageUploadId.HasValue)
                    {
                        var imagesService = ServiceLocator.Locator.Get<IImageService>();
                        userVm.Image = await imagesService.GetPrivateImage(userVm.ImageUploadId.Value, 100);
                    }
                }
            }
        }

        private async Task FixLocalResultsNames()
        {
            var cachingQbClient = ServiceLocator.Locator.Get<ICachingQuickbloxClient>();
            foreach (UserViewModel userVm in LocalResults)
            {
                var user = await cachingQbClient.GetUserById(userVm.UserId);
                if (user != null && !string.IsNullOrEmpty(user.FullName))
                {
                    userVm.FullName = user.FullName;
                }
            }
        }

        private async Task SetPresences()
        {
            var cachingQbClient = ServiceLocator.Locator.Get<ICachingQuickbloxClient>();
            foreach (var localResult in LocalResults)
            {
                localResult.IsOnline = QuickbloxClient.ChatXmppClient.Presences.Any(p => p.UserId == localResult.UserId && (p.PresenceType == PresenceType.None || p.PresenceType == PresenceType.Subscribed));
                var user = await cachingQbClient.GetUserById(localResult.UserId);
                if (user?.LastRequestAt != null)
                {
                    localResult.LastActive = user.LastRequestAt.Value;
                }
            }
        }

        private async Task LoadLocalResultsImages()
        {
            var cachingQbClient = ServiceLocator.Locator.Get<ICachingQuickbloxClient>();
            var imagesService = ServiceLocator.Locator.Get<IImageService>();
            foreach (UserViewModel userVm in LocalResults)
            {
                var user = await cachingQbClient.GetUserById(userVm.UserId);
                if (user != null && user.BlobId.HasValue)
                {
                    userVm.ImageUploadId = user.BlobId;
                    userVm.Image = await imagesService.GetPrivateImage(user.BlobId.Value, 100);
                }
            }
        }

        private async Task OpenLocalCommandExecute(UserViewModel user)
        {
            var dialogsManager = ServiceLocator.Locator.Get<IDialogsManager>();
            var userDialog = dialogsManager.Dialogs.FirstOrDefault(d => d.DialogType == DialogType.Private && d.OccupantIds.Contains(user.UserId));
            if (userDialog != null)
            {
                NavigationService.Navigate(ViewLocator.Chat, new ChatNavigationParameter { Dialog = userDialog });
            }
            else
            {
                var response = await QuickbloxClient.ChatClient.CreateDialogAsync(user.FullName, DialogType.Private, user.UserId.ToString());
                if (response.StatusCode == HttpStatusCode.Created)
                {
                    var dialogVm = DialogViewModel.FromDialog(response.Result);
                    dialogVm.Image = user.Image;
                    dialogVm.PrivatePhotoId = user.ImageUploadId;
                    dialogVm.Name = user.FullName;
                    dialogsManager.Dialogs.Insert(0, dialogVm);
                    NavigationService.Navigate(ViewLocator.Chat, new ChatNavigationParameter { Dialog = dialogVm });
                }
            }
        }

        private void OpenGlobalCommandExecute(UserViewModel user)
        {
            NavigationService.Navigate(ViewLocator.SendRequest, user);
        }

        private async void LoadMoreGlobalResultsCommandExecute()
        {
            if (IsLoading) return;

            IsLoading = true;


            cts = new CancellationTokenSource();
            var token = cts.Token;

            currentGlobalResultsPage++;

            await LoadGlobalResults(SearchText, currentGlobalResultsPage, false, token);

            IsLoading = false;
        }

        private async Task LoadGlobalResults(string searchQuery, uint currentPage, bool firstLoad, CancellationToken token)
        {
            var response = await QuickbloxClient.UsersClient.GetUserByFullNameAsync(searchQuery, currentPage, numberOfItemsPerPage, token);
            if (response.StatusCode == HttpStatusCode.OK && !token.IsCancellationRequested)
            {
                int currentUserId = SettingsManager.Instance.ReadFromSettings<int>(SettingsKeys.CurrentUserId);
                using (await globalResultsLock.LockAsync())
                {
                    foreach (UserResponse item in response.Result.Items.Where(i => i.User.Id != currentUserId))
                    {
                        ServiceLocator.Locator.Get<ICachingQuickbloxClient>().ManuallyUpdateUserInCache(item.User);
                        GlobalResults.Add(UserViewModel.FromUser(item.User));
                    }
                }

                await LoadGlobalResultsImages(GlobalResults.Count - response.Result.Items.Length - 1);
            }
            else if(firstLoad)
            {
                await ClearGlobalResults();
            }
        }

        #endregion

    }
}
