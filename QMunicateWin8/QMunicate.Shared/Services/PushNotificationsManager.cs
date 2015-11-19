using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Windows.Networking.PushNotifications;
using QMunicate.Helper;
using Quickblox.Sdk;
using Quickblox.Sdk.GeneralDataModel.Models;
using Quickblox.Sdk.Modules.NotificationModule.Models;
using Quickblox.Sdk.Modules.NotificationModule.Requests;
using Environment = Quickblox.Sdk.Modules.NotificationModule.Models.Environment;

namespace QMunicate.Services
{
    public interface IPushNotificationsManager
    {
        Task UpdatePushTokenIfNeeded(PushNotificationChannel pushChannel);
        Task DeletePushToken();
        Task<bool> CreateSubscriptionIfNeeded();
        Task DeleteSubscription();
    }

    public class PushNotificationsManager : IPushNotificationsManager
    {
        #region Fields

        private IQuickbloxClient quickbloxClient;

        #endregion

        #region Ctor

        public PushNotificationsManager(IQuickbloxClient quickbloxClient)
        {
            this.quickbloxClient = quickbloxClient;
        }

        #endregion

        #region IPushNotificationsManager Members

        public async Task UpdatePushTokenIfNeeded(PushNotificationChannel pushChannel)
        {
            string tokenHash = Helpers.ComputeMD5(pushChannel.Uri);
            string storedTokenHash = SettingsManager.Instance.ReadFromSettings<string>(SettingsKeys.PushTokenHash);
            if (tokenHash != storedTokenHash)
            {
                string storedTokenId = SettingsManager.Instance.ReadFromSettings<string>(SettingsKeys.PushTokenId);
                if (!string.IsNullOrEmpty(storedTokenId))
                    await quickbloxClient.NotificationClient.DeletePushTokenAsync(storedTokenId);

                var settings = new CreatePushTokenRequest()
                {
                    DeviceRequest =
                        new DeviceRequest() {Platform = Platform.windows_phone, Udid = Helpers.GetHardwareId()},
                    PushToken =
                        new PushToken()
                        {
                            Environment = Environment.production,
                            ClientIdentificationSequence = pushChannel.Uri
                        }
                };
                var createPushTokenResponse = await quickbloxClient.NotificationClient.CreatePushTokenAsync(settings);
                if (createPushTokenResponse.StatusCode == HttpStatusCode.Created)
                {
                    SettingsManager.Instance.WriteToSettings(SettingsKeys.PushTokenId,
                        createPushTokenResponse.Result.PushToken.PushTokenId);
                    SettingsManager.Instance.WriteToSettings(SettingsKeys.PushTokenHash, tokenHash);
                }
            }
        }

        public async Task DeletePushToken()
        {
            string storedTokenId = SettingsManager.Instance.ReadFromSettings<string>(SettingsKeys.PushTokenId);
            if (!string.IsNullOrEmpty(storedTokenId))
                await quickbloxClient.NotificationClient.DeletePushTokenAsync(storedTokenId);

            SettingsManager.Instance.DeleteFromSettings(SettingsKeys.PushTokenHash);
            SettingsManager.Instance.DeleteFromSettings(SettingsKeys.PushTokenId);
        }

        public async Task<bool> CreateSubscriptionIfNeeded()
        {
            int pushSubscriptionId = SettingsManager.Instance.ReadFromSettings<int>(SettingsKeys.PushSubscriptionId);
            if (pushSubscriptionId == default(int))
            {
                var createSubscriptionsResponse =
                    await quickbloxClient.NotificationClient.CreateSubscriptionsAsync(NotificationChannelType.mpns);
                if (createSubscriptionsResponse.StatusCode == HttpStatusCode.Created)
                {
                    var subscription = createSubscriptionsResponse.Result.FirstOrDefault();
                    if (subscription != null)
                    {
                        SettingsManager.Instance.WriteToSettings(SettingsKeys.PushSubscriptionId, subscription.Subscription.Id);
                    }
                    else
                    {
                        var subscriptions = await quickbloxClient.NotificationClient.GetSubscriptionsAsync();
                        if (subscriptions.StatusCode == HttpStatusCode.OK)
                        {
                            var subs = subscriptions.Result.FirstOrDefault(s => 
                                        s.Subscription != null && s.Subscription.NotificationChannel != null &&
                                        s.Subscription.NotificationChannel.Name == NotificationChannelType.mpns);
                            if (subs != null)
                                SettingsManager.Instance.WriteToSettings(SettingsKeys.PushSubscriptionId, subs.Subscription.Id);
                        }
                    }
                }
                else return false;
            }

            return true;
        }

        public async Task DeleteSubscription()
        {
            int pushSubscriptionId = SettingsManager.Instance.ReadFromSettings<int>(SettingsKeys.PushSubscriptionId);
            if (pushSubscriptionId != default(int))
            {
                await quickbloxClient.NotificationClient.DeleteSubscriptionsAsync(pushSubscriptionId);
                SettingsManager.Instance.DeleteFromSettings(SettingsKeys.PushSubscriptionId);
            }
        }

        #endregion

    }
}
