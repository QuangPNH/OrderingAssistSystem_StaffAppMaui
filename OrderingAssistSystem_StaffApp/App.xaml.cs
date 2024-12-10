using OrderingAssistSystem_StaffApp.Models;
using OrderingAssistSystem_StaffApp.Models;
using OrderingAssistSystem_StaffApp.Services;
using Plugin.LocalNotification;
namespace OrderingAssistSystem_StaffApp
{
    public partial class App : Application
    {
        public static PageCache PageCache { get; private set; }
        readonly IPushDemoNotificationActionService _actionService;
        public App(IPushDemoNotificationActionService service)
        {
            InitializeComponent();

            _actionService = service;
            _actionService.ActionTriggered += NotificationActionTriggered;
            INotificationRegistrationService serviceNoti = DependencyService.Get<INotificationRegistrationService>();
            MainPage = new AppShell();
            //MainPage = new AppTabbedPage();
        }
        void NotificationActionTriggered(object sender, OasStaffAppAction e)
        {
            ShowActionAlert(e);
        }

        void ShowActionAlert(OasStaffAppAction action)
        {
            MainThread.BeginInvokeOnMainThread(() =>
            {
                var notification = new NotificationRequest
                {
                    Title = "Push notifications demo",
                    Description = $"{action} action received.",
                    ReturningData = "Dummy data", // Returning data when tapped on notification.
                    NotificationId = 1337
                };
                LocalNotificationCenter.Current.Show(notification);
            });
        }
    }
}
