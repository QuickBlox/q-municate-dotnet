using System;
using QMunicate.Core.DependencyInjection;
using QMunicate.Core.Observable;
using QMunicate.Services;
using Quickblox.Sdk;
using Quickblox.Sdk.Builder;
using Quickblox.Sdk.GeneralDataModel.Filters;
using Quickblox.Sdk.GeneralDataModel.Models;
using Quickblox.Sdk.Modules.ChatModule.Requests;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using Windows.ApplicationModel.Core;
using Windows.UI.Core;
using Windows.UI.Xaml.Media.Imaging;

namespace QMunicate.ViewModels.PartialViewModels
{
    /// <summary>
    /// MessageCollectionViewModel acts as a holder for current dialog's messages.
    /// Allows to load them from a dialog or to add manually.
    /// Proper notifications messages are generated automatically.
    /// </summary>
    public class MessageCollectionViewModel : ObservableObject
    {
        #region Fields

        private ObservableCollection<DayOfMessages> messages;

        #endregion

        #region Ctor

        public MessageCollectionViewModel()
        {
            Messages = new ObservableCollection<DayOfMessages>();
        }

        #endregion

        #region Properties

        public ObservableCollection<DayOfMessages> Messages
        {
            get { return messages; }
            set { Set(ref messages, value); }
        }


        #endregion

        #region Public methods

        public async Task LoadMessages(string dialogId)
        {
            var retrieveMessagesRequest = new RetrieveMessagesRequest();
            var aggregator = new FilterAggregator();
            aggregator.Filters.Add(new FieldFilter<string>(() => new Message().ChatDialogId, dialogId));
            aggregator.Filters.Add(new SortFilter<long>(SortOperator.Desc, () => new Message().DateSent));
            retrieveMessagesRequest.Filter = aggregator;

            var quickbloxClient = ServiceLocator.Locator.Get<IQuickbloxClient>();

            var response = await quickbloxClient.ChatClient.GetMessagesAsync(retrieveMessagesRequest);
            if (response.StatusCode == HttpStatusCode.OK)
            {
                Messages.Clear();
                var messageList = new List<MessageViewModel>();
                for (int i = response.Result.Items.Length - 1; i >= 0; i--) // doing it in reverse order because we requested them from server in descending order
                {
                    var messageViewModel = await CreateMessageViewModelFromMessage(response.Result.Items[i]);
                    await GenerateProperNotificationMessages(messageViewModel, response.Result.Items[i]);
                    messageList.Add(messageViewModel);
                }
                InitializeMessagesFromList(messageList);
            }
        }

        public async Task AddNewMessage(MessageViewModel messageViewModel)
        {
            await AddMessage(messageViewModel);
        }

        public async Task AddNewMessage(Message message)
        {
            var messageViewModel = await CreateMessageViewModelFromMessage(message);
            await GenerateProperNotificationMessages(messageViewModel, message);
            await AddMessage(messageViewModel);
        }

        #endregion

        #region Private methods

        private void InitializeMessagesFromList(IEnumerable<MessageViewModel> messageList)
        {
            IEnumerable<DayOfMessages> groups =
            from msg in messageList
            group msg by msg.DateTime.Date into messageGroup
            select new DayOfMessages(messageGroup)
            {
                Date = messageGroup.Key
            };

            Messages = new ObservableCollection<DayOfMessages>(groups);
        }

        private async Task AddMessage(MessageViewModel messageViewModel)
        {
            if (CoreApplication.MainView.CoreWindow.Dispatcher.HasThreadAccess)
            {
                await AddMessageWithoutThreadAccessCheck(messageViewModel);
            }
            else
            {
                CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(CoreDispatcherPriority.Normal, async () =>
                {
                    await AddMessageWithoutThreadAccessCheck(messageViewModel);
                });
            }
        }

        private async Task AddMessageWithoutThreadAccessCheck(MessageViewModel messageViewModel)
        {
            var messageGroup = Messages.FirstOrDefault(msgGroup => msgGroup.Date.Date == messageViewModel.DateTime.Date);
            if (messageGroup == null)
            {
                messageGroup = new DayOfMessages { Date = messageViewModel.DateTime.Date };
                Messages.Add(messageGroup);
            }
            messageGroup.Add(messageViewModel);
        }

        #region Notification messages generation

        private async Task GenerateProperNotificationMessages(MessageViewModel messageViewModel, Message originalMessage)
        {
            switch (originalMessage.NotificationType)
            {
                case NotificationTypes.FriendsRequest:
                    messageViewModel.MessageText = await BuildFriendsRequestMessage(originalMessage, messageViewModel.MessageType);
                    break;

                case NotificationTypes.FriendsAccept:
                    messageViewModel.MessageText = BuildFriendsAcceptMessage(messageViewModel.MessageType);
                    break;

                case NotificationTypes.FriendsReject:
                    messageViewModel.MessageText = BuildFriendsRejectMessage(messageViewModel.MessageType);
                    break;

                case NotificationTypes.FriendsRemove:
                    messageViewModel.MessageText = await BuildFriedsRemoveMessage(originalMessage, messageViewModel.MessageType);
                    break;

                case NotificationTypes.GroupCreate:
                    messageViewModel.MessageText = await BuildGroupCreateMessage(originalMessage);
                    break;

                case NotificationTypes.GroupUpdate:
                    messageViewModel.MessageText = await BuildGroupUpdateMessage(originalMessage);
                    break;
            }
        }

        private async Task<string> BuildFriendsRequestMessage(Message message, MessageType messageType)
        {
            if (messageType == MessageType.Outgoing) return "Your request has been sent";

            var cachingQbClient = ServiceLocator.Locator.Get<ICachingQuickbloxClient>();
            var senderUser = await cachingQbClient.GetUserById(message.SenderId);

            return string.Format("{0} has sent a request to you", senderUser == null ? null : senderUser.FullName);
        }

        private string BuildFriendsAcceptMessage(MessageType messageType)
        {
            return messageType == MessageType.Outgoing ? "You have accepted a request" : "Your request has been accepted";
        }

        private string BuildFriendsRejectMessage(MessageType messageType)
        {
            return messageType == MessageType.Outgoing ? "You have rejected a request" : "Your request has been rejected";
        }

        private async Task<string> BuildFriedsRemoveMessage(Message message, MessageType messageType)
        {
            if (messageType == MessageType.Outgoing) return "You have deleted this contact";

            var cachingQbClient = ServiceLocator.Locator.Get<ICachingQuickbloxClient>();
            var senderUser = await cachingQbClient.GetUserById(message.SenderId);

            return string.Format("{0} has deleted you from the contact list", senderUser == null ? null : senderUser.FullName);
        }

        private async Task<string> BuildGroupCreateMessage(Message message)
        {
            var cachingQbClient = ServiceLocator.Locator.Get<ICachingQuickbloxClient>();
            var senderUser = await cachingQbClient.GetUserById(message.SenderId);

            var addedUsersBuilder = new StringBuilder();
            IList<int> occupantsIds = !message.AddedOccupantsIds.Any() ? message.OccupantsIds : message.AddedOccupantsIds;
            foreach (var userId in occupantsIds.Where(o => o != message.SenderId))
            {
                var user = await cachingQbClient.GetUserById(userId);
                if (user != null)
                    addedUsersBuilder.Append(user.FullName + ", ");
            }
            if (addedUsersBuilder.Length > 1)
                addedUsersBuilder.Remove(addedUsersBuilder.Length - 2, 2);

            return $"{(senderUser == null ? "" : senderUser.FullName)} has added {addedUsersBuilder} to the group chat";
        }

        private async Task<string> BuildGroupUpdateMessage(Message message)
        {
            var cachingQbClient = ServiceLocator.Locator.Get<ICachingQuickbloxClient>();
            var senderUser = await cachingQbClient.GetUserById(message.SenderId);

            string messageText = null;
            if (!string.IsNullOrEmpty(message.RoomName))
                messageText = $"{(senderUser == null ? "" : senderUser.FullName)} has changed the chat name to {message.RoomName}";

            if (!string.IsNullOrEmpty(message.RoomPhoto))
                messageText = $"{(senderUser == null ? "" : senderUser.FullName)} has changed the chat picture";

            if (message.AddedOccupantsIds.Any() || message.AddedOccupantsIds.Any())
            {
                var addedUsersBuilder = new StringBuilder();
                IList<int> occupantsIds = !message.AddedOccupantsIds.Any() ? message.OccupantsIds : message.AddedOccupantsIds;
                foreach (var userId in occupantsIds.Where(o => o != message.SenderId))
                {
                    var user = await cachingQbClient.GetUserById(userId);
                    if (user != null)
                        addedUsersBuilder.Append(user.FullName + ", ");
                }
                if (addedUsersBuilder.Length > 1)
                    addedUsersBuilder.Remove(addedUsersBuilder.Length - 2, 2);

                messageText = $"{(senderUser == null ? "" : senderUser.FullName)} has added {addedUsersBuilder} to the group chat";
            }

            if (message.DeletedOccupantsIds.Any())
            {
                var user = await cachingQbClient.GetUserById(message.DeletedOccupantsIds.First());
                messageText = $"{user?.FullName} has left";
            }

            if (message.DeletedId != 0)
            {
                var user = await cachingQbClient.GetUserById(message.DeletedId);
                messageText = $"{user?.FullName} has left";
            }

            return messageText;
        }

        #endregion

        private async Task<MessageViewModel> CreateMessageViewModelFromMessage(Message message)
        {
            var messageViewModel = new MessageViewModel
            {
                MessageText = WebUtility.HtmlDecode(message.MessageText),
                DateTime = message.DateSent.ToDateTime(),
                NotificationType = message.NotificationType,
                SenderId = message.SenderId
            };

            int currentUserId = SettingsManager.Instance.ReadFromSettings<int>(SettingsKeys.CurrentUserId);
            messageViewModel.MessageType = messageViewModel.SenderId == currentUserId ? MessageType.Outgoing : MessageType.Incoming;

            var cachingQbClient = ServiceLocator.Locator.Get<ICachingQuickbloxClient>();
            var senderUser = await cachingQbClient.GetUserById(message.SenderId);
            if (senderUser != null) messageViewModel.SenderName = senderUser.FullName;

            bool isImageAttached = message.Attachments != null && message.Attachments.Any() && !string.IsNullOrEmpty(message.Attachments[0].Url);

            if (isImageAttached)
            {
                var attachedImageUri = new Uri(message.Attachments[0].Url);
                await CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
                {
                    messageViewModel.AttachedImage = new BitmapImage(attachedImageUri);
                });
            }

            return messageViewModel;
        }

        #endregion

    }
}
