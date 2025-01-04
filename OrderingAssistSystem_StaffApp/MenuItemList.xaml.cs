
using CommunityToolkit.Maui.Views;
using Microsoft.Maui.Controls;
using Newtonsoft.Json;
using OrderingAssistSystem_StaffApp.Models;
using OrderingAssistSystem_StaffApp.Services;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Net.Http.Json;
using System.Runtime.CompilerServices;
using System.Text;
using System.Windows.Input;
using Twilio.TwiML.Voice;
using Application = Microsoft.Maui.Controls.Application;
using MenuItem = OrderingAssistSystem_StaffApp.Models.MenuItem;
using Task = System.Threading.Tasks.Task;
//using Twilio.TwiML.Voice;

namespace OrderingAssistSystem_StaffApp;

public partial class MenuItemList : ContentPage
{
	private ObservableCollection<Models.Notification> Notifications { get; set; } = new ObservableCollection<Models.Notification>();
	private readonly HttpClient _client = new HttpClient(new HttpClientHandler
	{
		ServerCertificateCustomValidationCallback = (message, cert, chain, sslPolicyErrors) => true
	});
	ConfigApi _config = new ConfigApi();
	string role = "";
    public ObservableCollection<CartItem> CartItems { get; set; } = new ObservableCollection<CartItem>();

	private int _currentPage = 1;
	private const int _itemsPerPage = 5;

	private void OnNextPageClicked(object sender, EventArgs e)
	{
		var viewModel = BindingContext as MenuItemListViewModel;
		_currentPage++;
		Preferences.Set("CurrentPage", _currentPage);
		viewModel.FilterMenuItems();
	}

	private void OnPreviousPageClicked(object sender, EventArgs e)
	{
		var viewModel = BindingContext as MenuItemListViewModel;
		if (_currentPage > 1)
		{
			_currentPage--;
			Preferences.Set("CurrentPage", _currentPage);
			viewModel.FilterMenuItems();
		}
	}


	public MenuItemList()
	{
        Authoriz();
        InitializeComponent();
		LoadNotifications();
		CalculateRemainingDays();
		BindingContext = new MenuItemListViewModel();
		Preferences.Set("CurrentPage", _currentPage);
		Preferences.Set("ItemsPerPage", _itemsPerPage);
	}


	private async void OnCheckBoxTapped(object sender, EventArgs e)
	{
		if (sender is CheckBox checkBox && checkBox.BindingContext is MenuItem menuItem)
		{
			checkBox.IsChecked = !(bool)menuItem.IsAvailable;
			menuItem.IsAvailable = checkBox.IsChecked;
			HandleIsAvailableChanged(menuItem);
		}
	}

	private void OnSearchButtonPressed(object sender, EventArgs e)
	{
		var viewModel = BindingContext as MenuItemListViewModel;
		if (viewModel != null)
		{
			viewModel.FilterMenuItems();
		}
	}

	private async void HandleIsAvailableChanged(MenuItem menuItem)
	{
		try
		{
			var a = menuItem.AvailableDrinkToppings;
			var b = menuItem.MenuCategories;
			menuItem.AvailableDrinkToppings = null;
			menuItem.MenuCategories = null;
			var httpClient = new HttpClient();
			var json = JsonConvert.SerializeObject(menuItem, new JsonSerializerSettings
			{
				ReferenceLoopHandling = ReferenceLoopHandling.Ignore,
				NullValueHandling = NullValueHandling.Ignore
			});
			menuItem.AvailableDrinkToppings = a;
			menuItem.MenuCategories = b;
			var content = new StringContent(json, Encoding.UTF8, "application/json");
			var response = await _client.PutAsync(_config.BaseAddress + "MenuItem/Update", content);

			if (!response.IsSuccessStatusCode)
			{
				// Handle failure (e.g., revert the status or display an error)
				if (Application.Current?.MainPage != null)
				{
					await Application.Current.MainPage.DisplayAlert("Error", "Failed to update menu item. Status code: {response.StatusCode}", "OK");
				}
			}
			else
			{
				await Application.Current.MainPage.DisplayAlert("Error", $"Update menu item status success.", "OK");
			}
		}
		catch (Exception ex)
		{
			if (Application.Current?.MainPage != null)
			{
				await Application.Current.MainPage.DisplayAlert("Error", $"Exception: {ex.Message}", "OK");
			}
		}
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
            await DisplayAlert("Status", "Login info not found or the internet isn't working.", "OK");
            INotificationRegistrationService notificationRegistrationService = DependencyService.Get<INotificationRegistrationService>();
            Application.Current.MainPage = new NavigationPage(new MainPage(notificationRegistrationService));
        }
		else
		{
			await DisplayAlert("Status", "Something went wrong.", "OK");
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
					//await DisplayAlert("Status", "Something went wrong.", "OK");
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
	}
*/
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


	//Add to cart
	private void OnAddToCartClicked(object sender, EventArgs e)
	{
		if (sender is Button button && button.BindingContext is MenuItem menuItem)
		{
			if (!(bool)menuItem.IsAvailable)
			{
				Application.Current.MainPage.DisplayAlert("Error", "Cannot add disabled items to cart.", "OK");
				return;
			}
			// Get selected sugar and ice levels
			string sugar = string.IsNullOrEmpty(menuItem.Sugar?.ToLower()) ? "normal Sugar" : $"{menuItem.Sugar} Sugar";
			string ice = string.IsNullOrEmpty(menuItem.Ice?.ToLower()) ? "normal Ice" : $"{menuItem.Ice} Ice";

			// Get selected toppings
			var selectedToppings = menuItem.AvailableDrinkToppings
				.Where(topping => topping.IsSelected)
				.Select(topping => topping.ItemName)
				.ToList();
			string toppingsList = selectedToppings.Any() ? string.Join(", ", selectedToppings) : "";

			// Combine into a single string
			string combinedPreferences = $"{ice}, {sugar}, {toppingsList}";

			// Check if the item with the same properties already exists in the cart
			var existingCartItem = CartItems?.FirstOrDefault(item =>
	item.ItemName == menuItem.ItemName &&
	item.Sugar == menuItem.Sugar &&
	item.Ice == menuItem.Ice &&
	item.Topping == toppingsList);

			if (existingCartItem != null)
			{
                string cartJson = Preferences.Get("Cart", "[]");
                var cartItems = JsonConvert.DeserializeObject<ObservableCollection<CartItem>>(cartJson);

				// If it exists, increase the quantity
				existingCartItem.Quantity = (int)menuItem.Quantity;




				CartItems.Remove(existingCartItem);
                CartItems.Add(new CartItem
                {
                    id = menuItem.MenuItemId,
                    ItemName = menuItem.ItemName,
                    Quantity = (int)menuItem.Quantity,
                    Sugar = menuItem.Sugar,
                    Ice = menuItem.Ice,
                    Topping = toppingsList,
                    Description = combinedPreferences,
                    Price = menuItem.Price ?? 0
                });
                RemoveCartItem(existingCartItem);
                UpdateTotalPrice();
                SaveCartToPreferences();
            }
			else
			{
				// If it doesn't exist, add a new item to the cart
				CartItems.Add(new CartItem
				{
					id = menuItem.MenuItemId,
					ItemName = menuItem.ItemName,
					Quantity = (int)menuItem.Quantity,
					Sugar = menuItem.Sugar,
					Ice = menuItem.Ice,
					Topping = toppingsList,
					Description = combinedPreferences,
					Price = menuItem.Price ?? 0
				});
			}
			UpdateTotalPrice();
			SaveCartToPreferences();
		}
	}

	private async void OnCreateOrderClicked(object sender, EventArgs e)
	{
		Member member = null;

		if (!string.IsNullOrEmpty(PhoneNumberEntry.Text?.Trim()))
		{
			// Check if phone number format is correct
			var phoneNumber = PhoneNumberEntry.Text.Trim();
			if (!System.Text.RegularExpressions.Regex.IsMatch(phoneNumber, @"^0[1-9]\d{8,14}$"))
			{
				await DisplayAlert("Error", "Invalid phone number format.", "OK");
				return;
			}

			try
			{
				// Get member by phone number
				var memberResponse = await _client.GetAsync($"{_config.BaseAddress}Member/Phone/{phoneNumber}");
				if (!memberResponse.IsSuccessStatusCode)
				{
					// Register new member
					var newMember = new Member { Phone = phoneNumber, IsDelete = false, Gmail = "" };
					var registerResponse = await _client.PostAsJsonAsync($"{_config.BaseAddress}Member/Add", newMember);
					if (!registerResponse.IsSuccessStatusCode)
					{
						await DisplayAlert("Error", "Failed to register new member.", "OK");
						return;
					}
					var registerData = await registerResponse.Content.ReadAsStringAsync();
					member = JsonConvert.DeserializeObject<Member>(registerData);
					await DisplayAlert("Registered", "Member " + member.Phone + " registered.", "OK");
				}
				else
				{
					var memberData = await memberResponse.Content.ReadAsStringAsync();
					member = JsonConvert.DeserializeObject<Member>(memberData);
				}
			}
			catch (Exception ex)
			{
				await DisplayAlert("Error", $"An error occurred while fetching or registering the member: {ex.Message}", "OK");
				return;
			}
		}

		try
		{
			var loginInfoJson = Preferences.Get("LoginInfo", string.Empty);
			var employee = JsonConvert.DeserializeObject<Employee>(loginInfoJson);
			var managerId = employee?.ManagerId ?? 0;
			var tablesResponse = await _client.GetAsync($"{_config.BaseAddress}Table/GetTablesByManagerId/" + managerId);
			if (!tablesResponse.IsSuccessStatusCode)
			{
				await DisplayAlert("Error", "Failed to fetch tables.", "OK");
				return;
			}

			var tablesData = await tablesResponse.Content.ReadAsStringAsync();
			var tables = JsonConvert.DeserializeObject<List<Table>>(tablesData);

			// Find the table with the lowest ID
			var tableWithLowestId = tables.OrderBy(t => t.TableId).FirstOrDefault();
			if (tableWithLowestId == null)
			{
				await DisplayAlert("Error", "No tables found.", "OK");
				return;
			}

			// Create order details
			var orderDetails = CartItems.Select(cartItem => new OrderDetail
			{
				Quantity = cartItem.Quantity,
				MenuItemId = cartItem.id,
				Status = null,
				Description = cartItem.Attributes
			}).ToList();

			// Create new order
			var newOrder = new Order
			{
				OrderDate = DateTime.Now,
				MemberId = member?.MemberId,
				Status = false,
				Cost = CartItems.Sum(item => item.Price * item.Quantity),
				Tax = 0, // Add tax calculation if needed
				OrderDetails = orderDetails,
				TableId = tableWithLowestId.TableId // Assign the table with the lowest ID
			};

			var orderResponse = await _client.PostAsJsonAsync($"{_config.BaseAddress}Order/", newOrder);
			if (!orderResponse.IsSuccessStatusCode)
			{
				await DisplayAlert("Error", "Failed to create order.", "OK");
				return;
			}

			// Update member points if member exists
			if (member != null)
			{
				var points = newOrder.Cost / 1000;
				var updatePointsResponse = await _client.GetAsync($"{_config.BaseAddress}Member/UpdatePoints/memberId/point?memberId={member.MemberId}&point={points}");
				if (!updatePointsResponse.IsSuccessStatusCode)
				{
					await DisplayAlert("Error", "Failed to update member points.", "OK");
					return;
				}
			}

			ClearCartPreferences();
			await DisplayAlert("Success", "Order created successfully.", "OK");
			SendOrderConfirmationNotificationAsync();


        }
		catch (Exception ex)
		{
			// Handle exceptions
			await DisplayAlert("Error", $"Exception: {ex.Message}", "OK");
		}
	}
    //Send from staff to bartend
    private async Task SendOrderConfirmationNotificationAsync()
    {
        var requestBody = new
        {
            text = "New Order Created Manually by staff !",
            action = "OrderSuccessesSToBartendFinished"
        };

        var json = JsonConvert.SerializeObject(requestBody);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        _client.DefaultRequestHeaders.Clear();
        _client.DefaultRequestHeaders.Add("apikey", "0624d820-6616-430d-92a5-e68265a08593");

        var uri = new Uri("https://push-noti-api-amg8fwasfebchtf2.southeastasia-01.azurewebsites.net/api/notifications/requests");

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

    private void ClearCartPreferences()
	{
		Preferences.Remove("Cart");
		CartItems.Clear();
		CartList.ItemsSource = CartItems;
		UpdateTotalPrice();
	}

	public void RemoveCartItem(CartItem cartItem)
	{
		CartItems.Remove(cartItem);
		SaveCartToPreferences();
	}

	public void SaveCartToPreferences()
	{
		CartList.ItemsSource = CartItems;
		string cartJson = JsonConvert.SerializeObject(CartItems);
		Preferences.Set("Cart", cartJson);
	}

	private void LoadCartFromPreferences()
	{
		string cartJson = Preferences.Get("Cart", "[]");
		var cartItems = JsonConvert.DeserializeObject<ObservableCollection<CartItem>>(cartJson);
		if (cartItems != null)
		{
			CartItems = cartItems;
			CartList.ItemsSource = CartItems;
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

	private void ShowCartPopup(object sender, EventArgs e)
	{
		LoadCartFromPreferences();
		UpdateTotalPrice();
		CartPopup.IsVisible = true;
	}

	private void HideCartPopup(object sender, EventArgs e)
	{
		CartPopup.IsVisible = false;
	}

	private void UpdateTotalPrice()
	{
		var totalPrice = CartItems.Sum(item => (item.Price - item.Discount) * item.Quantity);
		TotalPriceLabel.Text = totalPrice.ToString("C", new System.Globalization.CultureInfo("vi-VN"));
	}

	private void OnBellIconClicked(object sender, EventArgs e)
	{
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
		var viewModel = BindingContext as MenuItemListViewModel;
		viewModel?.LoadMenuItems();
		Application.Current.MainPage.DisplayAlert("Loaded", "Menu items reloaded.", "OK");
	}

	private void OnItemToMakeClicked(object sender, EventArgs e)
	{
		CalculateRemainingDays();
		if (role.Equals("bartender"))
		{
			SwitchToPage("ItemToMakeBartender", () => new ItemToMakeBartender());
		}
		else if (role.Equals("staff"))
		{
			SwitchToPage("ItemsToMake", () => new ItemToMake());
		}
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
			var viewModel = BindingContext as MenuItemListViewModel;
			viewModel?.LoadMenuItems();
		}
	}

	public event PropertyChangedEventHandler PropertyChanged;

	protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
	{
		PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
	}

	private void HandleIsAvailableChanged(MenuItem menuItem, bool isAvailable)
	{
		// Your logic to handle the change in availability
		// For example, you can update the MenuItem or perform other actions
		menuItem.IsAvailable = isAvailable;
		// Additional logic here
	}
}

public class CartItem
{
	public string ItemName { get; set; }
	public int Quantity { get; set; }
	public string Sugar { get; set; } = "normal"; // Default value
	public string Ice { get; set; } = "normal";   // Default value
	public string Topping { get; set; } = string.Empty;
	public string Description { get; set; } = string.Empty;
	public double Price { get; set; }
	public int id { get; set; }
	public double Discount { get; set; }

	public string Attributes => $"{Ice} Ice, {Sugar} Sugar{(string.IsNullOrEmpty(Topping) ? "" : $", {Topping}")}";

	public Command RemoveCommand => new Command(() =>
	{
		var menuItemList = Application.Current.MainPage.Navigation.NavigationStack
			.OfType<MenuItemList>()
			.FirstOrDefault();
		menuItemList?.RemoveCartItem(this);
	});

	// Parse description to fill Sugar, Ice, and Topping
	public void ParseDescription(string description)
	{
		string[] attributes = description.Split(',');

		foreach (var attribute in attributes)
		{
			string trimmed = attribute.Trim(); // Remove whitespace

			if (trimmed.Contains("Ice", StringComparison.OrdinalIgnoreCase))
			{
				Ice = trimmed.Replace("Ice", "", StringComparison.OrdinalIgnoreCase).Trim();
			}
			else if (trimmed.Contains("Sugar", StringComparison.OrdinalIgnoreCase))
			{
				Sugar = trimmed.Replace("Sugar", "", StringComparison.OrdinalIgnoreCase).Trim();
			}
			else
			{
				Topping += (string.IsNullOrEmpty(Topping) ? "" : ", ") + trimmed;
			}
		}
	}
}

public class MenuItemListViewModel : INotifyPropertyChanged
{
	private string _searchText;
	private ItemCategory _selectedCategory;
	public ObservableCollection<MenuItem> AvailableDrinkToppings { get; set; } = new ObservableCollection<MenuItem>();
    public ObservableCollection<MenuItem> AvailableFoodToppings { get; set; } = new ObservableCollection<MenuItem>();
    private readonly Models.ConfigApi _config = new Models.ConfigApi();
	private readonly HttpClient _client = new HttpClient(new HttpClientHandler
	{
		ServerCertificateCustomValidationCallback = (message, cert, chain, sslPolicyErrors) => true
	});
	public ObservableCollection<MenuItem> MenuItems { get; set; }
	public ObservableCollection<ItemCategory> Categories { get; set; }
	public ObservableCollection<MenuItem> FilteredMenuItems { get; set; }
	public ICommand AddToCartCommand { get; set; }
	private CancellationTokenSource _debounceCts;
	public string Role { get; set; }

    public string SearchText
	{
		get => _searchText;
		set
		{
			_searchText = value;
		}
	}

	public ItemCategory SelectedCategory
	{
		get => _selectedCategory;
		set
		{
			_selectedCategory = value;
			OnPropertyChanged();
			DebounceFilterMenuItems();
		}
	}

	public MenuItemListViewModel()
	{
        string loginInfoJson = Preferences.Get("LoginInfo", string.Empty);
        Employee emp = JsonConvert.DeserializeObject<Employee>(loginInfoJson);
        Role = emp.Role.RoleName;
        MenuItems = new ObservableCollection<MenuItem>();
		FilteredMenuItems = new ObservableCollection<MenuItem>();
		Categories = new ObservableCollection<ItemCategory>();
		LoadCategories();
		LoadMenuItems();
	}

	private async void LoadCategories()
	{
		try
		{
			var uri = new Uri(_config.BaseAddress + "ItemCategory?id=1");
			HttpResponseMessage response = await _client.GetAsync(uri);
			if (response.IsSuccessStatusCode)
			{
				string data = await response.Content.ReadAsStringAsync();
				var categories = JsonConvert.DeserializeObject<List<ItemCategory>>(data);
				Categories.Clear();
				Categories.Add(new ItemCategory { ItemCategoryName = "None" });

				if (categories != null)
				{
					foreach (var category in categories)
					{
						if (category != null)
						{
							Categories.Add(category);
						}
					}
				}
				SelectedCategory = Categories.FirstOrDefault(c => c.ItemCategoryName == "None");
			}
		}
		catch (Exception ex)
		{
			Console.WriteLine($"Error fetching categories: {ex.Message}");
		}
	}

	public async void LoadMenuItems()
	{
		try
		{
			var loginInfoJson = Preferences.Get("LoginInfo", string.Empty);
			var employee = JsonConvert.DeserializeObject<Employee>(loginInfoJson);
			var managerId = employee?.ManagerId ?? 0;
			var uri = new Uri(_config.BaseAddress + "MenuItem/GetAllMenuItem?employeeId=" + managerId);
			HttpResponseMessage response = await _client.GetAsync(uri);

			if (response.IsSuccessStatusCode)
			{
				string data = await response.Content.ReadAsStringAsync();
				var menuItems = JsonConvert.DeserializeObject<List<MenuItem>>(data);
				menuItems = menuItems.OrderBy(item => item.ItemName).ToList();

				MenuItems.Clear();
				AvailableDrinkToppings.Clear();
                AvailableFoodToppings.Clear();

                if (menuItems != null)
				{
					foreach (var menuItem in menuItems)
					{
						if (menuItem != null)
						{
							if (!menuItem.MenuCategories.Any(mc => mc.ItemCategory.Description == "TOPPING" || mc.ItemCategory.ItemCategoryName == "L?P PH?"))
							{
								
                                if (menuItem.Description.Contains("food/"))
                                    menuItem.isDrink = false;
                                else
                                    menuItem.isDrink = true;
                                MenuItems.Add(menuItem);
                            }
							else
							{
                                if (menuItem.Description.Contains("food/"))
                                    AvailableFoodToppings.Add(menuItem);
                                else
                                    AvailableDrinkToppings.Add(menuItem);
                            }
						}
					}
				}
				foreach (var menuItem in menuItems)
				{
					if (menuItem != null)
					{
                        if (menuItem.Description.Contains("food/"))
                            menuItem.AvailableFoodToppings = AvailableFoodToppings;
                        else
                            menuItem.AvailableDrinkToppings = AvailableDrinkToppings;
                    }
				}
			}
			FilterMenuItems();
		}
		catch (Exception ex)
		{
			// Handle exceptions
			Console.WriteLine($"Error fetching menu items: {ex.Message}");
		}
	}

	private async void DebounceFilterMenuItems()
	{
		_debounceCts?.Cancel();
		_debounceCts = new CancellationTokenSource();

		try
		{
			await Task.Delay(1500, _debounceCts.Token); // Adjust the delay as needed
			FilterMenuItems();
		}
		catch (TaskCanceledException)
		{

		}
	}

	/*public void FilterMenuItems()
	{
		List<MenuItem> filtered = MenuItems.Where(mi => (string.IsNullOrWhiteSpace(SearchText) || mi.ItemName.ToLower().Contains(SearchText.ToLower())) &&
			(SelectedCategory == null || SelectedCategory.ItemCategoryName == "None" || mi.MenuCategories.Any(mc => mc.ItemCategory.ItemCategoryName == SelectedCategory.ItemCategoryName))).Take(5).ToList();
		FilteredMenuItems.Clear();
		foreach (var item in filtered)
			FilteredMenuItems.Add(item);
		*//*FilteredMenuItems = new ObservableCollection<MenuItem>(MenuItems);*//*

		int a = 1;
	}*/

	public void FilterMenuItems()
	{
		int pageNumber = Preferences.Get("CurrentPage", 1);
		int pageSize = Preferences.Get("ItemsPerPage", 5);

		var filtered = MenuItems
			.Where(mi => (string.IsNullOrWhiteSpace(SearchText) || mi.ItemName.ToLower().Contains(SearchText.ToLower())) &&
						 (SelectedCategory == null || SelectedCategory.ItemCategoryName == "None" || mi.MenuCategories.Any(mc => mc.ItemCategory.ItemCategoryName == SelectedCategory.ItemCategoryName)))
			.Skip((pageNumber - 1) * pageSize)
			.Take(pageSize)
			.ToList();

		FilteredMenuItems.Clear();
		foreach (var item in filtered)
		{
			FilteredMenuItems.Add(item);
		}
	}

	public event PropertyChangedEventHandler PropertyChanged;

	protected void OnPropertyChanged([CallerMemberName] string name = null)
	{
		PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
	}
}



public class MenuItemViewModel : INotifyPropertyChanged
{

	private int _quantity = 1;
	private string _sugar = "normal";
	private string _ice = "normal";

	public int MenuItemId { get; set; }
	public string ItemName { get; set; }
	public double Price { get; set; }
	public string Category { get; set; }

	ConfigApi _config = new ConfigApi();

	private bool _isAvailable;
	public bool IsAvailable
	{ get; set; }

	public event PropertyChangedEventHandler PropertyChanged;

	protected void OnPropertyChanged([CallerMemberName] string name = null)
	{
		PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
	}

	public int Quantity
	{
		get => _quantity;
		set
		{
			_quantity = value;
			OnPropertyChanged();
		}
	}

	public string Sugar
	{
		get => _sugar;
		set
		{
			_sugar = value;
			OnPropertyChanged();
			UpdateDescription();
		}
	}

	public string Ice
	{
		get => _ice;
		set
		{
			_ice = value;
			OnPropertyChanged();
			UpdateDescription();
		}
	}

	public ObservableCollection<ToppingViewModel> AvailableDrinkToppings { get; set; }

	public ICommand AddToCartCommand { get; set; }

	public MenuItemViewModel()
	{
		_sugar = "normal"; // Default value
		_ice = "normal";   // Default value
		AddToCartCommand = new Command<MenuItemViewModel>(AddToCart);
	}

	private readonly HttpClient httpClient = new HttpClient(new HttpClientHandler
	{
		ServerCertificateCustomValidationCallback = (message, cert, chain, sslPolicyErrors) => true
	});

	private void AddToCart(MenuItemViewModel menuItem)
	{
		// The actual implementation is handled in the parent ViewModel
	}

	private void UpdateDescription()
	{
		// Combine Sugar, Ice, and Toppings into Description
		var selectedToppings = AvailableDrinkToppings.Where(t => t.IsSelected).Select(t => t.ItemName);
		Description = $"{Ice} Ice, {Sugar} Sugar, {string.Join(", ", selectedToppings)}";
		OnPropertyChanged(nameof(Description));
	}

	private string _description;
	public string Description
	{
		get => _description;
		set
		{
			_description = value;
			OnPropertyChanged();
		}
	}


}

public class ToppingViewModel : INotifyPropertyChanged
{
	private bool _isSelected = false;
	public string ItemName { get; set; }

	public bool IsSelected
	{
		get => _isSelected;
		set
		{
			_isSelected = value;
			OnPropertyChanged();
		}
	}

	public event PropertyChangedEventHandler PropertyChanged;

	protected void OnPropertyChanged([CallerMemberName] string name = null)
	{
		PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
	}
}

