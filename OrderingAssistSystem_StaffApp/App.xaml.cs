using OrderingAssistSystem_StaffApp.Models;
using OrderingAssistSystem_StaffApp.Models;
using OrderingAssistSystem_StaffApp.Services;
namespace OrderingAssistSystem_StaffApp
{
    public partial class App : Application
    {
        public static PageCache PageCache { get; private set; }
        readonly IPushDemoNotificationActionService _actionService;
        public App(IPushDemoNotificationActionService service, INotificationRegistrationService serviceNoti)
        {
            InitializeComponent();

            _actionService = service;
            _actionService.ActionTriggered += NotificationActionTriggered;

            //MainPage = new AppShell();
            MainPage = new NavigationPage(new MainPage(serviceNoti));

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
                Windows[0].Page?.DisplayAlert("Push notifications demo", $"{action} action received.", "OK")
                    .ContinueWith((task) =>
                    {
                        if (task.IsFaulted)
                            throw task.Exception;
                    });
            });
        }
    }
}
