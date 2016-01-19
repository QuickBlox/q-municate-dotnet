using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Windows.ApplicationModel.Core;
using Windows.UI.Core;
using Windows.UI.StartScreen;
using Newtonsoft.Json;
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
using Quickblox.Sdk.Modules.UsersModule.Models;

namespace QMunicate.Services
{
    public interface IDialogsManager
    {
        /// <summary>
        /// Dialogs collection
        /// </summary>
        ObservableCollection<DialogViewModel> Dialogs { get; }

        /// <summary>
        /// Reloads all dialogs from server
        /// </summary>
        /// <returns></returns>
        Task ReloadDialogs();

        /// <summary>
        /// Checks for dialogs updates (private chat names and images)
        /// </summary>
        /// <returns></returns>
        Task UpdateDialogsStates();

        /// <summary>
        /// Updates last activity information for a specific dialog
        /// </summary>
        /// <param name="dialogId"></param>
        /// <param name="lastActivity"></param>
        /// <param name="lastMessageSent"></param>
        /// <returns></returns>
        Task UpdateDialogLastMessage(string dialogId, string lastActivity, DateTime lastMessageSent);
    }

    public class DialogsManager : IDialogsManager
    {
        #region Fields

        private static readonly DateTime UnixEpoch = new DateTime(1970, 1, 1, 0, 0, 0, 0).ToLocalTime();

        private bool isReloadingDialogs;
        private readonly IQuickbloxClient quickbloxClient;

        #endregion

        #region Ctor

        public DialogsManager(IQuickbloxClient quickbloxClient)
        {
            this.quickbloxClient = quickbloxClient;
            quickbloxClient.ChatXmppClient.OnMessageReceived += MessagesClientOnOnMessageReceived;
            quickbloxClient.ChatXmppClient.OnSystemMessageReceived += MessagesClientOnOnSystemMessageReceived;
            quickbloxClient.ChatXmppClient.OnContactRemoved += ChatXmppClientOnOnContactRemoved;
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
                        if (dialog.Type == DialogType.PublicGroup) continue;

                        var dialogVm = DialogViewModel.FromDialog(dialog);

                        if (dialog.Type == DialogType.Private)
                        {
                            int otherUserId = dialogVm.OccupantIds.FirstOrDefault(o => o != currentUserId);
                            dialogVm.Name = GetUserNameFromContacts(otherUserId);
                        }

                        Dialogs.Add(dialogVm);
                    }

                    JoinAllGroupDialogs();
                    await UpdateDialogsStates();
                }
            }
            finally
            {
                isReloadingDialogs = false;
            }
        }

        public async Task UpdateDialogsStates()
        {
            await FixPrivateDialogsNamesAndImages();
            await LoadDialogImages(100);
        }

        public async Task UpdateDialogLastMessage(string dialogId, string lastActivity, DateTime lastMessageSent)
        {
            if (string.IsNullOrEmpty(dialogId)) return;

            var dialog = Dialogs.FirstOrDefault(d => d.Id == dialogId);
            if (dialog != null)
            {
                if (string.IsNullOrEmpty(lastActivity) && lastMessageSent == UnixEpoch) return;

                dialog.LastActivity = WebUtility.HtmlDecode(lastActivity);
                dialog.LastMessageSent = lastMessageSent;
                int itemIndex = Dialogs.IndexOf(dialog);
                Dialogs.Move(itemIndex, 0);
            }
            else // This is depricated now. We use Quickblox System messages instead. Left for backward compatibility (can be deleted)
            {
                await QmunicateLoggerHolder.Log(QmunicateLogLevel.Warn, "The dialog wasn't found in DialogsManager. Reloading dialogs.");
                await ReloadDialogs();
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

        private async void ChatXmppClientOnOnContactRemoved(object sender, Contact contact)
        {
            var currentUserId = SettingsManager.Instance.ReadFromSettings<int>(SettingsKeys.CurrentUserId);
            foreach (var dialogVm in Dialogs.Where(d => d.DialogType == DialogType.Private))
            {
                int otherUserId = dialogVm.OccupantIds.FirstOrDefault(o => o != currentUserId);
                if (otherUserId == contact.UserId)
                {
                    await Helpers.RunOnTheUiThread(()=> Dialogs.Remove(dialogVm));
                    break;
                }
            }
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

        private void JoinAllGroupDialogs()
        {
            int currentUserId = SettingsManager.Instance.ReadFromSettings<int>(SettingsKeys.CurrentUserId);

            foreach (DialogViewModel dialogVm in Dialogs)
            {
                if (dialogVm.DialogType == DialogType.Group)
                {
                    var groupChatManager = quickbloxClient.ChatXmppClient.GetGroupChatManager(dialogVm.XmppRoomJid, dialogVm.Id);
                    groupChatManager.JoinGroup(currentUserId.ToString());
                }
            }
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
                    if (!string.IsNullOrEmpty(user.CustomData))
                    {
                        var customData = JsonConvert.DeserializeObject<CustomData>(user.CustomData);
                        if(customData != null) dialogVm.Photo = customData.AvatarUrl;
                    }
                }
            }
        }

        private async Task LoadDialogImages(int? decodePixelWidth = null, int? decodePixelHeight = null)
        {
            var imagesService = ServiceLocator.Locator.Get<IImageService>();

            foreach (DialogViewModel dialogVm in Dialogs.Where(d => !string.IsNullOrEmpty(d.Photo)))
            {
                dialogVm.Image = await imagesService.GetPublicImage(dialogVm.Photo);
            }

            Parallel.ForEach(Dialogs.Where(d => string.IsNullOrEmpty(d.Photo) && d.PrivatePhotoId.HasValue), async (dialogVm, state) =>
            {
                if (dialogVm.PrivatePhotoId.HasValue)
                {
                    var imageBytes = await imagesService.GetPrivateImageBytes(dialogVm.PrivatePhotoId.Value);
                    if(imageBytes != null)
                    {
                        await CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(CoreDispatcherPriority.Normal, async () =>
                        dialogVm.Image = await ImageHelper.CreateBitmapImage(imageBytes, decodePixelWidth, decodePixelHeight));
                    }
                    
                }
            });
        }

        #endregion

    }
}
