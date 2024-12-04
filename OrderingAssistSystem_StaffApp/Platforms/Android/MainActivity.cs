using Android.App;
using Android.Content.PM;
using Android.OS;
using Android.Content;
using OrderingAssistSystem_StaffApp.Services;
using Firebase.Messaging;
namespace OrderingAssistSystem_StaffApp
{
    [Activity(
                Theme = "@style/Maui.SplashTheme",
                MainLauncher = true,
                LaunchMode = LaunchMode.SingleTop,
                ConfigurationChanges = ConfigChanges.ScreenSize | ConfigChanges.Orientation | ConfigChanges.UiMode | ConfigChanges.ScreenLayout | ConfigChanges.SmallestScreenSize | ConfigChanges.Density)]
    public class MainActivity : MauiAppCompatActivity, Android.Gms.Tasks.IOnSuccessListener
    {
        IPushDemoNotificationActionService? _notificationActionService;
        IDeviceInstallationService? _deviceInstallationService;

        IPushDemoNotificationActionService NotificationActionService =>
            _notificationActionService ??= IPlatformApplication.Current.Services.GetService<IPushDemoNotificationActionService>()
            ?? throw new InvalidOperationException("Service not found: IPushDemoNotificationActionService");

        IDeviceInstallationService DeviceInstallationService =>
            _deviceInstallationService ??= IPlatformApplication.Current.Services.GetService<IDeviceInstallationService>()
            ?? throw new InvalidOperationException("Service not found: IDeviceInstallationService");

        public void OnSuccess(Java.Lang.Object? result)
        {
            if (result != null)
            {
                DeviceInstallationService.Token = result.ToString();
            }
        }

        void ProcessNotificationsAction(Intent? intent)
        {
            try
            {
                if (intent?.HasExtra("action") == true)
                {
                    var action = intent.GetStringExtra("action");

                    if (!string.IsNullOrEmpty(action))
                        NotificationActionService.TriggerAction(action);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(ex.Message);
            }
        }

        protected override void OnNewIntent(Intent? intent)
        {
            base.OnNewIntent(intent);
            if (intent != null)
            {
                ProcessNotificationsAction(intent);
            }
        }

        protected override void OnCreate(Bundle? savedInstanceState)
        {
            base.OnCreate(savedInstanceState);

            if (DeviceInstallationService.NotificationsSupported)
                FirebaseMessaging.Instance.GetToken().AddOnSuccessListener(this);

            if (Intent != null)
            {
                ProcessNotificationsAction(Intent);
            }
        }
    }
}
