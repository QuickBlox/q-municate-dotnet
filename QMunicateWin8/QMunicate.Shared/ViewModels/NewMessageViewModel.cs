using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Xaml.Navigation;
using QMunicate.Core.Command;
using QMunicate.Core.DependencyInjection;
using QMunicate.Helper;
using QMunicate.Models;
using QMunicate.Services;
using QMunicate.ViewModels.PartialViewModels;
using Quickblox.Sdk.Modules.ChatModule.Models;
using Quickblox.Sdk.Modules.ChatXmppModule.Models;

namespace QMunicate.ViewModels
{
    public class NewMessageViewModel : ViewModel
    {
        private string searchText;

        #region Ctor

        public NewMessageViewModel()
        {
            Contacts = new ObservableCollection<UserViewModel>();
            CreateGroupCommand = new RelayCommand(CreateGroupCommandExecute, () => !IsLoading);
            OpenContactCommand = new RelayCommand<UserViewModel>(u => OpenContactCommandExecute(u), u => !IsLoading);
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

        public ObservableCollection<UserViewModel> Contacts { get; set; }

        public RelayCommand CreateGroupCommand { get; set; }

        public RelayCommand<UserViewModel> OpenContactCommand { get; set; }

        #endregion

        #region Navigation

        public override async void OnNavigatedTo(NavigationEventArgs e)
        {
            Search(null);
        }

        #endregion

        #region Base members

        protected override void OnIsLoadingChanged()
        {
            CreateGroupCommand.RaiseCanExecuteChanged();
            OpenContactCommand.RaiseCanExecuteChanged();
        }

        #endregion

        private async void Search(string searchQuery)
        {
            Contacts.Clear();
            if (string.IsNullOrEmpty(searchQuery))
            {
                foreach (Contact contact in QuickbloxClient.ChatXmppClient.Contacts)
                {
                    Contacts.Add(UserViewModel.FromContact(contact));
                }
            }
            else
            {
                foreach (Contact contact in QuickbloxClient.ChatXmppClient.Contacts.Where(c => !string.IsNullOrEmpty(c.Name) && c.Name.IndexOf(searchQuery, StringComparison.OrdinalIgnoreCase) >= 0))
                {
                    Contacts.Add(UserViewModel.FromContact(contact));
                }
            }

            await SetPresences();
            await LoadUserNamesAndImages();
        }

        private async Task LoadUserNamesAndImages()
        {
            var cachingQbClient = ServiceLocator.Locator.Get<ICachingQuickbloxClient>();
            var imagesService = ServiceLocator.Locator.Get<IImageService>();
            foreach (UserViewModel userVm in Contacts)
            {
                var user = await cachingQbClient.GetUserById(userVm.UserId);
                if (user != null)
                {
                    userVm.FullName = user.FullName;

                    if (user.BlobId.HasValue)
                    {
                        userVm.ImageUploadId = user.BlobId;
                        userVm.Image = await imagesService.GetPrivateImage(user.BlobId.Value, 100);
                    }
                }
            }
        }

        private async Task SetPresences()
        {
            var cachingQbClient = ServiceLocator.Locator.Get<ICachingQuickbloxClient>();
            foreach (var localResult in Contacts)
            {
                localResult.IsOnline = QuickbloxClient.ChatXmppClient.Presences.Any(p => p.UserId == localResult.UserId && (p.PresenceType == PresenceType.None || p.PresenceType == PresenceType.Subscribed));
                var user = await cachingQbClient.GetUserById(localResult.UserId);
                if (user?.LastRequestAt != null)
                {
                    localResult.LastActive = user.LastRequestAt.Value;
                }
            }
        }


        private void CreateGroupCommandExecute()
        {
            NavigationService.Navigate(ViewLocator.GroupAddMember);
        }


        private async Task OpenContactCommandExecute(UserViewModel user)
        {
            var dialogsManager = ServiceLocator.Locator.Get<IDialogsManager>();
            var userDialog = dialogsManager.Dialogs.FirstOrDefault(d => d.DialogType == DialogType.Private && d.OccupantIds.Contains(user.UserId));
            if (userDialog != null)
                NavigationService.Navigate(ViewLocator.Chat, new ChatNavigationParameter { Dialog = userDialog });
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

    }
}
