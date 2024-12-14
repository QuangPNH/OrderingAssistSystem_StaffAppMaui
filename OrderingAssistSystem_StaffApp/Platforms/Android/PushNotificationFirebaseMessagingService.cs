using Android.App;
using Firebase.Messaging;
using OrderingAssistSystem_StaffApp.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OrderingAssistSystem_StaffApp.Platforms.Android
{

    [Service(Exported = false)]
    [IntentFilter(new[] { "com.google.firebase.MESSAGING_EVENT" })]
    public class PushNotificationFirebaseMessagingService : FirebaseMessagingService
    {
        IPushDemoNotificationActionService _notificationActionService;
        INotificationRegistrationService _notificationRegistrationService;
        IDeviceInstallationService _deviceInstallationService;
        int _messageId;

        IPushDemoNotificationActionService NotificationActionService =>
            _notificationActionService ?? (_notificationActionService = IPlatformApplication.Current.Services.GetService<IPushDemoNotificationActionService>());

        INotificationRegistrationService NotificationRegistrationService =>
            _notificationRegistrationService ?? (_notificationRegistrationService = IPlatformApplication.Current.Services.GetService<INotificationRegistrationService>());

        IDeviceInstallationService DeviceInstallationService =>
            _deviceInstallationService ?? (_deviceInstallationService = IPlatformApplication.Current.Services.GetService<IDeviceInstallationService>());

        public override void OnNewToken(string token)
        {
            DeviceInstallationService.Token = token;

            NotificationRegistrationService.RefreshRegistrationAsync()
                .ContinueWith((task) =>
                {
                    if (task.IsFaulted)
                        throw task.Exception;
                });
        }

        public override void OnMessageReceived(RemoteMessage message)
        {
            base.OnMessageReceived(message);
            var a = message.DescribeContents;
            var b = message.GetRawData;
            var c = message.GetNotification;
            if (message.Data.TryGetValue("action", out var messageAction))
                NotificationActionService.TriggerAction(messageAction);
        }
    }
}