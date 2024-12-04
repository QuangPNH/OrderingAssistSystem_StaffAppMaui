using CommunityToolkit.Maui.Views;
using Newtonsoft.Json;
using OrderingAssistSystem_StaffApp.Models;
using OrderingAssistSystem_StaffApp.Services;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Net.Http.Json;
using System.Runtime.CompilerServices;
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
    Models.ConfigApi _config = new Models.ConfigApi();
    public ObservableCollection<CartItem> CartItems { get; set; } = new ObservableCollection<CartItem>();
    public MenuItemList()
    {
        InitializeComponent();
        LoadNotifications();
    }

    private void Button_Clicked(object sender, EventArgs e)
    {
        if (sender is Button button && button.BindingContext is MenuItem menuItem)
        {
            // Get selected sugar and ice levels
            string sugar = string.IsNullOrEmpty(menuItem.Sugar?.ToLower()) ? "normal Sugar" : $"{menuItem.Sugar} Sugar";
            string ice = string.IsNullOrEmpty(menuItem.Ice?.ToLower()) ? "normal Ice" : $"{menuItem.Ice} Ice";

            // Get selected toppings
            var selectedToppings = menuItem.AvailableToppings
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
                // If it exists, increase the quantity
                existingCartItem.Quantity += 1;
            }
            else
            {
                // If it doesn't exist, add a new item to the cart
                CartItems.Add(new CartItem
                {id = menuItem.MenuItemId,
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
            try
            {
                // Get member by phone number
                var memberResponse = await _client.GetAsync($"{_config.BaseAddress}Member/Phone/{PhoneNumberEntry.Text.Trim()}");
                if (!memberResponse.IsSuccessStatusCode)
                {
                    // Register new member
                    var newMember = new Member { Phone = PhoneNumberEntry.Text.Trim(), IsDelete = false, Gmail = "" };
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
            // Fetch tables by manager ID
            var tablesResponse = await _client.GetAsync($"{_config.BaseAddress}Table/GetTablesByManagerId/1");
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
        }
        catch (Exception ex)
        {
            // Handle exceptions
            await DisplayAlert("Error", $"An error occurred: {ex.Message}", "OK");
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
            var uri = new Uri(_config.BaseAddress + "Notification/Employee/1");
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
        SwitchToPage("PendingOrders", () => new PendingOrderList());
    }

    private void OnMenuItemsClicked(object sender, EventArgs e)
    {
        var viewModel = BindingContext as MenuItemListViewModel;
        viewModel?.LoadMenuItems();
    }

    private void OnItemToMakeClicked(object sender, EventArgs e)
    {
        SwitchToPage("ItemsToMake", () => new ItemToMake());
    }

    private async void OnLogOutClicked(object sender, EventArgs e)
    {
        Preferences.Remove("LoginInfo");
        INotificationRegistrationService notificationRegistrationService = DependencyService.Get<INotificationRegistrationService>();
        // Reset the MainPage to the login page
        Application.Current.MainPage = new NavigationPage(new MainPage(notificationRegistrationService));
        await Task.CompletedTask; // Ensure the method is still async.
    }
}

public class CartItem
{
    public string ItemName { get; set; }
    public int Quantity { get; set; }
    public string Sugar { get; set; } = "Normal"; // Default value
    public string Ice { get; set; } = "Normal";   // Default value
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
    public ObservableCollection<MenuItem> AvailableToppings { get; set; } = new ObservableCollection<MenuItem>();
    private readonly Models.ConfigApi _config = new Models.ConfigApi();
    private readonly HttpClient _client = new HttpClient(new HttpClientHandler
    {
        ServerCertificateCustomValidationCallback = (message, cert, chain, sslPolicyErrors) => true
    });
    public ObservableCollection<MenuItem> MenuItems { get; set; }
    public ObservableCollection<ItemCategory> Categories { get; set; }
    public ObservableCollection<MenuItem> FilteredMenuItems { get; set; }
    public ICommand AddToCartCommand { get; set; }

    public string SearchText
    {
        get => _searchText;
        set
        {
            _searchText = value;
            OnPropertyChanged();
            FilterMenuItems();
        }
    }

    public ItemCategory SelectedCategory
    {
        get => _selectedCategory;
        set
        {
            _selectedCategory = value;
            OnPropertyChanged();
            FilterMenuItems();
        }
    }

    public MenuItemListViewModel()
    {
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
            var uri = new Uri(_config.BaseAddress + "MenuItem/GetAllMenuItem?employeeId=1");
            HttpResponseMessage response = await _client.GetAsync(uri);

            if (response.IsSuccessStatusCode)
            {
                string data = await response.Content.ReadAsStringAsync();
                var menuItems = JsonConvert.DeserializeObject<List<MenuItem>>(data);

                MenuItems.Clear();
                AvailableToppings.Clear();

                if (menuItems != null)
                {
                    foreach (var menuItem in menuItems)
                    {
                        if (menuItem != null)
                        {
                            if (!menuItem.MenuCategories.Any(mc => mc.ItemCategory.Description == "TOPPING" || mc.ItemCategory.ItemCategoryName == "L?P PH?"))
                            {
                                MenuItems.Add(menuItem);
                            }
                            else
                            {
                                AvailableToppings.Add(menuItem);
                            }
                        }
                    }
                }

                foreach (var menuItem in menuItems)
                {
                    if (menuItem != null)
                    {
                        menuItem.AvailableToppings = AvailableToppings;
                    }
                }


                FilterMenuItems();
            }
        }
        catch (Exception ex)
        {
            // Handle exceptions
            Console.WriteLine($"Error fetching menu items: {ex.Message}");
        }
    }

    private void FilterMenuItems()
    {
        var filtered = MenuItems.Where(mi => (string.IsNullOrWhiteSpace(SearchText) || mi.ItemName.ToLower().Contains(SearchText.ToLower())) &&
            (SelectedCategory == null || SelectedCategory.ItemCategoryName == "None" || mi.MenuCategories.Any(mc => mc.ItemCategory.ItemCategoryName == SelectedCategory.ItemCategoryName)));
        FilteredMenuItems.Clear();
        foreach (var item in filtered)
            FilteredMenuItems.Add(item);
    }

    public event PropertyChangedEventHandler PropertyChanged;

    protected void OnPropertyChanged([CallerMemberName] string name = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}



public class MenuItemViewModel : INotifyPropertyChanged
{
    private bool _isAvailable = true;
    private int _quantity = 1;
    private string _sugar = "Normal";
    private string _ice = "Normal";

    public int MenuItemId { get; set; }
    public string ItemName { get; set; }
    public double Price { get; set; }
    public string Category { get; set; }


    public bool IsAvailable
    {
        get => _isAvailable;
        set
        {
            _isAvailable = value;
            OnPropertyChanged();
        }
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

    public ObservableCollection<ToppingViewModel> AvailableToppings { get; set; }

    public ICommand AddToCartCommand { get; set; }

    public MenuItemViewModel()
    {
        _sugar = "Normal"; // Default value
        _ice = "Normal";   // Default value
        AddToCartCommand = new Command<MenuItemViewModel>(AddToCart);
    }

    private void AddToCart(MenuItemViewModel menuItem)
    {
        // The actual implementation is handled in the parent ViewModel
    }

    private void UpdateDescription()
    {
        // Combine Sugar, Ice, and Toppings into Description
        var selectedToppings = AvailableToppings.Where(t => t.IsSelected).Select(t => t.ItemName);
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

    public event PropertyChangedEventHandler PropertyChanged;

    protected void OnPropertyChanged([CallerMemberName] string name = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
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

