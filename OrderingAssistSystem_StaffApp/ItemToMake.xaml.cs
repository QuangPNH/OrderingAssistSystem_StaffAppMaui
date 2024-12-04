using CommunityToolkit.Maui.Views;
using Newtonsoft.Json;
using OrderingAssistSystem_StaffApp.Models;
using OrderingAssistSystem_StaffApp.Services;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Text;
using MenuItem = OrderingAssistSystem_StaffApp.Models.MenuItem;

namespace OrderingAssistSystem_StaffApp;

public partial class ItemToMake : ContentPage
{
    private ObservableCollection<Models.Notification> Notifications { get; set; } = new ObservableCollection<Models.Notification>();
    private readonly HttpClient _client = new HttpClient(new HttpClientHandler
    {
        ServerCertificateCustomValidationCallback = (message, cert, chain, sslPolicyErrors) => true
    });
    Models.ConfigApi _config = new Models.ConfigApi();
    public ItemToMake()
    {
        InitializeComponent();
        BindingContext = new ItemToMakeListViewModel();
        // Mock Notifications
        LoadNotifications();
        Authoriz();
    }

    public async Task Authoriz()
    {
        //DisplayAlert("Status", Preferences.Get("LoginInfo", string.Empty), "OK");

        AuthorizeLogin authorizeLogin = new AuthorizeLogin(_client);

        var loginStatus = await authorizeLogin.CheckLogin();
        if (loginStatus.Equals("staff") || loginStatus.Equals("bartender"))
        {
        }
        else if (loginStatus.Equals("employee expired"))
        {
            LogOut();
            await DisplayAlert("Status", "The owner's subscription has been over for over a week. Contact for more info.", "OK");
        }
        else if (loginStatus.Equals("null"))
        {
            await DisplayAlert("Status", "Staff not found", "OK");
        }
        else
        {
            await DisplayAlert("Status", "Nothing much really", "OK");
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
            // Update the status of the order detail
            orderDetail.Status = false;

            var uri = new Uri(_config.BaseAddress + $"OrderDetail/{orderDetail.OrderDetailId}");
            var content = new StringContent(JsonConvert.SerializeObject(orderDetail), Encoding.UTF8, "application/json");
            HttpResponseMessage response = await _client.PutAsync(uri, content);

            uri = new Uri(_config.BaseAddress + $"Order/{orderDetail.Order?.OrderId}");
            response = await _client.GetAsync(uri);
            if (response.IsSuccessStatusCode)
            {
                string data = await response.Content.ReadAsStringAsync();
                var order = JsonConvert.DeserializeObject<Order>(data);
                var orderDetails = order?.OrderDetails;
                if (orderDetails != null && orderDetails.All(od => od.Status == false))
                {
                    // Update the status of the order
                    orderDetail.Order.Status = false;
                    uri = new Uri(_config.BaseAddress + $"Order/{orderDetail.Order.OrderId}");
                    content = new StringContent(JsonConvert.SerializeObject(order), Encoding.UTF8, "application/json");
                    response = await _client.PutAsync(uri, content);
                }
            }

            // Handle the PendingItem object here
            await DisplayAlert("Item Started", $"Item: {orderDetail.MenuItem?.ItemName} has been started.", "OK");

            // Reload the to-make list
            var viewModel = BindingContext as ItemToMakeListViewModel;
            viewModel?.LoadOrderDetails();
        }
    }

    private async void OnFinishItemClicked(object sender, EventArgs e)
    {
        var button = sender as Button;
        var orderDetail = button?.CommandParameter as OrderDetail; // Cast to your Order type
        if (orderDetail != null)
        {
            // Update the status of the order detail
            orderDetail.Status = true;

            var uri = new Uri(_config.BaseAddress + $"OrderDetail/{orderDetail.OrderDetailId}");
            var content = new StringContent(JsonConvert.SerializeObject(orderDetail), Encoding.UTF8, "application/json");
            HttpResponseMessage response = await _client.PutAsync(uri, content);

            // Check if all order details have status = true
            uri = new Uri(_config.BaseAddress + $"Order/{orderDetail.Order?.OrderId}");
            response = await _client.GetAsync(uri);
            if (response.IsSuccessStatusCode)
            {
                string data = await response.Content.ReadAsStringAsync();
                var order = JsonConvert.DeserializeObject<Order>(data);
                order.OrderDate = DateTime.Now;
                var orderDetails = order?.OrderDetails;
                if (orderDetails != null && orderDetails.All(od => od.Status == true))
                {
                    // Update the status of the order
                    orderDetail.Order.Status = true;
                    uri = new Uri(_config.BaseAddress + $"Order/{orderDetail.Order.OrderId}");
                    content = new StringContent(JsonConvert.SerializeObject(order), Encoding.UTF8, "application/json");
                    response = await _client.PutAsync(uri, content);
                }
            }
            // Handle the ProcessingItem object here
            await DisplayAlert("Item Finished", $"Item: {orderDetail.MenuItem?.ItemName} has been finished.", "OK");

            // Reload the to-make list
            var viewModel = BindingContext as ItemToMakeListViewModel;
            viewModel?.LoadOrderDetails();
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
        SwitchToPage("PendingOrders", () => new PendingOrderList());
    }

    private void OnMenuItemsClicked(object sender, EventArgs e)
    {
        SwitchToPage("MenuItems", () => new MenuItemList());
    }

    private void OnItemToMakeClicked(object sender, EventArgs e)
    {
        var viewModel = BindingContext as ItemToMakeListViewModel;
        viewModel?.LoadOrderDetails();
    }

    private async void OnLogOutClicked(object sender, EventArgs e)
    {
        LogOut();
    }
}



public class ItemToMakeListViewModel : INotifyPropertyChanged
{
    private readonly HttpClient _client = new HttpClient(new HttpClientHandler
    {
        ServerCertificateCustomValidationCallback = (message, cert, chain, sslPolicyErrors) => true
    });
    private readonly ConfigApi _config = new ConfigApi();

    public ObservableCollection<GroupedMenuItem> GroupedMenuItems { get; set; } = new ObservableCollection<GroupedMenuItem>();
    public ItemToMakeListViewModel()
    {
        LoadOrderDetails();
    }

    public async void LoadOrderDetails()
    {
        try
        {
            var loginInfoJson = Preferences.Get("LoginInfo", string.Empty);
            var employee = JsonConvert.DeserializeObject<Employee>(loginInfoJson);
            var managerId = employee?.ManagerId ?? 0;
            var uri = new Uri(_config.BaseAddress + "OrderDetail/Employee/" + managerId);
            HttpResponseMessage response = await _client.GetAsync(uri);

            if (response.IsSuccessStatusCode)
            {
                string data = await response.Content.ReadAsStringAsync();
                var orderDetails = JsonConvert.DeserializeObject<List<OrderDetail>>(data);

                if (orderDetails != null)
                {
                    foreach (var orderDetail in orderDetails.Where(od => od.Order?.Status != null))
                    {
                        ParseOrderDetails(orderDetail);
                    }
                    GroupedMenuItems.Clear();
                    foreach (var groupedItem in orderDetails.Where(od => od.Order?.Status != null).GroupBy(o => o.MenuItem?.ItemName)
                        .Select(g => new GroupedMenuItem
                        {
                            MenuItemName = g.Key ?? string.Empty,
                            PendingItems = g.Where(o => o.Status == null).ToList(),
                            ProcessingItems = g.Where(o => o.Status == false).ToList(),
                            DoneItems = g.Where(o => o.Status == true && o.Order?.OrderDate?.Date == DateTime.Today).ToList()
                        }))
                    {
                        GroupedMenuItems.Add(groupedItem);
                    }
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
        orderDetail.Ice = string.IsNullOrEmpty(orderDetail.Ice) ? "none" : orderDetail.Ice;
        orderDetail.Sugar = string.IsNullOrEmpty(orderDetail.Sugar) ? "none" : orderDetail.Sugar;
        orderDetail.Topping = string.IsNullOrEmpty(orderDetail.Topping) ? "none" : orderDetail.Topping;
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = "")
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    public class GroupedMenuItem
    {
        public string MenuItemName { get; set; } = string.Empty;
        public List<OrderDetail> PendingItems { get; set; } = new List<OrderDetail>();
        public List<OrderDetail> ProcessingItems { get; set; } = new List<OrderDetail>();
        public List<OrderDetail> DoneItems { get; set; } = new List<OrderDetail>();
    }
}
