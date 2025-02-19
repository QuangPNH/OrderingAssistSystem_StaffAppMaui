﻿using OrderingAssistSystem_StaffApp.Models;
using System.Collections.ObjectModel;
using System.Globalization;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using CommunityToolkit.Maui.Views;
using Newtonsoft.Json;

using ConfigApi = OrderingAssistSystem_StaffApp.Models.ConfigApi;
using Twilio.TwiML.Voice;
using Application = Microsoft.Maui.Controls.Application;
using Task = System.Threading.Tasks.Task;
using OrderingAssistSystem_StaffApp.Services;
using System.Windows.Input;
using System.Text;
using AzzanOrder.Data.Models;
using static Android.Renderscripts.ScriptGroup;

namespace OrderingAssistSystem_StaffApp;

public partial class PendingOrderList : ContentPage
{
	private ObservableCollection<Models.Notification> Notifications { get; set; } = new ObservableCollection<Models.Notification>();
	private readonly HttpClient _client = new HttpClient(new HttpClientHandler
	{
		ServerCertificateCustomValidationCallback = (message, cert, chain, sslPolicyErrors) => true
	});
	string role = "";
	ConfigApi _config = new ConfigApi();
	public PendingOrderList()
	{
		Authoriz();
		InitializeComponent();
		BindingContext = new CombinedViewModel();
		var viewModel = BindingContext as CombinedViewModel;
		viewModel?.PendingOrder.LoadOrders();
		viewModel?.ItemToMake.LoadOrderDetails();
		LoadNotifications();
		CheckEmptyLists();
	}

	private void CheckEmptyLists()
	{
		var viewModel = BindingContext as CombinedViewModel;
		if (viewModel?.PendingOrder.Orders == null || (viewModel?.ItemToMake.GroupedMenuItems == null))
		{
			DisplayAlert("Info", "Nothing here.", "OK");
		}
	}
	private async Task<NotiChange> GetNotiChangeByTableNameAsync(string tableName)
	{
		var uri = new Uri(_config._apiUrl + $"NotiChanges/tableName/{tableName}");
		HttpResponseMessage response = await _client.GetAsync(uri);

		if (response.IsSuccessStatusCode)
		{
			string data = await response.Content.ReadAsStringAsync();
			return JsonConvert.DeserializeObject<NotiChange>(data);
		}
		else
		{
			Console.WriteLine($"Failed to fetch NotiChange. Status code: {response.StatusCode}");
			return null;
		}
	}

	//SendToClient
	private async Task SendNotificationAsync(string tableName, string message)
	{
		var notiChange = await GetNotiChangeByTableNameAsync(tableName);

		var newnotiChange = new NotiChange
		{
			id = notiChange.id,
			tableName = tableName, // Replace with actual table name if available
			message = message,
			isSent = false,
			DateCreated = DateTime.Now
		};

		var json = JsonConvert.SerializeObject(newnotiChange);
		var content = new StringContent(json, Encoding.UTF8, "application/json");

		var response = await _client.PutAsync(_config._apiUrl + $"NotiChanges/{notiChange.id}", content);

		if (response.IsSuccessStatusCode)
		{
			Console.WriteLine("Notification sent successfully.");
		}
		else
		{
			Console.WriteLine($"Failed to send notification. Status code: {response.StatusCode}");
		}
	}

	public async Task Authoriz()
	{
		//DisplayAlert("Status", Preferences.Get("LoginInfo", string.Empty), "OK");

		AuthorizeLogin authorizeLogin = new AuthorizeLogin(_client);

		var loginInfoJson = Preferences.Get("LoginInfo", string.Empty);
		var employee = JsonConvert.DeserializeObject<Employee>(loginInfoJson);

		var loginStatus = await authorizeLogin.CheckLogin();
		string welcome = Preferences.Get("isWelcome", string.Empty);
		if (loginStatus.Equals("staff") || loginStatus.Equals("bartender") || loginStatus.Equals("manager"))
		{
			if (welcome.Equals("false")) {
				Preferences.Set("isWelcome", "true");
				DisplayAlert("Hi", "Welcome " + employee.EmployeeName + "!", "OK");
                SwitchToPage("PendingOrders", () => new PendingOrderList());
            }
			role = loginStatus;
		}
		else if (loginStatus.Equals("employee expired"))
		{
			LogOut();
		}
		else if (loginStatus.Equals("null"))
		{
			await DisplayAlert("Status", "Login info not found or the internet isn't working.", "OK");
			INotificationRegistrationService notificationRegistrationService = DependencyService.Get<INotificationRegistrationService>();
			Application.Current.MainPage = new NavigationPage(new MainPage(notificationRegistrationService));
		}
		else
		{
			//await DisplayAlert("Status", "Something went wrong.", "OK");
		}
	}

	/*public async Task Authoriz()
    {
        // Get login info from shared preferences
        var loginInfoJson = Preferences.Get("LoginInfo", string.Empty);
        var employee = JsonConvert.DeserializeObject<Employee>(loginInfoJson);

        if (employee != null)
        {
            switch (employee.RoleId)
            {
                case 1:
                    role = "manager";
                    break;
                case 2:
                    role = "staff";
                    break;
                default:
                    await DisplayAlert("Status", "Something went wrong.", "OK");
                    return;
            }
        }
        else
        {
            await DisplayAlert("Status", "Login info not found.", "OK");
            INotificationRegistrationService notificationRegistrationService = DependencyService.Get<INotificationRegistrationService>();
            Application.Current.MainPage = new NavigationPage(new MainPage(notificationRegistrationService));
            return;
        }

        // Additional logic for expired employee
        if (employee.IsDelete == true)
        {
            LogOut();
        }
    }*/

	private async void LogOut()
	{
		Preferences.Remove("LoginInfo");
		INotificationRegistrationService notificationRegistrationService = DependencyService.Get<INotificationRegistrationService>();
		// Clear the page cache
		PageCache.Instance.ClearCache();
		// Reset the MainPage to the login page
		Application.Current.MainPage = new NavigationPage(new MainPage(notificationRegistrationService));
		await Task.CompletedTask; // Ensure the method is still async.
	}


	private async void OnConfirmOrderPaidClicked(object sender, EventArgs e)
	{
		// Get the Order object from the CommandParameter
		var button = sender as Button;
		var order = button?.CommandParameter as Order; // Cast to your Order type
		var viewModel = BindingContext as CombinedViewModel;
		if (order != null)
		{
			try
			{
				var uri = new Uri(_config.BaseAddress + $"Order/{order.OrderId}");
				order.Status = false; // Update the status in the order object
				var content = new StringContent(JsonConvert.SerializeObject(order), Encoding.UTF8, "application/json");
				HttpResponseMessage response = await _client.PutAsync(uri, content);

				if (response.IsSuccessStatusCode)
				{
					// Update the order status locally if needed
					order.Status = false;
					// Refresh the pending order list and item to make list
					DisplayAlert("Confirmed", $"Order {order.OrderId} has been confirmed paid.", "OK");


					await SendOrderConfirmationNotificationAsync();
					//Sent Noti to client
					await SendNotificationAsync(order.Table.Qr, $"Order {order.OrderId} has been confirmed paid.");
				}
				// Update member points if member exists
				if (order.Member != null)
				{
					var points = order.Cost / 1000;
					var updatePointsResponse = await _client.GetAsync($"{_config.BaseAddress}Member/UpdatePoints/memberId/point?memberId={order.Member.MemberId}&point={points}");
					if (!updatePointsResponse.IsSuccessStatusCode)
					{
						await DisplayAlert("Error", "Failed to update member points.", "OK");
						return;
					}
				}

                PageCache.Instance.ClearCache();
				viewModel?.PendingOrder.Orders.Remove(order);
				viewModel?.PendingOrder.LoadOrders();
				viewModel?.ItemToMake.LoadOrderDetails();
			}
			catch (Exception ex)
			{
				PageCache.Instance.ClearCache();
				viewModel?.PendingOrder.Orders.Remove(order);
				viewModel?.PendingOrder.LoadOrders();
				viewModel?.ItemToMake.LoadOrderDetails();
				// Handle exceptions
				Console.WriteLine($"Error updating order status: {ex.Message}");
				DisplayAlert("Confirmed", $"Order {order.OrderId} may has already been confirmed.", "OK");
			}
		}
		CheckEmptyLists();
	}

	private async void OnCancelOrderClicked(object sender, EventArgs e)
	{
		// Get the Order object from the CommandParameter
		var button = sender as Button;
		var order = button?.CommandParameter as Order; // Cast to your Order type
		var viewModel = BindingContext as CombinedViewModel;
		if (order != null)
		{
			try
			{
				var uri = new Uri(_config.BaseAddress + $"Order/{order.OrderId}");
				HttpResponseMessage response = await _client.DeleteAsync(uri);

				if (response.IsSuccessStatusCode)
				{
					// Remove the order from the local collection if needed
					viewModel?.PendingOrder.Orders.Remove(order);
					// Refresh the pending order list and item to make list
					viewModel?.PendingOrder.LoadOrders();
					viewModel?.ItemToMake.LoadOrderDetails();
					DisplayAlert("Cancelled", $"Order: {order.OrderId} has been cancelled.", "OK");

                    //notihere send to employee and client
                    //Sent Noti to client
                    await SendNotificationAsync(order.Table.Qr, $"Order: {order.OrderId} has been cancelled.");
					await SendOrderConfirmationNotificationAsync();

                }
			}
			catch (Exception ex)
			{
				// Remove the order from the local collection if needed
				viewModel?.PendingOrder.Orders.Remove(order);
				// Refresh the pending order list and item to make list
				viewModel?.PendingOrder.LoadOrders();
				viewModel?.ItemToMake.LoadOrderDetails();
				Console.WriteLine($"Error deleting order: {ex.Message}");
				DisplayAlert("Cancelled", $"Order: {order.OrderId} may has already been cancelled.", "OK");
			}
		}
		CheckEmptyLists();
	}


	private async void LoadNotifications()
	{
		try
		{
			var loginInfoJson = Preferences.Get("LoginInfo", string.Empty);
			var employee = JsonConvert.DeserializeObject<Employee>(loginInfoJson);
			var managerId = employee?.ManagerId ?? employee.EmployeeId;

			var uri = new Uri(_config.BaseAddress + $"Notification/Employee/{managerId}");
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
		/*var popup = new NotificationPopup(Notifications);
        this.ShowPopup(popup);*/
	}

	private void SwitchToPage(string pageKey, Func<Page> createPage)
	{
		var page = PageCache.Instance.GetOrCreatePage(pageKey, createPage);
		Application.Current.MainPage = new NavigationPage(page);
	}

	private void OnPendingOrdersClicked(object sender, EventArgs e)
	{
		var viewModel = BindingContext as CombinedViewModel;
		viewModel?.CalculateRemainingDays();
		viewModel?.PendingOrder.LoadOrders();
		CheckEmptyLists();
		Application.Current.MainPage.DisplayAlert("Loaded", "Pending items reloaded.", "OK");
	}

	private void OnMenuItemsClicked(object sender, EventArgs e)
	{
		var viewModel = BindingContext as CombinedViewModel;
		viewModel?.CalculateRemainingDays();
		SwitchToPage("MenuItems", () => new MenuItemList());
	}

	private void OnItemToMakeClicked(object sender, EventArgs e)
	{
		var viewModel = BindingContext as CombinedViewModel;
		viewModel?.CalculateRemainingDays();
		SwitchToPage("ItemToMakeBartender", () => new ItemToMakeBartender());
	}

	private void OnProcessingClicked(object sender, EventArgs e)
	{
		var viewModel = BindingContext as CombinedViewModel;
		viewModel?.CalculateRemainingDays();
		SwitchToPage("ItemsToMake", () => new ItemToMake());
	}

	private async void OnLogOutClicked(object sender, EventArgs e)
	{
		LogOut();
	}

	//Send from staff to bartend
	private async Task SendOrderConfirmationNotificationAsync()
	{
		var requestBody = new
		{
			text = "Order Confirmed !",
			action = "Confirm"
		};

		var json = JsonConvert.SerializeObject(requestBody);
		var content = new StringContent(json, Encoding.UTF8, "application/json");

		_client.DefaultRequestHeaders.Clear();
		_client.DefaultRequestHeaders.Add("apikey", "0624d820-6616-430d-92a5-e68265a08593");

		var uri = new Uri("https://pushnotiapis-arhzeqchhrg6adem.southeastasia-01.azurewebsites.net/api/notifications/requests");

		try
		{
			HttpResponseMessage response = await _client.PostAsync(uri, content);

			if (response.IsSuccessStatusCode)
			{
				Console.WriteLine("Notification sent successfully.");
			}
			else
			{
				Console.WriteLine($"Failed to send notification. Status code: {response.StatusCode}");
			}
		}
		catch (Exception ex)
		{
			Console.WriteLine($"Error sending notification: {ex.Message}");
		}
	}
	protected override void OnSizeAllocated(double width, double height)
	{
		base.OnSizeAllocated(width, height);

		// Check if the orientation is vertical
		if (width < height)
		{
			var viewModel = BindingContext as CombinedViewModel;
			viewModel?.CalculateRemainingDays();
			viewModel?.PendingOrder.LoadOrders();
		}
	}
}

public class CombinedViewModel : INotifyPropertyChanged
{
	public PendingOrderViewModel PendingOrder { get; set; }
	public ItemToMakeListViewModel ItemToMake { get; set; }
	public string Role { get; set; }
	public CombinedViewModel()
	{
		string loginInfoJson = Preferences.Get("LoginInfo", string.Empty);
		Employee emp = JsonConvert.DeserializeObject<Employee>(loginInfoJson);
		Role = emp.Role.RoleName;
		PendingOrder = new PendingOrderViewModel();
		ItemToMake = new ItemToMakeListViewModel();
		CalculateRemainingDays();
	}

	private string _remainingDaysMessage;
	public string RemainingDaysMessage
	{
		get => _remainingDaysMessage;
		set
		{
			_remainingDaysMessage = value;
			OnPropertyChanged();
		}
	}

	public void CalculateRemainingDays()
	{
		string loginInfoJson = Preferences.Get("LoginInfo", string.Empty);
		Employee emp = JsonConvert.DeserializeObject<Employee>(loginInfoJson);
		DateTime? subscribeEndDate = emp?.Owner?.SubscribeEndDate;
		if (subscribeEndDate.HasValue)
		{
			DateTime endDateWithGracePeriod = subscribeEndDate.Value.AddDays(8);
			TimeSpan remainingTime = endDateWithGracePeriod - DateTime.Now;
			if (remainingTime.Days <= 7 && !(remainingTime.Days == 0))
			{
				RemainingDaysMessage = $"Your owner's subscription to the service has expired.\nYou can still use the system for {remainingTime.Days} day(s).";
			}
			else if (remainingTime.Days <= 0)
			{
				Application.Current.MainPage.DisplayAlert("Expired", "Your owner's subscription to the service has expired for over a week.", "Ok");
			}
			else
			{
				RemainingDaysMessage = string.Empty;
			}
		}
	}

	public event PropertyChangedEventHandler PropertyChanged;
	protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
	{
		PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
	}
}


public class NotificationPopup : Popup
{
	public NotificationPopup(ObservableCollection<Models.Notification> notifications)
	{
		Content = new StackLayout
		{
			Padding = new Thickness(20),
			BackgroundColor = Colors.White,
			WidthRequest = 300,
			Children =
				{
					new Label { Text = "Notification History", FontAttributes = FontAttributes.Bold, FontSize = 18 },
					new ListView
					{
						ItemsSource = notifications,
						ItemTemplate = new DataTemplate(() =>
						{
							var textCell = new TextCell();
							textCell.SetBinding(TextCell.TextProperty, "Title");
							textCell.SetBinding(TextCell.DetailProperty, "Content");
							return textCell;
						})
					},
					new Button
					{
						Text = "Close",
						Command = new Command(() => this.Close())
					}
				}
		};
	}
}


public class PendingOrderViewModel
{
	private readonly HttpClient _client = new HttpClient(new HttpClientHandler
	{
		ServerCertificateCustomValidationCallback = (message, cert, chain, sslPolicyErrors) => true
	});
	private readonly ConfigApi _config = new ConfigApi();
	public ObservableCollection<Order> Orders { get; set; } = new ObservableCollection<Order>();

	public PendingOrderViewModel()
	{
		LoadOrders();
	}

	public async void LoadOrders()
	{
		try
		{
			var employeeInfoJson = Preferences.Get("LoginInfo", string.Empty);
			var employee = JsonConvert.DeserializeObject<Employee>(employeeInfoJson);
			var managerId = employee?.ManagerId ?? employee.EmployeeId;
			var uri = new Uri(_config.BaseAddress + $"Order/Employee/{managerId}");
			HttpResponseMessage response = await _client.GetAsync(uri);
			if (response.IsSuccessStatusCode)
			{
				string data = await response.Content.ReadAsStringAsync();
				var orders = JsonConvert.DeserializeObject<List<Order>>(data)
					?.Where(o => o.Status == null)
					.OrderBy(o => o.OrderDate)
					.ToList();
				Orders.Clear();
				if (orders != null)
				{
					foreach (var order in orders)
					{
						Orders.Add(order);
						foreach (var orderDetail in order.OrderDetails)
						{
							ParseOrderDetails(orderDetail);
						}
					}
				}
			}
		}
		catch (Exception ex)
		{
			// Handle exceptions
			Console.WriteLine($"Error fetching orders: {ex.Message}");
		}
	}

	private void ParseOrderDetails(OrderDetail orderDetail)
	{
		string[] attributes = orderDetail.Description?.Split(',') ?? Array.Empty<string>();

		foreach (var attribute in attributes)
		{
			string trimmed = attribute.Trim(); // Remove any leading/trailing whitespace

			if (trimmed.Contains("Ice", StringComparison.OrdinalIgnoreCase))
			{
				orderDetail.Ice = trimmed.Replace("Ice", "", StringComparison.OrdinalIgnoreCase).Trim();
			}
			else if (trimmed.Contains("Sugar", StringComparison.OrdinalIgnoreCase))
			{
				orderDetail.Sugar = trimmed.Replace("Sugar", "", StringComparison.OrdinalIgnoreCase).Trim();
			}
			else
			{
				orderDetail.Topping += (string.IsNullOrEmpty(orderDetail.Topping) ? "" : ", ") + trimmed;
			}
		}
		// Set default values if properties are empty
		orderDetail.Ice = string.IsNullOrEmpty(orderDetail.Ice) ? "none" : orderDetail.Ice;
		orderDetail.Sugar = string.IsNullOrEmpty(orderDetail.Sugar) ? "none" : orderDetail.Sugar;
		orderDetail.Topping = string.IsNullOrEmpty(orderDetail.Topping) ? "none" : orderDetail.Topping;
	}
}


public class StatusToTextConverter : IValueConverter
{
	public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
	{
		if (value is bool status)
		{
			return status ? "done" : "not done";
		}
		return "not done"; // Default in case of invalid value
	}

	public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
	{
		return value.ToString() == "done"; // Convert "done" back to true
	}
}


public class DetailStatusToTextConverter : IValueConverter
{
	public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
	{
		if (value == null)
		{
			return "not processed"; // null -> not processed
		}
		else if (value is bool status)
		{
			return status ? "done" : "processing"; // true -> done, false -> processing
		}
		return "not processed"; // Default in case of invalid value
	}

	public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
	{
		if (value.ToString() == "done")
			return true;
		if (value.ToString() == "processing")
			return false;
		return null; // Default: not processed
	}
}