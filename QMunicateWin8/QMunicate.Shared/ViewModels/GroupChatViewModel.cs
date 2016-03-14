using QMunicate.Core.Command;
using QMunicate.Core.DependencyInjection;
using QMunicate.Core.Logger;
using QMunicate.Core.MessageService;
using QMunicate.Helper;
using QMunicate.ViewModels.PartialViewModels;
using Quickblox.Sdk;
using Quickblox.Sdk.GeneralDataModel.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Windows.ApplicationModel.Core;
using Windows.Storage;
using Windows.Storage.Pickers;
using Windows.Storage.Streams;
using Windows.UI.Core;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Imaging;
using Windows.UI.Xaml.Navigation;
using QMunicate.Services;
using Quickblox.Sdk.Modules.ChatXmppModule.Interfaces;
using Quickblox.Sdk.Modules.ChatXmppModule.Models;
using Quickblox.Sdk.Modules.ContentModule;

namespace QMunicate.ViewModels
{
    public class GroupChatViewModel : ViewModel, IFileOpenPickerContinuable
    {
        #region Fields

        private string newMessageText;
        private string chatName;
        private ImageSource chatImage;
        private DialogViewModel dialog;
        private IGroupChatManager groupChatManager;
        private int numberOfMembers;
        private int currentUserId;

        #endregion

        #region Ctor

        public GroupChatViewModel()
        {
            MessageCollectionViewModel = new MessageCollectionViewModel();
            SendCommand = new RelayCommand(SendCommandExecute, () => !IsLoading);
            SendAttachmentCommand = new RelayCommand(SendAttachmentCommandExecute, () => !IsLoading);
            ShowGroupInfoCommand = new RelayCommand(ShowGroupInfoCommandExecute, () => !IsLoading);
            ShowImageCommand = new RelayCommand<ImageSource>(ShowImageCommandExecute, img => !IsLoading);
        }

        #endregion

        #region Properties

        public MessageCollectionViewModel MessageCollectionViewModel { get; set; }

        public string NewMessageText
        {
            get { return newMessageText; }
            set { Set(ref newMessageText, value); }
        }

        public string ChatName
        {
            get { return chatName; }
            set { Set(ref chatName, value); }
        }

        public ImageSource ChatImage
        {
            get { return chatImage; }
            set { Set(ref chatImage, value); }
        }

        public int NumberOfMembers
        {
            get { return numberOfMembers; }
            set { Set(ref numberOfMembers, value); }
        }

        public RelayCommand SendCommand { get; private set; }

        public RelayCommand SendAttachmentCommand { get; }

        public RelayCommand ShowGroupInfoCommand { get; private set; }

        public RelayCommand<ImageSource> ShowImageCommand { get; }

        #endregion

        #region Navigation

        public async override void OnNavigatedTo(NavigationEventArgs e)
        {
            while (NavigationService.BackStack.Count > 1)
            {
                NavigationService.BackStack.RemoveAt(NavigationService.BackStack.Count - 1);
            }

            await Initialize(e.Parameter as string);
        }

        public override void OnNavigatedFrom(NavigationEventArgs e)
        {
            base.OnNavigatedFrom(e);

            if (groupChatManager != null) groupChatManager.MessageReceived -= ChatManagerOnOnMessageReceived;
        }

        #endregion

        #region Base members

        protected override void OnIsLoadingChanged()
        {
            SendCommand.RaiseCanExecuteChanged();
            SendAttachmentCommand.RaiseCanExecuteChanged();
            ShowGroupInfoCommand.RaiseCanExecuteChanged();
        }

        #endregion

        #region IFileOpenPickerContinuable Members

        public async void ContinueFileOpenPicker(IReadOnlyList<StorageFile> files)
        {
            if (files == null || !files.Any()) return;
            IsLoading = true;
            using (var stream = (FileRandomAccessStream)await files[0].OpenAsync(FileAccessMode.Read))
            {
                var newImageBytes = new byte[stream.Size];
                using (var dataReader = new DataReader(stream))
                {
                    await dataReader.LoadAsync((uint)stream.Size);
                    dataReader.ReadBytes(newImageBytes);
                }

                var contentHelper = new ContentClientHelper(QuickbloxClient);
                var blobUploadInfo = await contentHelper.UploadImage(newImageBytes, false);
                if (blobUploadInfo != null)
                {
                    SendAttachment(blobUploadInfo);
                }
            }
            IsLoading = false;
        }

        #endregion

        #region Private methods

        private async Task Initialize(string dialogId)
        {
            IsLoading = true;

            var dialogManager = ServiceLocator.Locator.Get<IDialogsManager>();
            dialog = dialogManager.Dialogs.FirstOrDefault(d => d.Id == dialogId);

            if (dialog == null) return;

            currentUserId = SettingsManager.Instance.ReadFromSettings<int>(SettingsKeys.CurrentUserId);

            ChatName = dialog.Name;
            ChatImage = dialog.Image;
            NumberOfMembers = dialog.OccupantIds.Count;

            await QmunicateLoggerHolder.Log(QmunicateLogLevel.Debug, string.Format("Initializing GroupChat page. CurrentUserId: {0}. Group JID: {1}.", currentUserId, dialog.XmppRoomJid));

            await MessageCollectionViewModel.LoadMessages(dialogId);

            groupChatManager = QuickbloxClient.ChatXmppClient.GetGroupChatManager(dialog.XmppRoomJid, dialog.Id);
            groupChatManager.MessageReceived += ChatManagerOnOnMessageReceived;
            groupChatManager.JoinGroup(currentUserId.ToString());

            IsLoading = false;
        }

        private async void SendCommandExecute()
        {
            if (string.IsNullOrWhiteSpace(NewMessageText)) return;

            bool isMessageSent = groupChatManager.SendMessage(WebUtility.HtmlEncode(NewMessageText));

            if (!isMessageSent)
            {
                var messageService = ServiceLocator.Locator.Get<IMessageService>();
                await messageService.ShowAsync("Message", "Failed to send a message");
                return;
            }

            var dialogsManager = ServiceLocator.Locator.Get<IDialogsManager>();
            await dialogsManager.UpdateDialogLastMessage(dialog.Id, NewMessageText, DateTime.Now);

            NewMessageText = "";
        }

        private async void ChatManagerOnOnMessageReceived(object sender, Message message)
        {
            await Helpers.RunOnTheUiThread(async () =>
            {
                await MessageCollectionViewModel.AddNewMessage(message);

                if (message.NotificationType == NotificationTypes.GroupUpdate)
                {
                    await UpdateGroupInfo(message);
                }
            });
        }

        private async Task UpdateGroupInfo(Message notificationMessage)
        {
            if (notificationMessage.CurrentOccupantsIds.Any())
            {
                NumberOfMembers = notificationMessage.CurrentOccupantsIds.Count;
            }

            if (!string.IsNullOrEmpty(notificationMessage.RoomPhoto))
            {
                var imagesService = ServiceLocator.Locator.Get<IImageService>();
                ChatImage = await imagesService.GetPublicImage(notificationMessage.RoomPhoto);
            }

            if (!string.IsNullOrEmpty(notificationMessage.RoomName))
            {
                ChatName = notificationMessage.RoomName;
            }
        }

        private void ShowGroupInfoCommandExecute()
        {
            NavigationService.Navigate(ViewLocator.GroupInfo, dialog.Id);
        }

        private void SendAttachmentCommandExecute()
        {
            FileOpenPicker picker = new FileOpenPicker();
            picker.FileTypeFilter.Add(".jpg");
            picker.FileTypeFilter.Add(".png");
#if WINDOWS_PHONE_APP
            picker.PickSingleFileAndContinue();
#endif
        }

        private void SendAttachment(BlobUploadInfo blobUploadInfo)
        {
            if (string.IsNullOrEmpty(blobUploadInfo.UId))
                return;

            var attachment = new AttachmentTag
            {
                Id = blobUploadInfo.UId,
                Type = "image"
            };

            groupChatManager.SendAttachemnt(attachment);
        }

        private void ShowImageCommandExecute(ImageSource image)
        {
            NavigationService.Navigate(ViewLocator.ImagePreview, image);
        }

        #endregion
    }
}
