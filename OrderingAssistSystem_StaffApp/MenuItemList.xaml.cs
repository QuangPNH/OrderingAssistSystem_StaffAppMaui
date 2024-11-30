using CommunityToolkit.Maui.Views;
using Newtonsoft.Json;
using OrderingAssistSystem_StaffApp.Models;
using System.Collections.ObjectModel;
using System.ComponentModel;
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
    public ObservableCollection<CartItem> CartItems { get; set; }
    public MenuItemList()
    {
        InitializeComponent();
        LoadNotifications();
        SaveCartToPreferences();
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
        CartPopup.IsVisible = true;
    }

    private void HideCartPopup(object sender, EventArgs e)
    {
        CartPopup.IsVisible = false;
    }

    private void UpdateTotalPrice()
    {
        var totalPrice = CartItems.Sum(item => (item.Price - item.Discount) * item.Quantity);
        TotalPriceLabel.Text = totalPrice.ToString("C");
    }

    // You can bind this to your command in a real application
    private void CreateOrder()
    {
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
        SwitchToPage("MenuItems", () => new MenuItemList());
    }

    private void OnItemToMakeClicked(object sender, EventArgs e)
    {
        SwitchToPage("ItemsToMake", () => new ItemToMake());
    }

    private async void OnLogOutClicked(object sender, EventArgs e)
    {
        Preferences.Remove("LoginInfo");
        // Reset the MainPage to the login page
        Application.Current.MainPage = new NavigationPage(new MainPage());
        await Task.CompletedTask; // Ensure the method is still async.
    }
    private void SaveCartToPreferences()
    {
        CartItems = new ObservableCollection<CartItem>
        {
            new CartItem { ItemName = "Item1", Quantity = 1, Price = 5.0 },
            new CartItem { ItemName = "Item2", Quantity = 1, Price = 5.0 }
        };

        CartItems[0].ParseDescription("less Ice, normal Sugar, Hạt Bí");
        CartItems[1].ParseDescription("normal Ice, less Sugar, Hướng Duong");

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
}

public class CartItem
{
    public string ItemName { get; set; }
    public int Quantity { get; set; }
    public string Sugar { get; set; } = "Normal"; // Default value
    public string Ice { get; set; } = "Normal";   // Default value
    public string Topping { get; set; } = string.Empty;
    public double Price { get; set; }
    public double Discount { get; set; }

    public string Attributes => $"{Ice} Ice, {Sugar} Sugar{(string.IsNullOrEmpty(Topping) ? "" : $", {Topping}")}";

    public Command RemoveCommand => new Command(() => App.Current.MainPage.DisplayAlert("Remove", "Removing Item!", "OK"));

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
    private readonly Models.Config _config = new Models.Config();
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
        AddToCartCommand = new Command<MenuItemViewModel>(AddToCart);
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

    private async void LoadMenuItems()
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

    private void AddToCart(MenuItemViewModel menuItem)
    {
        if (!menuItem.IsAvailable)
        {
            // Optionally notify user that item is disabled
            return;
        }

        if (menuItem.Quantity <= 0)
        {
            // Optionally notify user to enter a valid quantity
            return;
        }

        // Create OrderDetail
        var orderDetail = new OrderDetail
        {
            MenuItemId = menuItem.MenuItemId,
            Quantity = menuItem.Quantity,
            Sugar = menuItem.Sugar,
            Ice = menuItem.Ice,
            Topping = string.Join(", ", menuItem.AvailableToppings.Where(t => t.IsSelected).Select(t => t.ItemName)),
            Description = $"{menuItem.Ice ?? "Normal"} Ice, {menuItem.Sugar ?? "Normal"} Sugar, {string.Join(", ", menuItem.AvailableToppings.Where(t => t.IsSelected).Select(t => t.ItemName))}",
            Status = null // Not processed yet
        };

        // Add to cart using Preferences
        // Serialize the OrderDetail and store it in Preferences
        string cartJson = Preferences.Get("Cart", "[]");
        var cart = JsonConvert.DeserializeObject<ObservableCollection<OrderDetail>>(cartJson) ?? new ObservableCollection<OrderDetail>();
        cart.Add(orderDetail);
        Preferences.Set("Cart", JsonConvert.SerializeObject(cart));

        // Optionally notify user that item was added
        Application.Current.MainPage.DisplayAlert("Success", $"{orderDetail.MenuItem.ItemName} added to cart.", "OK");
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

public class ItemCategory
{
    public int ItemCategoryId { get; set; }
    public string ItemCategoryName { get; set; }
    public string Description { get; set; }
    public double Discount { get; set; }
    public string Image { get; set; }
    public bool IsDelete { get; set; }
}
