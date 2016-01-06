﻿using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Windows.ApplicationModel.Core;
using Windows.UI.Core;
using QMunicate.Core.DependencyInjection;
using QMunicate.Core.Logger;
using QMunicate.Helper;
using QMunicate.ViewModels.PartialViewModels;
using Quickblox.Sdk;
using Quickblox.Sdk.Builder;
using Quickblox.Sdk.GeneralDataModel.Models;
using Quickblox.Sdk.Modules.ChatModule.Models;
using Quickblox.Sdk.Modules.ChatModule.Requests;
using Quickblox.Sdk.Modules.ChatXmppModule.Models;
using Quickblox.Sdk.Modules.Models;

namespace QMunicate.Services
{
    public interface IDialogsManager
    {
        ObservableCollection<DialogViewModel> Dialogs { get; }
        Task ReloadDialogs();
        void JoinAllGroupDialogs();
        Task UpdateDialogLastMessage(string dialogId, string lastActivity, DateTime lastMessageSent);
    }

    public class DialogsManager : IDialogsManager
    {
        #region Fields

        private static readonly DateTime UnixEpoch = new DateTime(1970, 1, 1, 0, 0, 0, 0).ToLocalTime();

        private bool isReloadingDialogs;
        private bool areAllGroupDialogsJoined;
        private readonly IQuickbloxClient quickbloxClient;

        #endregion

        #region Ctor

        public DialogsManager(IQuickbloxClient quickbloxClient)
        {
            this.quickbloxClient = quickbloxClient;
            quickbloxClient.ChatXmppClient.OnMessageReceived += MessagesClientOnOnMessageReceived;
            quickbloxClient.ChatXmppClient.OnSystemMessageReceived += MessagesClientOnOnSystemMessageReceived;
            Dialogs = new ObservableCollection<DialogViewModel>();
        }

        #endregion

        #region Properties

        public ObservableCollection<DialogViewModel> Dialogs { get; private set; }

        #endregion

        #region Public methods

        public async Task ReloadDialogs()
        {
            if (isReloadingDialogs) return;
            isReloadingDialogs = true;

            try
            {
                var retrieveDialogsRequest = new RetrieveDialogsRequest();
                var response = await quickbloxClient.ChatClient.GetDialogsAsync(retrieveDialogsRequest);
                if (response.StatusCode == HttpStatusCode.OK)
                {
                    Dialogs.Clear();
                    var currentUserId = SettingsManager.Instance.ReadFromSettings<int>(SettingsKeys.CurrentUserId);
                    foreach (var dialog in response.Result.Items)
                    {
                        var dialogVm = DialogViewModel.FromDialog(dialog);

                        if (dialog.Type == DialogType.Private)
                        {
                            int otherUserId = dialogVm.OccupantIds.FirstOrDefault(o => o != currentUserId);
                            dialogVm.Name = GetUserNameFromContacts(otherUserId);
                        }

                        Dialogs.Add(dialogVm);
                    }

                    await FixPrivateDialogsNamesAndImages();
                    await LoadDialogImages(100);
                }
            }
            finally
            {
                isReloadingDialogs = false;
            }
        }

        public void JoinAllGroupDialogs()
        {
            if (areAllGroupDialogsJoined) return;

            int currentUserId = SettingsManager.Instance.ReadFromSettings<int>(SettingsKeys.CurrentUserId);

            foreach (DialogViewModel dialogVm in Dialogs)
            {
                if (dialogVm.DialogType == DialogType.Group)
                {
                    var groupChatManager = quickbloxClient.ChatXmppClient.GetGroupChatManager(dialogVm.XmppRoomJid, dialogVm.Id);
                    groupChatManager.JoinGroup(currentUserId.ToString());
                }
            }

            areAllGroupDialogsJoined = true;
        }

        public async Task UpdateDialogLastMessage(string dialogId, string lastActivity, DateTime lastMessageSent)
        {
            if (string.IsNullOrEmpty(dialogId)) return;

            var dialog = Dialogs.FirstOrDefault(d => d.Id == dialogId);
            if (dialog != null)
            {
                if (string.IsNullOrEmpty(lastActivity) && lastMessageSent == UnixEpoch) return;

                dialog.LastActivity = lastActivity;
                dialog.LastMessageSent = lastMessageSent;
                int itemIndex = Dialogs.IndexOf(dialog);
                Dialogs.Move(itemIndex, 0);
            }
            else // This is depricated now. We use Quickblox System messages instead. Left for backward compatibility
            {
                await QmunicateLoggerHolder.Log(QmunicateLogLevel.Warn, "The dialog wasn't found in DialogsManager. Reloading dialogs.");
                await ReloadDialogs();
                areAllGroupDialogsJoined = false;
                JoinAllGroupDialogs();
            }

        }

        #endregion

        #region Private methods

        private async void MessagesClientOnOnMessageReceived(object sender, Message message)
        {
            await CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(CoreDispatcherPriority.Normal, async () =>
            {
                await UpdateDialogLastMessage(message.ChatDialogId, message.MessageText, message.DateSent.ToDateTime());

                if (message.NotificationType == NotificationTypes.GroupUpdate)
                    await UpdateGroupDialog(message);
            });
        }

        private async void MessagesClientOnOnSystemMessageReceived(object sender, SystemMessage message)
        {
            await CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(CoreDispatcherPriority.Normal, async () =>
            {
                var groupInfoMessage = message as GroupInfoMessage;
                if (groupInfoMessage != null)
                {
                    await OnGroupInfoMessage(groupInfoMessage);
                }
            });
        }

        private async Task OnGroupInfoMessage(GroupInfoMessage groupInfoMessage)
        {
            var dialog = new Dialog
            {
                Id = groupInfoMessage.DialogId,
                LastMessage = "Notification message",
                LastMessageDateSent = groupInfoMessage.DateSent.ToUnixEpoch(),
                Name = groupInfoMessage.RoomName,
                Photo = groupInfoMessage.RoomPhoto,
                OccupantsIds = groupInfoMessage.CurrentOccupantsIds,
                Type = groupInfoMessage.DialogType,
                XmppRoomJid = BuildRoomJid(groupInfoMessage.DialogId)
            };

            var dialogViewModel = DialogViewModel.FromDialog(dialog);
            var imagesService = ServiceLocator.Locator.Get<IImageService>();
            dialogViewModel.Image = await imagesService.GetPublicImage(dialogViewModel.Photo);
            int currentUserId = SettingsManager.Instance.ReadFromSettings<int>(SettingsKeys.CurrentUserId);
            var groupChatManager = quickbloxClient.ChatXmppClient.GetGroupChatManager(dialogViewModel.XmppRoomJid, dialogViewModel.Id);
            groupChatManager.JoinGroup(currentUserId.ToString());

            Dialogs.Insert(0, dialogViewModel);
        }

        private async Task UpdateGroupDialog(Message message)
        {
            var updatedDialog = Dialogs.FirstOrDefault(d => d.Id == message.ChatDialogId);
            if (updatedDialog != null)
            {
                if (!string.IsNullOrEmpty(message.RoomPhoto))
                {
                    updatedDialog.Photo = message.RoomPhoto;
                    var imagesService = ServiceLocator.Locator.Get<IImageService>();
                    updatedDialog.Image = await imagesService.GetPublicImage(message.RoomPhoto);
                }

                if (!string.IsNullOrEmpty(message.RoomName))
                {
                    updatedDialog.Name = message.RoomName;
                }

                if (message.CurrentOccupantsIds.Any())
                {
                    updatedDialog.OccupantIds = message.CurrentOccupantsIds;
                }
            }
        }

        private string GetUserNameFromContacts(int userId)
        {
            var otherContact = quickbloxClient.ChatXmppClient.Contacts.FirstOrDefault(c => c.UserId == userId);
            if (otherContact != null)
                return otherContact.Name;

            return null;
        }

        private string BuildRoomJid(string dialogId)
        {
            return $"{ApplicationKeys.ApplicationId}_{dialogId}@{ApplicationKeys.ChatMucDomain}";
        }

        private async Task FixPrivateDialogsNamesAndImages()
        {
            var currentUserId = SettingsManager.Instance.ReadFromSettings<int>(SettingsKeys.CurrentUserId);
            var cachingQbClient = ServiceLocator.Locator.Get<ICachingQuickbloxClient>();
            foreach (DialogViewModel dialogVm in Dialogs.Where(dvm => dvm.DialogType == DialogType.Private))
            {
                int otherUserId = dialogVm.OccupantIds.FirstOrDefault(o => o != currentUserId);
                var user = await cachingQbClient.GetUserById(otherUserId);
                if (user != null)
                {
                    dialogVm.Name = user.FullName;
                    dialogVm.PrivatePhotoId = user.BlobId;
                }
            }
        }

        private async Task LoadDialogImages(int? decodePixelWidth = null, int? decodePixelHeight = null)
        {
            var imagesService = ServiceLocator.Locator.Get<IImageService>();

            foreach (DialogViewModel dialogVm in Dialogs.Where(dvm => dvm.DialogType == DialogType.Group))
            {
                dialogVm.Image = await imagesService.GetPublicImage(dialogVm.Photo);
            }

            Parallel.ForEach(Dialogs.Where(d => d.DialogType == DialogType.Private), async (dialogVm, state) =>
            {
                if (dialogVm.PrivatePhotoId.HasValue)
                {
                    var imageBytes = await imagesService.GetPrivateImageBytes(dialogVm.PrivatePhotoId.Value);
                    CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(CoreDispatcherPriority.Normal, async () =>
                        dialogVm.Image = await ImageHelper.CreateBitmapImage(imageBytes, decodePixelWidth, decodePixelHeight));
                }
            });
        }

        #endregion

    }
}
