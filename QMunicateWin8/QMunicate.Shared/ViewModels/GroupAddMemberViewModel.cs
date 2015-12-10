using QMunicate.Core.AsyncLock;
using QMunicate.Core.Command;
using QMunicate.Core.DependencyInjection;
using QMunicate.Core.MessageService;
using QMunicate.Helper;
using QMunicate.Services;
using QMunicate.ViewModels.PartialViewModels;
using Quickblox.Sdk;
using Quickblox.Sdk.Modules.ChatModule.Models;
using Quickblox.Sdk.Modules.ChatModule.Requests;
using Quickblox.Sdk.Modules.ContentModule;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using Windows.Storage;
using Windows.Storage.Pickers;
using Windows.Storage.Streams;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using Quickblox.Sdk.Modules.ChatXmppModule.Models;

namespace QMunicate.ViewModels
{
    public class GroupAddMemberViewModel : ViewModel, IFileOpenPickerContinuable
    {
        private string groupName;
        private string searchText;
        private string membersText;
        private readonly AsyncLock contactsLock = new AsyncLock();
        private List<SelectableListBoxItem<UserViewModel>> allContacts;
        private bool isEditMode;
        private DialogViewModel editedDialog;

        private ImageSource chatImage;
        private byte[] chatImageBytes;

        #region Ctor

        public GroupAddMemberViewModel()
        {
            UsersToAdd = new ObservableCollection<SelectableListBoxItem<UserViewModel>>();
            
            CreateGroupCommand = new RelayCommand(CreateGroupCommandExecute, () => !IsLoading && UsersToAdd.Count > 0);
            ChangeImageCommand = new RelayCommand(ChangeImageCommandExecute, () => !IsLoading);

            UsersToAdd.CollectionChanged += (sender, args) => { CreateGroupCommand.RaiseCanExecuteChanged(); };
        }

        #endregion

        #region Properties

        public string GroupName
        {
            get { return groupName; }
            set { Set(ref groupName, value); }
        }

        public string SearchText
        {
            get { return searchText; }
            set
            {
                if (Set(ref searchText, value))
                    Search(searchText);
            }
        }

        public string MembersText
        {
            get { return membersText; }
            set { Set(ref membersText, value); }
        }

        public ObservableCollection<SelectableListBoxItem<UserViewModel>> UsersToAdd { get; set; }

        public bool IsEditMode
        {
            get { return isEditMode; }
            set { Set(ref isEditMode, value); }
        }

        public ImageSource ChatImage
        {
            get { return chatImage; }
            set { Set(ref chatImage, value); }
        }

        public RelayCommand CreateGroupCommand { get; set; }

        public RelayCommand ChangeImageCommand { get; private set; }

        #endregion

        #region Navigation

        public override async void OnNavigatedTo(NavigationEventArgs e)
        {
            var dialog = e.Parameter as DialogViewModel;
            if (dialog != null)
            {
                IsEditMode = true;
                editedDialog = dialog;
            }

            IsLoading = true;
            await InitializeAllContacts(editedDialog);
            await Search(null);
            IsLoading = false;
        }

        //public override void OnNavigatedFrom(NavigatingCancelEventArgs e)
        //{
        //    foreach (var contact in UsersToAdd)
        //    {
        //        contact.Item.Image = null;
        //    }
        //}

        #endregion

        #region Base members

        protected override void OnIsLoadingChanged()
        {
            CreateGroupCommand.RaiseCanExecuteChanged();
        }

        #endregion

        #region IFileOpenPickerContinuable Members

        public async void ContinueFileOpenPicker(IReadOnlyList<StorageFile> files)
        {
            if (files != null && files.Any())
            {
                var stream = (FileRandomAccessStream)await files[0].OpenAsync(FileAccessMode.Read);
                using (var streamForImage = stream.CloneStream())
                {
                    chatImageBytes = new byte[stream.Size];
                    using (var dataReader = new DataReader(stream))
                    {
                        await dataReader.LoadAsync((uint)stream.Size);
                        dataReader.ReadBytes(chatImageBytes);
                    }

                    ChatImage = ImageHelper.CreateBitmapImage(streamForImage, 100);
                }
            }
        }

        #endregion

        #region Private methods

        private async void ChangeImageCommandExecute()
        {
            FileOpenPicker picker = new FileOpenPicker();
            picker.FileTypeFilter.Add(".jpg");
            picker.FileTypeFilter.Add(".png");
#if WINDOWS_PHONE_APP
            picker.PickSingleFileAndContinue();
#endif
        }

        private async Task InitializeAllContacts(DialogViewModel existingDialog)
        {
            allContacts = new List<SelectableListBoxItem<UserViewModel>>();

            foreach (Contact contact in QuickbloxClient.ChatXmppClient.Contacts)
            {
                if(existingDialog != null && existingDialog.OccupantIds.Contains(contact.UserId)) continue;

                var userVm = UserViewModel.FromContact(contact);
                allContacts.Add(new SelectableListBoxItem<UserViewModel>(userVm));
            }

            await LoadContactsNamesAndImages();
        }

        private async Task LoadContactsNamesAndImages()
        {
            var cachingQbClient = ServiceLocator.Locator.Get<ICachingQuickbloxClient>();
            var imagesService = ServiceLocator.Locator.Get<IImageService>();
            foreach (var userVm in allContacts)
            {
                var user = await cachingQbClient.GetUserById(userVm.Item.UserId);
                if (user != null)
                {
                    userVm.Item.FullName = user.FullName;

                    if(user.BlobId.HasValue)
                        userVm.Item.Image = await imagesService.GetPrivateImage(user.BlobId.Value, 100);
                }
            }
        }

        private async Task Search(string searchQuery)
        {
            using (await contactsLock.LockAsync())
            {
                UsersToAdd.Clear();
                if (string.IsNullOrEmpty(searchQuery))
                {
                    foreach (var userVm in allContacts)
                    {
                        UsersToAdd.Add(userVm);
                    }
                }
                else
                {
                    foreach (var userVm in allContacts.Where(c => !string.IsNullOrEmpty(c.Item.FullName) && c.Item.FullName.IndexOf(searchQuery, StringComparison.OrdinalIgnoreCase) >= 0))
                    {
                        UsersToAdd.Add(userVm);
                    }
                }
            }


        }

        private async void CreateGroupCommandExecute()
        {
            IsLoading = true;

            if (!await Validate())
            {
                IsLoading = false;
                return;
            }

            if (IsEditMode)
            {
                await UpdateGroup();
            }
            else
            {
                await CreateGroup();
            }

            IsLoading = false;
        }

        private async Task UpdateGroup()
        {
            var selectedContacts = allContacts.Where(c => c.IsSelected).ToList();

            var updateDialogRequest = new UpdateDialogRequest {DialogId = editedDialog.Id};
            var addedUsers = selectedContacts.Where(c => !editedDialog.OccupantIds.Contains(c.Item.UserId)).Select(u => u.Item.UserId).ToArray();
            if (addedUsers.Any())
                updateDialogRequest.PushAll = new EditedOccupants() {OccupantsIds = addedUsers};

            var updateDialogResponse = await QuickbloxClient.ChatClient.UpdateDialogAsync(updateDialogRequest);

            if (updateDialogResponse.StatusCode == HttpStatusCode.OK)
            {
                var dialogsManager = ServiceLocator.Locator.Get<IDialogsManager>();
                var dialog = dialogsManager.Dialogs.FirstOrDefault(d => d.Id == editedDialog.Id);
                dialog.OccupantIds = updateDialogResponse.Result.OccupantsIds;

                var groupChatManager = QuickbloxClient.ChatXmppClient.GetGroupChatManager(editedDialog.XmppRoomJid, editedDialog.Id);
                groupChatManager.NotifyAboutGroupUpdate(addedUsers, new List<int>(), updateDialogResponse.Result);

                NavigationService.Navigate(ViewLocator.GroupChat, editedDialog.Id);
            }
        }

        private async Task CreateGroup()
        {
            var selectedContactsIds = allContacts.Where(c => c.IsSelected).Select(c => c.Item.UserId).ToList();
            string selectedUsersString = BuildUsersString(selectedContactsIds);

            string imageLink = null;
            if (chatImageBytes != null)
            {
                var contentHelper = new ContentClientHelper(QuickbloxClient.ContentClient);
                imageLink = await contentHelper.UploadPublicImage(chatImageBytes);
            }

            var createDialogResponse = await QuickbloxClient.ChatClient.CreateDialogAsync(GroupName, DialogType.Group, selectedUsersString, imageLink);
            if (createDialogResponse.StatusCode == HttpStatusCode.Created)
            {
                var dialogVm = DialogViewModel.FromDialog(createDialogResponse.Result);
                dialogVm.Image = ChatImage;

                var dialogsManager = ServiceLocator.Locator.Get<IDialogsManager>();
                dialogsManager.Dialogs.Insert(0, dialogVm);

                int currentUserId = SettingsManager.Instance.ReadFromSettings<int>(SettingsKeys.CurrentUserId);
                var groupChatManager = QuickbloxClient.ChatXmppClient.GetGroupChatManager(createDialogResponse.Result.XmppRoomJid, createDialogResponse.Result.Id);
                groupChatManager.JoinGroup(currentUserId.ToString());


                groupChatManager.NotifyAboutGroupCreation(selectedContactsIds, createDialogResponse.Result);

                NavigationService.Navigate(ViewLocator.GroupChat, createDialogResponse.Result.Id);
            }
        }

        private async Task<bool> Validate()
        {
            var messageService = ServiceLocator.Locator.Get<IMessageService>();
            if (!isEditMode)
            {
                if (string.IsNullOrWhiteSpace(GroupName))
                {
                    await messageService.ShowAsync("Group name", "A Group name field must not be empty.");
                    return false;
                }

                if (!GroupName.Any(char.IsLetter))
                {
                    await messageService.ShowAsync("Group name", "A Group name field must contain at least one letter.");
                    return false;
                }
            }

            if (!allContacts.Any(c => c.IsSelected))
            {
                await messageService.ShowAsync("No users", "Please, select some users to add to the group.");
                IsLoading = false;
                return false;
            }

            return true;
        }

        private string BuildUsersString(IEnumerable<int> users)
        {
            var userIdsBuilder = new StringBuilder();
            foreach (int userId in users)
            {
                userIdsBuilder.Append(userId + ",");
            }
            if (userIdsBuilder.Length > 1)
                userIdsBuilder.Remove(userIdsBuilder.Length - 1, 1);

            return userIdsBuilder.ToString();
        }

        #endregion

    }
}
