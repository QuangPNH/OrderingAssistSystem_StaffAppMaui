using AzzanOrder.Data.Models;
using Newtonsoft.Json;
using OrderingAssistSystem_StaffApp.Models;
using OrderingAssistSystem_StaffApp.Services;
using Plugin.LocalNotification;
using System.Net.Http.Json;
namespace OrderingAssistSystem_StaffApp
{
    public partial class App : Application
    {
        private readonly HttpClient httpClient;
        public static PageCache PageCache { get; private set; }
        readonly IPushDemoNotificationActionService _actionService;
        public App(IPushDemoNotificationActionService service)
        {
            InitializeComponent();
            _actionService = service;
            _actionService.ActionTriggered += NotificationActionTriggered;
            INotificationRegistrationService serviceNoti = DependencyService.Get<INotificationRegistrationService>();
            MainPage = new AppShell();
            httpClient = new HttpClient();
        }
        void NotificationActionTriggered(object sender, OasStaffAppAction e)
        {
            ShowActionAlert(e);
        }

        public async Task<StaffNotiChannel> GetLatestStaffNotiChannelAsync(int managerId)
        {
            ConfigApi configApi = new ConfigApi();
            var url = configApi._apiUrl + $"StaffNotiChannels/latest/{managerId}";
            return await httpClient.GetFromJsonAsync<StaffNotiChannel>(url);
        }
        async void ShowActionAlert(OasStaffAppAction action)
        {
            PendingOrderViewModel _pendingOrderViewModel = new PendingOrderViewModel();
            ItemToMakeListViewModel itemToMakeListViewModel = new ItemToMakeListViewModel();
            var a = action.ToString();
            if (action.ToString().Equals("Confirm"))
            {
                var loginInfo = Preferences.Get("LoginInfo", string.Empty);
                Employee emp = JsonConvert.DeserializeObject<Employee>(loginInfo);
                if (emp != null)
                {
                    StaffNotiChannel _latestStaffNoti = await GetLatestStaffNotiChannelAsync(emp.ManagerId.Value);
                    if (_latestStaffNoti != null && _latestStaffNoti.IsSent == false)
                    {
                        MainThread.BeginInvokeOnMainThread(() =>
                        {
                            var notification = new NotificationRequest
                            {
                                Title = "Notice",
                                Description = _latestStaffNoti.Message,
                                ReturningData = "Dummy data",
                                NotificationId = 1337
                            };
                            LocalNotificationCenter.Current.Show(notification);
                        });
                    }
                }
                _pendingOrderViewModel.LoadOrders();
            }
            else if (action.ToString().Equals("OrderSuccesses"))
            {
                var loginInfo = Preferences.Get("LoginInfo", string.Empty);
                Employee emp = JsonConvert.DeserializeObject<Employee>(loginInfo);
                if (emp != null && emp.RoleId == 3)
                {
                    MainThread.BeginInvokeOnMainThread(() =>
                    {
                        var notification = new NotificationRequest
                        {
                            Title = "Notice",
                            Description = "New Order Being Made !.",
                            ReturningData = "Dummy data",
                            NotificationId = 1337
                        };
                        LocalNotificationCenter.Current.Show(notification);
                    });
                }
                itemToMakeListViewModel.LoadOrderDetails();
            }
			/*else
            {
                MainThread.BeginInvokeOnMainThread(() =>
                {
                    var notification = new NotificationRequest
                    {
                        Title = "Notice",
                        Description = "Unspecified.",
                        ReturningData = "Dummy data",
                        NotificationId = 1337
                    };
                    LocalNotificationCenter.Current.Show(notification);
                });
			}*/
        }
    }
}
