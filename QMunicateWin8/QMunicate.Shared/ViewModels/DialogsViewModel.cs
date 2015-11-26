using QMunicate.Core.Command;
using QMunicate.Core.DependencyInjection;
using QMunicate.Helper;
using QMunicate.Models;
using Quickblox.Sdk;
using Quickblox.Sdk.Modules.ChatModule.Models;
using System;
using System.Linq;
using System.Threading.Tasks;
using Windows.Networking.PushNotifications;
using Windows.UI.Xaml.Navigation;
using QMunicate.Core.Logger;
using QMunicate.Services;
using QMunicate.ViewModels.PartialViewModels;
using Quickblox.Sdk.Logger;

namespace QMunicate.ViewModels
{
    public class DialogsViewModel : ViewModel
    {
        #region Ctor

        public DialogsViewModel()
        {
            DialogsManager = ServiceLocator.Locator.Get<IDialogsManager>();
            OpenChatCommand = new RelayCommand<object>(OpenChatCommandExecute, obj => !IsLoading);
            NewMessageCommand = new RelayCommand(NewMessageCommandExecute, () => !IsLoading);
            SearchCommand = new RelayCommand(SearchCommandExecute, () => !IsLoading);
            SettingsCommand = new RelayCommand(SettingsCommandExecute, () => !IsLoading);
            InviteFriendsCommand = new RelayCommand(InviteFriendsCommandExecute, () => !IsLoading);
        }

        #endregion

        #region Properties

        public IDialogsManager DialogsManager { get; set; }

        public RelayCommand<object> OpenChatCommand { get; set; }

        public RelayCommand NewMessageCommand { get; set; }

        public RelayCommand SearchCommand { get; set; }

        public RelayCommand SettingsCommand { get; set; }

        public RelayCommand InviteFriendsCommand { get; set; }

        #endregion

        #region Navigation

        public async override void OnNavigatedTo(NavigationEventArgs e)
        {
            IsLoading = true;
            var parameter = e.Parameter as DialogsNavigationParameter;
            if (parameter != null && e.NavigationMode != NavigationMode.Back)
            {
                NavigationService.BackStack.Clear();

                var previousSessionUserId = SettingsManager.Instance.ReadFromSettings<int>(SettingsKeys.CurrentUserId);
                if (previousSessionUserId != parameter.CurrentUserId)
                {
                    ResetPushSettings();
                }

                SettingsManager.Instance.WriteToSettings(SettingsKeys.CurrentUserId, parameter.CurrentUserId);

                await InitializeChat(parameter.CurrentUserId, parameter.Password);
            }
            await LoadDialogs();
            await InitializePush();
            IsLoading = false;
        }

        #endregion

        #region Base members

        protected override void OnIsLoadingChanged()
        {
            OpenChatCommand.RaiseCanExecuteChanged();
            NewMessageCommand.RaiseCanExecuteChanged();
            SearchCommand.RaiseCanExecuteChanged();
            SettingsCommand.RaiseCanExecuteChanged();
            InviteFriendsCommand.RaiseCanExecuteChanged();
        }

        #endregion

        #region Private methods

        private async Task InitializeChat(int userId, string password)
        {
            if (!QuickbloxClient.ChatXmppClient.IsConnected)
            {
                await QuickbloxClient.ChatXmppClient.Connect(userId, password);
                QuickbloxClient.ChatXmppClient.ReloadContacts();
            }
        }

        private async Task LoadDialogs()
        {
            var dialogsManager = ServiceLocator.Locator.Get<IDialogsManager>();
            if(!dialogsManager.Dialogs.Any()) await dialogsManager.ReloadDialogs();
            dialogsManager.JoinAllGroupDialogs();
        }

        #region Push notifications

        private async Task InitializePush()
        {
            var pushChannel = await PushNotificationChannelManager.CreatePushNotificationChannelForApplicationAsync();
            pushChannel.PushNotificationReceived += PushChannelOnPushNotificationReceived;

            var pushNotificationsManager = ServiceLocator.Locator.Get<IPushNotificationsManager>();
            await pushNotificationsManager.UpdatePushTokenIfNeeded(pushChannel);

            bool userDisabledPush = SettingsManager.Instance.ReadFromSettings<bool>(SettingsKeys.UserDisabledPush);
            if (!userDisabledPush)
                await pushNotificationsManager.CreateSubscriptionIfNeeded();
        }

        private async void PushChannelOnPushNotificationReceived(PushNotificationChannel sender, PushNotificationReceivedEventArgs args)
        {
            await QmunicateLoggerHolder.Log(QmunicateLogLevel.Debug, "Push notification was received.");
        }

        #endregion

        private void ResetPushSettings()
        {
            SettingsManager.Instance.DeleteFromSettings(SettingsKeys.UserDisabledPush);
            SettingsManager.Instance.DeleteFromSettings(SettingsKeys.PushSubscriptionId);
            SettingsManager.Instance.DeleteFromSettings(SettingsKeys.PushTokenId);
            SettingsManager.Instance.DeleteFromSettings(SettingsKeys.PushTokenHash);
        }

        private void OpenChatCommandExecute(object dialog)
        {
            var dialogVm = dialog as DialogViewModel;
            if (dialogVm == null) return;

            if (dialogVm.DialogType == DialogType.Private)
            {
                NavigationService.Navigate(ViewLocator.Chat, new ChatNavigationParameter { Dialog = dialogVm });
            }
            else if (dialogVm.DialogType == DialogType.Group)
            {
                NavigationService.Navigate(ViewLocator.GroupChat, dialogVm.Id);
            }


        }

        private async void NewMessageCommandExecute()
        {
            NavigationService.Navigate(ViewLocator.NewMessage);
        }

        private void SearchCommandExecute()
        {
            NavigationService.Navigate(ViewLocator.Search);
        }

        private void SettingsCommandExecute()
        {
            NavigationService.Navigate(ViewLocator.Settings);
        }

        private void InviteFriendsCommandExecute()
        {

        }

        #endregion

    }
}
