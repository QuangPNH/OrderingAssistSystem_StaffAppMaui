using OrderingAssistSystem_StaffApp.Models;
using System.Collections.ObjectModel;
using System.Globalization;
using Microsoft.Maui.Controls;
using MenuItem = OrderingAssistSystem_StaffApp.Models.MenuItem;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using CommunityToolkit.Maui.Views;
using Newtonsoft.Json;

using Config = OrderingAssistSystem_StaffApp.Models.Config;
using System.Text.Json;
using Twilio.TwiML.Voice;
using Application = Microsoft.Maui.Controls.Application;
using Task = System.Threading.Tasks.Task;
using System.Windows.Input;

namespace OrderingAssistSystem_StaffApp;

public partial class PendingOrderList : ContentPage
{
    private ObservableCollection<Models.Notification> Notifications { get; set; } = new ObservableCollection<Models.Notification>();
    private readonly HttpClient _client = new HttpClient(new HttpClientHandler
    {
        ServerCertificateCustomValidationCallback = (message, cert, chain, sslPolicyErrors) => true
    });
    Config _config = new Config();
    public PendingOrderList()
    {
        InitializeComponent();
        BindingContext = new CombinedViewModel();
        LoadNotifications();
    }

    private void OnConfirmOrderPaidClicked(object sender, EventArgs e)
    {
        // Get the Order object from the CommandParameter
        var button = sender as Button;
        var order = button?.CommandParameter as Order; // Cast to your Order type

        if (order != null)
        {
            
        }
    }

    private void OnCancelOrderClicked(object sender, EventArgs e)
    {
        // Get the Order object from the CommandParameter
        var button = sender as Button;
        var order = button?.CommandParameter as Order; // Cast to your Order type

        if (order != null)
        {
            // Handle the logic for canceling the order
            // For example, cancel order, update status, etc.
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
}

public class CombinedViewModel
{
    public PendingOrderViewModel PendingOrder { get; set; }
    public ItemToMakeListViewModel ItemToMake { get; set; }

    public CombinedViewModel()
    {
        PendingOrder = new PendingOrderViewModel();
        ItemToMake = new ItemToMakeListViewModel();
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
    private readonly Config _config = new Config();
    public ObservableCollection<Order> Orders { get; set; } = new ObservableCollection<Order>();

    public PendingOrderViewModel()
    {
        LoadOrders();
    }

    private async void LoadOrders()
    {
        try
        {
            var uri = new Uri(_config.BaseAddress + "Order/");
            HttpResponseMessage response = await _client.GetAsync(uri);
            if (response.IsSuccessStatusCode)
            {
                string data = await response.Content.ReadAsStringAsync();
                var orders = JsonConvert.DeserializeObject<List<Order>>(data);

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


/*public class ItemToMakeListViewModel : INotifyPropertyChanged
{
    private ObservableCollection<GroupedMenuItem> _groupedMenuItems;
    public ObservableCollection<GroupedMenuItem> GroupedMenuItems
    {
        get => _groupedMenuItems;
        set
        {
            _groupedMenuItems = value;
            OnPropertyChanged();
        }
    }

    public class OrderDetail
    {
        public int OrderDetailId { get; set; }
        public string ItemName { get; set; }
        public int Quantity { get; set; }
        public bool? Status { get; set; } // null = not processed, false = processing, true = done
        public string Description { get; set; }
        public string Sugar { get; set; }
        public string Ice { get; set; }
        public string Topping { get; set; }
        public MenuItem MenuItem { get; set; }
    }

    public class MenuItem
    {
        public string ItemName { get; set; }
    }

    public class GroupedMenuItem
    {
        public string MenuItemName { get; set; }
        public List<OrderDetail> PendingItems { get; set; }
        public List<OrderDetail> ProcessingItems { get; set; }
        public List<OrderDetail> DoneItems { get; set; }
    }

    public ItemToMakeListViewModel()
    {
        var orderDetails = GetSampleOrderDetails();

        string[] attributes;
        foreach (OrderDetail orderDetail in orderDetails)
        {
            attributes = orderDetail.Description.Split(',');

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
                    orderDetail.Topping += (orderDetail.Topping == "" ? "" : ", ") + trimmed;
                }
            }
            orderDetail.Topping = orderDetail.Topping.Length > 2 ? orderDetail.Topping.Substring(2) : string.Empty;
        }

        // Group order details by MenuItem and then by status
        GroupedMenuItems = new ObservableCollection<GroupedMenuItem>(
            orderDetails.GroupBy(o => o.MenuItem.ItemName)
                .Select(g => new GroupedMenuItem
                {
                    MenuItemName = g.Key,
                    PendingItems = g.Where(o => o.Status == null).ToList(),
                    ProcessingItems = g.Where(o => o.Status == false).ToList(),
                    DoneItems = g.Where(o => o.Status == true).ToList()
                })
        );
    }

    // Sample method to generate some mock data
    private List<OrderDetail> GetSampleOrderDetails()
    {

        return new List<OrderDetail>
        {
            new OrderDetail { OrderDetailId = 1, ItemName = "Pizza", Quantity = 2, Status = null, MenuItem = new MenuItem { ItemName = "Pizza" }, Description = "normal Ice, normal Sugar, Hạt Bí, Hướng Duong" },
            new OrderDetail { OrderDetailId = 2, ItemName = "Pizza", Quantity = 1, Status = false, MenuItem = new MenuItem { ItemName = "Pizza" }, Description = "normal Ice, normal Sugar, Hạt Bí, Hướng Duong" },
            new OrderDetail { OrderDetailId = 3, ItemName = "Pasta", Quantity = 3, Status = true, MenuItem = new MenuItem { ItemName = "Pasta" }, Description = "normal Ice, normal Sugar, Hạt Bí, Hướng Duong" },
            new OrderDetail { OrderDetailId = 4, ItemName = "Pasta", Quantity = 1, Status = null, MenuItem = new MenuItem { ItemName = "Pasta" }, Description = "normal Ice, normal Sugar, Hạt Bí, Hướng Duong" },
        };
    }

    public event PropertyChangedEventHandler PropertyChanged;

    protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}*/


public class ItemToMakeListViewModel : INotifyPropertyChanged
{
    private readonly HttpClient _client = new HttpClient(new HttpClientHandler
    {
        ServerCertificateCustomValidationCallback = (message, cert, chain, sslPolicyErrors) => true
    });
    private readonly Config _config = new Config();
    private ObservableCollection<GroupedMenuItem> _groupedMenuItems;

    public ObservableCollection<GroupedMenuItem> GroupedMenuItems
    {
        get => _groupedMenuItems;
        set
        {
            _groupedMenuItems = value;
            OnPropertyChanged();
        }
    }

    public ItemToMakeListViewModel()
    {
        LoadOrderDetails();
    }

    private async void LoadOrderDetails()
    {
        try
        {
            var uri = new Uri(_config.BaseAddress + "OrderDetail/");
            HttpResponseMessage response = await _client.GetAsync(uri);

            if (response.IsSuccessStatusCode)
            {
                string data = await response.Content.ReadAsStringAsync();
                var orderDetails = JsonConvert.DeserializeObject<List<OrderDetail>>(data);

                if (orderDetails != null)
                {
                    foreach (var orderDetail in orderDetails)
                    {
                        ParseOrderDetails(orderDetail);
                    }
                    GroupedMenuItems = new ObservableCollection<GroupedMenuItem>(
                        orderDetails.GroupBy(o => o.MenuItem.ItemName)
                            .Select(g => new GroupedMenuItem
                            {
                                MenuItemName = g.Key,
                                PendingItems = g.Where(o => o.Status == null).ToList(),
                                ProcessingItems = g.Where(o => o.Status == false).ToList(),
                                DoneItems = g.Where(o => o.Status == true).ToList()
                            })
                    );
                }
            }
        }
        catch (Exception ex)
        {
            // Handle exceptions
            Console.WriteLine($"Error fetching order details: {ex.Message}");
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

    public event PropertyChangedEventHandler PropertyChanged;

    protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    public class GroupedMenuItem
    {
        public string MenuItemName { get; set; }
        public List<OrderDetail> PendingItems { get; set; }
        public List<OrderDetail> ProcessingItems { get; set; }
        public List<OrderDetail> DoneItems { get; set; }
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