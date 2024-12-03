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
using System.Text;

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

    


    private async void OnConfirmOrderPaidClicked(object sender, EventArgs e)
    {
        // Get the Order object from the CommandParameter
        var button = sender as Button;
        var order = button?.CommandParameter as Order; // Cast to your Order type

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
                    var viewModel = BindingContext as CombinedViewModel;
                    viewModel?.PendingOrder.LoadOrders();
                    viewModel?.ItemToMake.LoadOrderDetails();
                    DisplayAlert("Confirmed", $"Order: {order.OrderId} has been confirmed paid.", "OK");
                }
            }
            catch (Exception ex)
            {
                // Handle exceptions
                Console.WriteLine($"Error updating order status: {ex.Message}");
            }
        }
    }

    private async void OnCancelOrderClicked(object sender, EventArgs e)
    {
        // Get the Order object from the CommandParameter
        var button = sender as Button;
        var order = button?.CommandParameter as Order; // Cast to your Order type

        if (order != null)
        {
            try
            {
                var uri = new Uri(_config.BaseAddress + $"Order/{order.OrderId}");
                HttpResponseMessage response = await _client.DeleteAsync(uri);

                if (response.IsSuccessStatusCode)
                {
                    // Remove the order from the local collection if needed
                    var viewModel = BindingContext as CombinedViewModel;
                    viewModel?.PendingOrder.Orders.Remove(order);
                    // Refresh the pending order list and item to make list
                    viewModel?.PendingOrder.LoadOrders();
                    viewModel?.ItemToMake.LoadOrderDetails();
                    DisplayAlert("Cancelled", $"Order: {order.OrderId} has been cancelled.", "OK");
                }
            }
            catch (Exception ex)
            {
                // Handle exceptions
                Console.WriteLine($"Error deleting order: {ex.Message}");
            }
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
        var viewModel = BindingContext as CombinedViewModel;
        viewModel?.PendingOrder.LoadOrders();
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

    public async void LoadOrders()
    {
        try
        {
            var uri = new Uri(_config.BaseAddress + "Order/Employee/1");
            HttpResponseMessage response = await _client.GetAsync(uri);
            if (response.IsSuccessStatusCode)
            {
                string data = await response.Content.ReadAsStringAsync();
                var orders = JsonConvert.DeserializeObject<List<Order>>(data);
                orders = orders.Where(o => o.Status == null).ToList();
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