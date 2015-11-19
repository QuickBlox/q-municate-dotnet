using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Networking.PushNotifications;
using Windows.UI.Xaml.Navigation;
using QMunicate.Core.Command;
using QMunicate.Core.DependencyInjection;
using QMunicate.Core.Logger;
using QMunicate.Core.Observable;
using QMunicate.Models;
using QMunicate.Services;
using QMunicate.ViewModels.PartialViewModels;
using Quickblox.Sdk;
using Quickblox.Sdk.Modules.ChatModule.Models;

namespace QMunicate.ViewModels
{
    public class DialogsViewModel : ViewModel, IUserControlViewModel
    {
        #region Fields

        private DialogViewModel selectedDialog;

        #endregion

        #region Events

        public event EventHandler<string> SelectedDialogChanged;

        #endregion

        #region Ctor

        public DialogsViewModel()
        {
            DialogsManager = ServiceLocator.Locator.Get<IDialogsManager>();
            OpenChatCommand = new RelayCommand<object>(OpenChatCommandExecute, obj => !IsLoading);
        }

        #endregion

        #region Properties

        public IDialogsManager DialogsManager { get; set; }

        public DialogViewModel SelectedDialog
        {
            get { return selectedDialog; }
            set
            {
                if (Set(ref selectedDialog, value))
                {
                    SelectedDialogChanged?.Invoke(this, value?.Id);
                }
            }
        }

        public RelayCommand<object> OpenChatCommand { get; set; }

        #endregion

        #region Base members

        /// <summary>
        /// TODO: move most of this operation ot MainViewModel
        /// </summary>
        /// <param name="parameter"></param>
        /// <returns></returns>
        public async Task Initialize(object parameter)
        {
            IsLoading = true;
            var dialogsParameter = parameter as DialogsNavigationParameter;
            if (dialogsParameter != null)
            {
                //NavigationService.BackStack.Clear();
                await InitializeChat(dialogsParameter.CurrentUserId, dialogsParameter.Password);
            }
            await LoadDialogs();
            await InitializePush();
            IsLoading = false;
        }

        protected override void OnIsLoadingChanged()
        {
            OpenChatCommand.RaiseCanExecuteChanged();
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
            if (!dialogsManager.Dialogs.Any()) await dialogsManager.ReloadDialogs();
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

        private void OpenChatCommandExecute(object dialog)
        {
            var dialogVm = dialog as DialogViewModel;
            if (dialogVm == null) return;

            //if (dialogVm.DialogType == DialogType.Private)
            //{
            //    NavigationService.Navigate(ViewLocator.Chat, new ChatNavigationParameter { Dialog = dialogVm });
            //}
            //else if (dialogVm.DialogType == DialogType.Group)
            //{
            //    NavigationService.Navigate(ViewLocator.GroupChat, dialogVm.Id);
            //}


        }

        #endregion

    }
}
