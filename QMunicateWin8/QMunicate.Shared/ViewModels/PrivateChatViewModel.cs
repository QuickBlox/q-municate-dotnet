﻿using QMunicate.Core.Command;
using QMunicate.Core.DependencyInjection;
using QMunicate.Core.Logger;
using QMunicate.Core.MessageService;
using QMunicate.Helper;
using QMunicate.Models;
using QMunicate.ViewModels.PartialViewModels;
using Quickblox.Sdk;
using Quickblox.Sdk.GeneralDataModel.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Windows.ApplicationModel.Core;
using Windows.Storage;
using Windows.Storage.Pickers;
using Windows.Storage.Streams;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Imaging;
using Windows.UI.Xaml.Navigation;
using QMunicate.Services;
using Quickblox.Sdk.Modules.ChatXmppModule.Interfaces;
using Quickblox.Sdk.Modules.ChatXmppModule.Models;
using Quickblox.Sdk.Modules.ContentModule;

namespace QMunicate.ViewModels
{
    public class PrivateChatViewModel : ViewModel, IFileOpenPickerContinuable
    {
        #region Fields

        private string newMessageText;
        private string chatName;
        private ImageSource chatImage;
        private bool isActiveContactRequest;
        private bool isWaitingForContactResponse;
        private bool isRequestRejected;
        private DialogViewModel dialog;
        private int currentUserId;
        private IPrivateChatManager privateChatManager;
        private bool isMeTyping;
        private bool isOtherUserTyping;
        private readonly DispatcherTimer typingIndicatorTimer = new DispatcherTimer();
        private readonly TimeSpan typingIndicatorTimeout = new TimeSpan(0, 0, 10);
        private readonly DispatcherTimer pausedTypingTimer = new DispatcherTimer();
        private readonly TimeSpan pausedTypingTimeout = new TimeSpan(0, 0, 10);
        

        #endregion

        #region Ctor

        public PrivateChatViewModel()
        {
            MessageCollectionViewModel = new MessageCollectionViewModel();
            SendCommand = new RelayCommand(SendCommandExecute, () => !IsLoading && IsMessageSendingAllowed);
            SendAttachmentCommand = new RelayCommand(SendAttachmentCommandExecute, () => !IsLoading && IsMessageSendingAllowed);
            AcceptRequestCommand = new RelayCommand(AcceptRequestCommandExecute, () => !IsLoading);
            RejectRequestCommand = new RelayCommand(RejectCRequestCommandExecute, () => !IsLoading);
            ShowUserInfoCommand = new RelayCommand(ShowUserInfoCommandExecute, () => !IsLoading);
            typingIndicatorTimer.Interval = typingIndicatorTimeout;
            typingIndicatorTimer.Tick += (sender, o) => IsOtherUserTyping = false;
            pausedTypingTimer.Interval = pausedTypingTimeout;
            pausedTypingTimer.Tick += PausedTypingTimerOnTick;
        }

        #endregion

        #region Properties

        public MessageCollectionViewModel MessageCollectionViewModel { get; set; }

        public string NewMessageText
        {
            get { return newMessageText; }
            set
            {
                Set(ref newMessageText, value);
                if(!string.IsNullOrEmpty(value)) NotifyIsTyping();
            }
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

        public bool IsActiveContactRequest
        {
            get { return isActiveContactRequest; }
            set
            {
                Set(ref isActiveContactRequest, value);
                NotifyCanExecuteChanged();
            }
        }

        public bool IsWaitingForContactResponse
        {
            get { return isWaitingForContactResponse; }
            set
            {
                Set(ref isWaitingForContactResponse, value);
                NotifyCanExecuteChanged();

            }
        }

        public bool IsRequestRejected
        {
            get { return isRequestRejected; }
            set
            {
                Set(ref isRequestRejected, value);
                NotifyCanExecuteChanged();
            }
        }

        public bool IsMessageSendingAllowed
        {
            get { return !IsActiveContactRequest && !IsRequestRejected && !IsWaitingForContactResponse; }
        }

        public bool IsOtherUserTyping
        {
            get { return isOtherUserTyping; }
            set { Set(ref isOtherUserTyping, value); }
        }

        public RelayCommand SendCommand { get; private set; }

        public RelayCommand SendAttachmentCommand { get; private set; }

        public RelayCommand AcceptRequestCommand { get; private set; }

        public RelayCommand RejectRequestCommand { get; private set; }

        public RelayCommand ShowUserInfoCommand { get; private set; }

        #endregion

        #region Navigation

        public async override void OnNavigatedTo(NavigationEventArgs e)
        {
            var chatParameter = e.Parameter as ChatNavigationParameter;
            if (chatParameter == null) return;

            await Initialize(chatParameter);
        }

        public override void OnNavigatedFrom(NavigationEventArgs e)
        {
            base.OnNavigatedFrom(e);

            if (privateChatManager != null) privateChatManager.OnMessageReceived -= ChatManagerOnOnMessageReceived;
        }

        #endregion

        #region Base members

        protected override void OnIsLoadingChanged()
        {
            NotifyCanExecuteChanged();
        }

        #endregion

        #region IFileOpenPickerContinuable Members

        public async void ContinueFileOpenPicker(IReadOnlyList<StorageFile> files)
        {
            if (files == null || !files.Any()) return;
            IsLoading = true;
            using (var stream = (FileRandomAccessStream) await files[0].OpenAsync(FileAccessMode.Read))
            {
                var newImageBytes = new byte[stream.Size];
                using (var dataReader = new DataReader(stream))
                {
                    await dataReader.LoadAsync((uint)stream.Size);
                    dataReader.ReadBytes(newImageBytes);
                }

                var contentHelper = new ContentClientHelper(QuickbloxClient.ContentClient);
                var imageUploadResult = await contentHelper.UploadPublicImage(newImageBytes);
                if (imageUploadResult != null)
                {
                    await SendAttachment(imageUploadResult);
                }
            }
            IsLoading = false;
        }

        #endregion

        #region Private methods

        private async Task Initialize(ChatNavigationParameter chatParameter)
        {
            IsLoading = true;

            currentUserId = SettingsManager.Instance.ReadFromSettings<int>(SettingsKeys.CurrentUserId);

            if (chatParameter.Dialog != null)
            {
                dialog = chatParameter.Dialog;
                ChatName = chatParameter.Dialog.Name;
                ChatImage = chatParameter.Dialog.Image;

                int otherUserId = dialog.OccupantIds.FirstOrDefault(id => id != currentUserId);
                await QmunicateLoggerHolder.Log(QmunicateLogLevel.Debug, string.Format("Initializing Chat page. CurrentUserId: {0}. OtherUserId: {1}.", currentUserId, otherUserId));

                if (!string.IsNullOrEmpty(chatParameter.Dialog.Id))
                    await MessageCollectionViewModel.LoadMessages(chatParameter.Dialog.Id);

                if (otherUserId != 0)
                {
                    privateChatManager = QuickbloxClient.ChatXmppClient.GetPrivateChatManager(otherUserId, chatParameter.Dialog.Id);
                    privateChatManager.OnMessageReceived += ChatManagerOnOnMessageReceived;
                    privateChatManager.OnIsTyping += PrivateChatManagerOnOnIsTyping;
                    privateChatManager.OnPausedTyping += PrivateChatManagerOnOnPausedTyping;
                }
                

                CheckIsMessageSendingAllowed();
            }

            IsLoading = false;
        }

        #region IsTyping functionality

        private void PrivateChatManagerOnOnIsTyping(object sender, EventArgs eventArgs)
        {
            var dispatcher = CoreApplication.MainView.CoreWindow.Dispatcher;
            dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
            {
                IsOtherUserTyping = true;

                typingIndicatorTimer.Stop();
                typingIndicatorTimer.Start();
            });
        }

        private void PrivateChatManagerOnOnPausedTyping(object sender, EventArgs eventArgs)
        {
            var dispatcher = CoreApplication.MainView.CoreWindow.Dispatcher;
            dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
            {
                IsOtherUserTyping = false;
            });
        }

        private void NotifyIsTyping()
        {
            if (privateChatManager == null || isMeTyping) return;

            pausedTypingTimer.Start();

            isMeTyping = true;
            privateChatManager.NotifyIsTyping();
        }

        private void PausedTypingTimerOnTick(object sender, object o)
        {
            NotifyPausedTyping();
        }

        private void NotifyPausedTyping()
        {
            pausedTypingTimer.Stop();
            if (privateChatManager == null) return;

            privateChatManager.NotifyPausedTyping();

            isMeTyping = false;
        }

        #endregion

        private void CheckIsMessageSendingAllowed()
        {
            bool isChecked = false;
            for (int i = MessageCollectionViewModel.Messages.Count - 1; i >= 0; i--)
            {
                if (isChecked) break;

                var currentMessageGroup = MessageCollectionViewModel.Messages[i];
                for (int j = currentMessageGroup.Count - 1; j >= 0; j--)
                {
                    var currentMessage = currentMessageGroup[j];
                    if (currentMessage.NotificationType == NotificationTypes.FriendsAccept)
                    {
                        isChecked = true;
                        break;
                    }

                    if (currentMessage.NotificationType == NotificationTypes.FriendsReject || currentMessage.NotificationType == NotificationTypes.FriendsRemove)
                    {
                        IsRequestRejected = true;
                        isChecked = true;
                        break;
                    }

                    if (currentMessage.MessageType == MessageType.Outgoing && currentMessage.NotificationType == NotificationTypes.FriendsRequest)
                    {
                        IsWaitingForContactResponse = true;
                        isChecked = true;
                        break;
                    }

                    if (currentMessage.MessageType == MessageType.Incoming && currentMessage.NotificationType == NotificationTypes.FriendsRequest)
                    {
                        IsActiveContactRequest = true;
                        isChecked = true;
                        break;
                    }
                }
            }
        }

        private async void SendCommandExecute()
        {
            if (string.IsNullOrWhiteSpace(NewMessageText)) return;

            NotifyPausedTyping();

            bool isMessageSent = privateChatManager.SendMessage(NewMessageText);

            if (!isMessageSent)
            {
                var messageService = ServiceLocator.Locator.Get<IMessageService>();
                await messageService.ShowAsync("Message", "Failed to send a message");
                return;
            }

            var messageViewModel = new MessageViewModel()
            {
                MessageText = NewMessageText,
                MessageType = MessageType.Outgoing,
                DateTime = DateTime.Now
            };

            await MessageCollectionViewModel.AddNewMessage(messageViewModel);
            var dialogsManager = ServiceLocator.Locator.Get<IDialogsManager>();
            await dialogsManager.UpdateDialogLastMessage(dialog.Id, NewMessageText, DateTime.Now);

            NewMessageText = "";
        }

        private async void SendAttachmentCommandExecute()
        {
            FileOpenPicker picker = new FileOpenPicker();
            picker.FileTypeFilter.Add(".jpg");
            picker.FileTypeFilter.Add(".png");
#if WINDOWS_PHONE_APP
            picker.PickSingleFileAndContinue();
#endif
        }

        private async Task SendAttachment(ImageUploadResult imageUploadResult)
        {
            if (string.IsNullOrEmpty(imageUploadResult.Url) || imageUploadResult.BlodId == 0) return;

            var attachment = new AttachmentTag
            {
                Id = imageUploadResult.BlodId.ToString(),
                Url = imageUploadResult.Url,
                Type = "photo"
            };

            privateChatManager.SendAttachemnt(attachment);

            var messageViewModel = new MessageViewModel()
            {
                AttachedImage = new BitmapImage(new Uri(imageUploadResult.Url)),
                MessageType = MessageType.Outgoing,
                DateTime = DateTime.Now
            };

            await MessageCollectionViewModel.AddNewMessage(messageViewModel);
            var dialogsManager = ServiceLocator.Locator.Get<IDialogsManager>();
            await dialogsManager.UpdateDialogLastMessage(dialog.Id, NewMessageText, DateTime.Now);
        }

        private async void AcceptRequestCommandExecute()
        {
            if (privateChatManager == null) return;
            IsLoading = true;
            bool accepted = privateChatManager.AcceptFriend();

            if (accepted)
            {
                IsActiveContactRequest = false;
                await MessageCollectionViewModel.LoadMessages(dialog.Id);
                CheckIsMessageSendingAllowed();
            }
            

            IsLoading = false;
        }

        private async void RejectCRequestCommandExecute()
        {
            if (privateChatManager == null) return;

            IsLoading = true;
            bool rejected = privateChatManager.RejectFriend();

            if (rejected)
            {
                IsActiveContactRequest = false;
                await MessageCollectionViewModel.LoadMessages(dialog.Id);
                CheckIsMessageSendingAllowed();
            }

            IsLoading = false;

        }

        private void ShowUserInfoCommandExecute()
        {
            NavigationService.Navigate(ViewLocator.UserInfo, dialog == null ? null : dialog.Id);
        }

        private async void ChatManagerOnOnMessageReceived(object sender, Message message)
        {
            await MessageCollectionViewModel.AddNewMessage(message);

            CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(CoreDispatcherPriority.Normal, async () =>
            {
                CheckIsMessageSendingAllowed();
            });
        }

        private void NotifyCanExecuteChanged()
        {
            SendCommand.RaiseCanExecuteChanged();
            SendAttachmentCommand.RaiseCanExecuteChanged();
            AcceptRequestCommand.RaiseCanExecuteChanged();
            RejectRequestCommand.RaiseCanExecuteChanged();
            ShowUserInfoCommand.RaiseCanExecuteChanged();
        }

        #endregion

    }
}
