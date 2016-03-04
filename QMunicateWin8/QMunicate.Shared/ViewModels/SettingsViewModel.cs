using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using Windows.Networking.PushNotifications;
using Windows.Security.Credentials;
using Windows.Storage.Streams;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Imaging;
using Windows.UI.Xaml.Navigation;
using QMunicate.Core.Command;
using QMunicate.Core.DependencyInjection;
using QMunicate.Core.MessageService;
using QMunicate.Helper;
using QMunicate.Services;
using Quickblox.Sdk;
using Quickblox.Sdk.Modules.Models;
using Quickblox.Sdk.Modules.NotificationModule.Models;

namespace QMunicate.ViewModels
{
    public class SettingsViewModel : ViewModel
    {
        #region Fields

        private ImageSource userImage;
        private string userName;
        private bool isPushEnabled;
        private bool isSettingPushEnabledFromCode;
        private string packageVersion;

        #endregion

        #region Ctor

        public SettingsViewModel()
        {
            SignOutCommand = new RelayCommand(SignOutCommandExecute, () => !IsLoading);
            DeleteAccountCommand = new RelayCommand(DeleteAccountCommandExecute, () => !IsLoading);
            EditCommand = new RelayCommand(EditCommandExecute, () => !IsLoading);
        }

        #endregion

        #region Properties

        public ImageSource UserImage
        {
            get { return userImage; }
            set { Set(ref userImage, value); }
        }

        public string UserName
        {
            get { return userName; }
            set { Set(ref userName, value); }
        }

        public bool IsPushEnabled
        {
            get { return isPushEnabled; }
            set
            {
                if (!isSettingPushEnabledFromCode)
                {
                    ChangePushsEnabled(value);
                }
                else
                {
                    isSettingPushEnabledFromCode = false;
                }
                Set(ref isPushEnabled, value);
                
            }
        }

        public string PackageVersion
        {
            get { return packageVersion; }
            set { Set(ref packageVersion, value); }
        }

        public RelayCommand SignOutCommand { get; set; }

        public RelayCommand DeleteAccountCommand { get; set; }

        public RelayCommand EditCommand { get; set; }

        #endregion

        #region Navigation

        public async override void OnNavigatedTo(NavigationEventArgs e)
        {
            int pushSubscriptionId = SettingsManager.Instance.ReadFromSettings<int>(SettingsKeys.PushSubscriptionId);
            if (pushSubscriptionId != default(int))
            {
                isSettingPushEnabledFromCode = true;
                IsPushEnabled = true;
            }

            PackageVersion = Helpers.GetAppVersion();

            await LoadUserData();
        }

        #endregion

        #region Base members

        protected override void OnIsLoadingChanged()
        {
            SignOutCommand.RaiseCanExecuteChanged();
            DeleteAccountCommand.RaiseCanExecuteChanged();
            EditCommand.RaiseCanExecuteChanged();
        }

        #endregion

        #region Private methods

        private async Task LoadUserData()
        {
            IsLoading = true;
            var cachingQbClient = ServiceLocator.Locator.Get<ICachingQuickbloxClient>();
            var user = await cachingQbClient.GetUserById(SettingsManager.Instance.ReadFromSettings<int>(SettingsKeys.CurrentUserId));
            if (user != null)
            {
                UserName = user.FullName;
                if (user.BlobId.HasValue)
                {
                    var imageService = ServiceLocator.Locator.Get<IImageService>();
                    UserImage = await imageService.GetPrivateImage(user.BlobId.Value, 100);
                }
            }
            IsLoading = false;
        }

        private async void ChangePushsEnabled(bool newValue)
        {
            IsLoading = true;

            var pushNotificationsManager = ServiceLocator.Locator.Get<IPushNotificationsManager>();

            if (newValue)
            {
                SettingsManager.Instance.WriteToSettings(SettingsKeys.UserDisabledPush, false);

                var pushChannel = await PushNotificationChannelManager.CreatePushNotificationChannelForApplicationAsync();
                await pushNotificationsManager.UpdatePushTokenIfNeeded(pushChannel);

                bool isEnabled = await pushNotificationsManager.CreateSubscriptionIfNeeded();
                if (!isEnabled)
                {
                    isSettingPushEnabledFromCode = true;
                    IsPushEnabled = false;
                }
            }
            else
            {
                SettingsManager.Instance.WriteToSettings(SettingsKeys.UserDisabledPush, true);
                await pushNotificationsManager.DeleteSubscription();
            }
            IsLoading = false;
        }

        private async void SignOutCommandExecute()
        {
            IsLoading = true;
            var messageService = ServiceLocator.Locator.Get<IMessageService>();
            DialogCommand logoutCommand = new DialogCommand("logout", new RelayCommand(SignOut));
            DialogCommand cancelCommand = new DialogCommand("cancel", new RelayCommand(() => { IsLoading = false; }), false, true);
            await messageService.ShowAsync("Logout", "Do you really want to logout?", new [] {logoutCommand, cancelCommand});
        }

        private async void SignOut()
        {
            IsLoading = true;

            await TurnOffPushNotifications();

            QuickbloxClient.ChatXmppClient.Disconnect();
            await QuickbloxClient.AuthenticationClient.DeleteSessionAsync(QuickbloxClient.Token);
            SettingsManager.Instance.DeleteFromSettings(SettingsKeys.CurrentUserId);

            ServiceLocator.Locator.Get<IDialogsManager>().Dialogs.Clear();

            ServiceLocator.Locator.Get<IImageService>().ClearImagesCache();

            ServiceLocator.Locator.Get<ICachingQuickbloxClient>().ClearUsersCache();

            ServiceLocator.Locator.Get<ICredentialsService>().DeleteSavedCredentials();
            
            IsLoading = false;
            NavigationService.Navigate(ViewLocator.First);
            NavigationService.BackStack.Clear();
        }

        private async Task TurnOffPushNotifications()
        {
            var pushNotificationsManager = ServiceLocator.Locator.Get<IPushNotificationsManager>();
            await pushNotificationsManager.DeleteSubscription();
            await pushNotificationsManager.DeletePushToken();

            SettingsManager.Instance.DeleteFromSettings(SettingsKeys.UserDisabledPush);
        }

        private async void DeleteAccountCommandExecute()
        {

        }

        private void EditCommandExecute()
        {
            NavigationService.Navigate(ViewLocator.SettingsEdit);
        }

        #endregion
    }
}
