using AzzanOrder.Data.Models;
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
	string role = "";
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
    private async Task SendNotificationAsync(string tableName, string message)
    {
        var notiChange = await GetNotiChangeByTableNameAsync(tableName);

        var newnotiChange = new NotiChange
        {
            id = notiChange.id,
            tableName = tableName,
            message = message,
            isSent = true,
            DateCreated = DateTime.Now
        };

        var json = JsonConvert.SerializeObject(notiChange);
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
    public ItemToMakeBartender()
	{
        Authoriz();
        InitializeComponent();
		BindingContext = new ItemToMakeListViewModel();
		// Mock Notifications
		LoadNotifications();
		
		CalculateRemainingDays();
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

    private async void OnStartItemClicked(object sender, EventArgs e)
    {
        var button = sender as Button;
        //pending items here

        var itemToMake = button?.CommandParameter as OrderDetail; // Cast to your Order type
        if (itemToMake != null)
        {
            var viewModel = BindingContext as ItemToMakeListViewModel;
            //viewModel.LoadOrderDetails();

            if (viewModel == null) return;

            viewModel.LoadOrderDetails();

            // Get all matching order details with the same order date, menu item name, sugar, ice, and topping
            var matchingOrderDetails = viewModel.AllOrderDetails
                .Where(od => /*od.Order?.OrderDate == itemToMake.Order?.OrderDate &&*/ //Not supposed to care about time though when start any item
                             od.MenuItem?.ItemName == itemToMake.MenuItem?.ItemName &&
                             od.Sugar == itemToMake.Sugar &&
                             od.Ice == itemToMake.Ice &&
                             od.Topping == itemToMake.Topping &&
                             od.Status == null)
                .ToList();

            if (matchingOrderDetails.Count == 0)
            {
                await DisplayAlert("Conflict", "Item may has already been started by other bartenders.", "OK");
                return;
            }

            // Update the status of the matching order details
            foreach (var detail in matchingOrderDetails)
            {
                detail.Status = false;
                detail.IsStartEnabled = false;
            }

            // Update the status of the order details in the backend
            foreach (var detail in matchingOrderDetails)
            {
                var uri = new Uri(_config.BaseAddress + $"OrderDetail/{detail.OrderDetailId}");
                var content = new StringContent(JsonConvert.SerializeObject(detail), Encoding.UTF8, "application/json");
                await _client.PutAsync(uri, content);
            }

            string jsonStartedItems = JsonConvert.SerializeObject(itemToMake);
            Preferences.Set("IStartedThis", jsonStartedItems);

            // Handle the PendingItem object here
            await DisplayAlert("Item Started", $"Starting item {itemToMake.MenuItem?.ItemName}.", "OK");
            await SendNotificationAsync(matchingOrderDetails.FirstOrDefault().Order.Table.Qr, $"Starting item {itemToMake.MenuItem?.ItemName}.");
            await SendOrderConfirmationNotificationAsync();
            // Reload the to-make list
            viewModel.LoadOrderDetails();
        }
    }

    private async Task SendOrderConfirmationNotificationAsync()
    {
        var requestBody = new
        {
            text = "Order Finished !",
            action = "OrderSuccessesStaff"
        };

        var json = JsonConvert.SerializeObject(requestBody);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        _client.DefaultRequestHeaders.Clear();
        _client.DefaultRequestHeaders.Add("apikey", "0624d820-6616-430d-92a5-e68265a08593");

        var uri = new Uri("https://oas-noti-api-handling-hqb2gxavecakdtey.southeastasia-01.azurewebsites.net/api/notifications/requests");

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

    private async void LoadNotifications()
	{
		try
		{
			var loginInfoJson = Preferences.Get("LoginInfo", string.Empty);
			var employee = JsonConvert.DeserializeObject<Employee>(loginInfoJson);
			var managerId = employee?.ManagerId ?? employee.EmployeeId;
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

