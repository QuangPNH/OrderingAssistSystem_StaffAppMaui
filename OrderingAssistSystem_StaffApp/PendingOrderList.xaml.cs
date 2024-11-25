using OrderingAssistSystem_StaffApp.Models;
using System.Collections.ObjectModel;
using System.Globalization;
using Microsoft.Maui.Controls;
using MenuItem = OrderingAssistSystem_StaffApp.Models.MenuItem;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using CommunityToolkit.Maui.Views;
using Newtonsoft.Json;
using Twilio.TwiML.Voice;
using Config = OrderingAssistSystem_StaffApp.Models.Config;
using System.Text.Json;

namespace OrderingAssistSystem_StaffApp;

public partial class PendingOrderList : ContentPage
{
    private ObservableCollection<Models.Notification> Notifications;

    Config config = new Config();
    public PendingOrderList()
    {
        InitializeComponent();
        BindingContext = new CombinedViewModel();

        // Mock Notifications
        Notifications = new ObservableCollection<Models.Notification>
            {
                new Models.Notification { Title = "Order", Content = "Order #1234 is ready." },
                new Models.Notification { Title = "Reminder", Content = "Restock ingredients soon." }
            };
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
        Microsoft.Maui.Controls.Application.Current.MainPage = new NavigationPage(page);
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
        Microsoft.Maui.Controls.Application.Current.MainPage = new NavigationPage(new MainPage());
        await System.Threading.Tasks.Task.CompletedTask; // Ensure the method is still async.
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
    public ObservableCollection<Order> Orders { get; set; }
    Config config = new Config();
    HttpClient _client = new HttpClient(new HttpClientHandler
    {
        ServerCertificateCustomValidationCallback = (message, cert, chain, sslPolicyErrors) => true
    });

    JsonSerializerOptions _serializerOptions = new JsonSerializerOptions
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true
    };

    /*public PendingOrderViewModel()
    {
        try
        {
            var uri = new Uri($"{config.BaseAddress}Employee/");
            HttpResponseMessage response = await _client.GetAsync(uri);

            if (response.IsSuccessStatusCode)
            {
                string data = await response.Content.ReadAsStringAsync();
                emp = JsonConvert.DeserializeObject<Employee>(data);
                if (emp.Phone != null)
                {
                    if (emp.Role.RoleName.ToLower() == "staff")
                    {
                        return "staff";
                    }
                    else if (emp.Role.RoleName.ToLower() == "bartender")
                    {
                        return "bartender";
                    }
                }
                if (emp.Owner.SubscribeEndDate < DateTime.Now.AddDays(7))
                {
                    return "employee expired";
                }
            }
            else { return "null"; }
        }
        catch (Exception ex)
        {

        }

        Orders = new ObservableCollection<Order>
		{
			new Order
			{
				OrderId = 1,
				OrderDate = DateTime.Now,
				Cost = 50.75,
				Status = false,
				Table = new Table { Qr = "TableQR123" },
				Member = new Member { MemberName = "John Doe", Phone = "123456789" },
				OrderDetails = new ObservableCollection<OrderDetail>
				{
					new OrderDetail { OrderDetailId = 1, MenuItem = new MenuItem { ItemName = "Pizza" }, Quantity = 2, Status = true, Description = "normal Ice, normal Sugar, Hạt Bí, Hướng Duong" },
					new OrderDetail { OrderDetailId = 2, MenuItem = new MenuItem { ItemName = "Pasta" }, Quantity = 1, Status = false, Description = "normal Ice, normal Sugar, Hạt Bí, Hướng Duong" },
				}
			},
			new Order
			{
				OrderId = 2,
				OrderDate = DateTime.Now.AddHours(-2),
				Cost = 30.25,
				Status = true,
				Table = new Table { Qr = "TableQR456" },
				Member = new Member { MemberName = "Jane Smith", Phone = "987654321" },
				OrderDetails = new ObservableCollection<OrderDetail>
				{
					new OrderDetail { OrderDetailId = 3, MenuItem = new MenuItem { ItemName = "Burger" }, Quantity = 3, Status = true, Description = "normal Ice, normal Sugar, Hạt Bí, Hướng Duong" },
					new OrderDetail { OrderDetailId = 4, MenuItem = new MenuItem { ItemName = "Fries" }, Quantity = 2, Status = true, Description = "normal Ice, normal Sugar, Hạt Bí, Hướng Duong" },
				}
			}
		};

        string[] attributes;
        foreach (Order order in Orders)
        {
            foreach (OrderDetail orderDetail in order.OrderDetails)
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
        }
    }*/

    public async void Yeh()
    {
        try
        {
            var uri = new Uri($"{config.BaseAddress}Order/");
            HttpResponseMessage response = await _client.GetAsync(uri);

            if (response.IsSuccessStatusCode)
            {
                string data = await response.Content.ReadAsStringAsync();
                var orders = JsonConvert.DeserializeObject<List<Order>>(data);
                Orders = orders != null ? new ObservableCollection<Order>(orders) : new ObservableCollection<Order>();
            }
        }
        catch (Exception ex)
        {

        }

        string[] attributes;
        foreach (Order order in Orders)
        {
            foreach (OrderDetail orderDetail in order.OrderDetails)
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
        }
    }
    public PendingOrderViewModel()
    {
        Yeh();
    }


}


public class ItemToMakeListViewModel : INotifyPropertyChanged
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