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
			PendingOrderViewModel _pendingOrderViewModel = new PendingOrderViewModel();
			ItemToMakeListViewModel itemToMakeListViewModel = new ItemToMakeListViewModel();
			var a = action.ToString();
			if (action.ToString().Equals("Confirm"))
			{
				MainThread.BeginInvokeOnMainThread(() =>
				{
					var notification = new NotificationRequest
					{
						Title = "Notice",
						Description = $"New orders need to be confirmed paid.",
						ReturningData = "Dummy data",
						NotificationId = 1337
					};
					LocalNotificationCenter.Current.Show(notification);
				});
				_pendingOrderViewModel.LoadOrders();
			}
			else if (action.ToString().Equals("OrderSuccesses"))
			{
				MainThread.BeginInvokeOnMainThread(() =>
				{
					var notification = new NotificationRequest
					{
						Title = "Notice",
						Description = $"New items to make incoming.",
						ReturningData = "Dummy data",
						NotificationId = 1337
					};
					LocalNotificationCenter.Current.Show(notification);
				});
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
