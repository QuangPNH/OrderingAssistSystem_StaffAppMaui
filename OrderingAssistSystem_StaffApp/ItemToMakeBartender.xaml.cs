using CommunityToolkit.Maui.Views;
using Newtonsoft.Json;
using OrderingAssistSystem_StaffApp.Models;
using OrderingAssistSystem_StaffApp.Services;
using System.Collections.ObjectModel;
using System.Text;

namespace OrderingAssistSystem_StaffApp;

public partial class ItemToMakeBartender : ContentPage
{
	private ObservableCollection<Models.Notification> Notifications { get; set; } = new ObservableCollection<Models.Notification>();
	private readonly HttpClient _client = new HttpClient(new HttpClientHandler
	{
		ServerCertificateCustomValidationCallback = (message, cert, chain, sslPolicyErrors) => true
	});
	Models.ConfigApi _config = new Models.ConfigApi();
	string role;

    private async Task SendNotificationAsync(string text)
    {
        var requestBody = new
        {
            text = text,
            action = "action_b"
        };

        var json = JsonConvert.SerializeObject(requestBody);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        _client.DefaultRequestHeaders.Clear();
        _client.DefaultRequestHeaders.Add("apikey", "0624d820-6616-430d-92a5-e68265a08593");

        var response = await _client.PostAsync("https://oas-noti-api-handling-hqb2gxavecakdtey.southeastasia-01.azurewebsites.net/api/notifications/requests", content);

        if (response.IsSuccessStatusCode)
        {
            Console.WriteLine("Notification sent successfully.");
        }
        else
        {
            Console.WriteLine($"Failed to send notification. Status code: {response.StatusCode}");
        }
    }
    public ItemToMakeBartender()
	{
		InitializeComponent();
		BindingContext = new ItemToMakeListViewModel();
		// Mock Notifications
		LoadNotifications();
		Authoriz();
		CalculateRemainingDays();
	}

	public void CalculateRemainingDays()
	{
		string loginInfoJson = Preferences.Get("LoginInfo", string.Empty);
		Employee emp = JsonConvert.DeserializeObject<Employee>(loginInfoJson);
		DateTime? subscribeEndDate = emp?.Owner?.SubscribeEndDate;
		if (subscribeEndDate.HasValue)
		{
			DateTime endDateWithGracePeriod = subscribeEndDate.Value.AddDays(7);
			TimeSpan remainingTime = endDateWithGracePeriod - DateTime.Now;
			if (remainingTime.Days <= 0)
			{
				INotificationRegistrationService notificationRegistrationService = DependencyService.Get<INotificationRegistrationService>();
				Application.Current.MainPage = new NavigationPage(new MainPage(notificationRegistrationService));
				Application.Current.MainPage.DisplayAlert("Expired", $"Your owner's subscription to the service is expired for over a week.", "Ok");
			}
		}
	}

	public async Task Authoriz()
	{
		//DisplayAlert("Status", Preferences.Get("LoginInfo", string.Empty), "OK");

		AuthorizeLogin authorizeLogin = new AuthorizeLogin(_client);

		var loginStatus = await authorizeLogin.CheckLogin();
		if (loginStatus.Equals("staff") || loginStatus.Equals("bartender"))
		{
			role = loginStatus;
		}
		else if (loginStatus.Equals("employee expired"))
		{
			LogOut();
		}
		else if (loginStatus.Equals("null"))
		{
			await DisplayAlert("Status", "Login info not found.", "OK");
			INotificationRegistrationService notificationRegistrationService = DependencyService.Get<INotificationRegistrationService>();
			Application.Current.MainPage = new NavigationPage(new MainPage(notificationRegistrationService));
		}
		else
		{
			await DisplayAlert("Status", "Something went wrong.", "OK");
		}
	}

	private async void LogOut()
	{
		Preferences.Remove("LoginInfo");
		INotificationRegistrationService notificationRegistrationService = DependencyService.Get<INotificationRegistrationService>();
		// Reset the MainPage to the login page
		Application.Current.MainPage = new NavigationPage(new MainPage(notificationRegistrationService));
		await Task.CompletedTask; // Ensure the method is still async.
	}

	private async void OnStartItemClicked(object sender, EventArgs e)
	{
		var button = sender as Button;
		var orderDetail = button?.CommandParameter as OrderDetail; // Cast to your Order type
		if (orderDetail != null)
		{
			var viewModel = BindingContext as ItemToMakeListViewModel;
			if (viewModel == null) return;

			// Get all matching order details with the same order date, menu item name, sugar, ice, and topping
			var matchingOrderDetails = viewModel.AllOrderDetails
				.Where(od => od.Order?.OrderDate == orderDetail.Order?.OrderDate &&
							 od.MenuItem?.ItemName == orderDetail.MenuItem?.ItemName &&
							 od.Sugar == orderDetail.Sugar &&
							 od.Ice == orderDetail.Ice &&
							 od.Topping == orderDetail.Topping)
				.ToList();

			// Update the status of the matching order details
			foreach (var detail in matchingOrderDetails)
			{
				detail.Status = false;
				detail.IsStartEnabled = true;
			}

			// Check for other order details with the same item name but different sugar, ice, and topping
			var otherOrderDetails = viewModel.AllOrderDetails
				.Where(od => od.MenuItem?.ItemName == orderDetail.MenuItem?.ItemName &&
							 (od.Sugar != orderDetail.Sugar || od.Ice != orderDetail.Ice || od.Topping != orderDetail.Topping))
				.ToList();

			foreach (var detail in otherOrderDetails)
			{
				// Check if there are no other order details with an order date further from now than the current order details
				var hasLaterOrder = viewModel.AllOrderDetails
					.Any(od => od.MenuItem?.ItemName == detail.MenuItem?.ItemName &&
							   od.Order?.OrderDate > detail.Order?.OrderDate);

				if (!hasLaterOrder)
				{
					detail.Status = false;
					detail.IsStartEnabled = true;
				}
			}

			// Update the status of the order details in the backend
			foreach (var detail in matchingOrderDetails.Concat(otherOrderDetails))
			{
				var uri = new Uri(_config.BaseAddress + $"OrderDetail/{detail.OrderDetailId}");
				var content = new StringContent(JsonConvert.SerializeObject(detail), Encoding.UTF8, "application/json");
				await _client.PutAsync(uri, content);
			}

			// Handle the PendingItem object here
			await DisplayAlert("Item Started", $"Starting item {orderDetail.MenuItem?.ItemName}.", "OK");
			await SendNotificationAsync($"Starting item {orderDetail.MenuItem?.ItemName}.");

			// Reload the to-make list
			viewModel.LoadOrderDetails();
		}
	}

	private async void LoadNotifications()
	{
		try
		{
			var loginInfoJson = Preferences.Get("LoginInfo", string.Empty);
			var employee = JsonConvert.DeserializeObject<Employee>(loginInfoJson);
			var managerId = employee?.ManagerId ?? 0;
			var uri = new Uri(_config.BaseAddress + "Notification/Employee/" + managerId);
			HttpResponseMessage response = await _client.GetAsync(uri);

			if (response.IsSuccessStatusCode)
			{
				string data = await response.Content.ReadAsStringAsync();
				var notifications = JsonConvert.DeserializeObject<List<Models.Notification>>(data);

				Notifications.Clear();
				if (notifications != null)
				{
					foreach (var notification in notifications)
					{
						Notifications.Add(notification);
					}
				}
			}
		}
		catch (Exception ex)
		{
			// Handle exceptions
			Console.WriteLine($"Error fetching notifications: {ex.Message}");
		}
	}

	private void OnBellIconClicked(object sender, EventArgs e)
	{
		// Create and display the popup
		var popup = new NotificationPopup(Notifications);
		this.ShowPopup(popup);
	}

	private void SwitchToPage(string pageKey, Func<Page> createPage)
	{
		var page = PageCache.Instance.GetOrCreatePage(pageKey, createPage);
		Application.Current.MainPage = new NavigationPage(page);
	}

	private void OnPendingOrdersClicked(object sender, EventArgs e)
	{
		CalculateRemainingDays();
		SwitchToPage("PendingOrders", () => new PendingOrderList());
	}

	private void OnMenuItemsClicked(object sender, EventArgs e)
	{
		CalculateRemainingDays();
		SwitchToPage("MenuItems", () => new MenuItemList());
	}

	private void OnItemToMakeClicked(object sender, EventArgs e)
	{
		CalculateRemainingDays();
		var viewModel = BindingContext as ItemToMakeListViewModel;
		viewModel?.LoadOrderDetails();
		Application.Current.MainPage.DisplayAlert("Loaded", "Items to make reloaded.", "OK");
	}

	private async void OnLogOutClicked(object sender, EventArgs e)
	{
		LogOut();
	}

	protected override void OnSizeAllocated(double width, double height)
	{
		base.OnSizeAllocated(width, height);

		// Check if the orientation is vertical
		if (width < height)
		{
			CalculateRemainingDays();
			var viewModel = BindingContext as ItemToMakeListViewModel;
			viewModel?.LoadOrderDetails();
		}
	}
}

