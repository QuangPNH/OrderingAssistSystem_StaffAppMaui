using AzzanOrder.Data.Models;
using CommunityToolkit.Maui.Views;
using Microsoft.IdentityModel.Tokens;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using OrderingAssistSystem_StaffApp.Models;
using OrderingAssistSystem_StaffApp.Services;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Text;

namespace OrderingAssistSystem_StaffApp;

public partial class ItemToMake : ContentPage
{
    private ObservableCollection<Models.Notification> Notifications { get; set; } = new ObservableCollection<Models.Notification>();
    private readonly HttpClient _client = new HttpClient(new HttpClientHandler
    {
        ServerCertificateCustomValidationCallback = (message, cert, chain, sslPolicyErrors) => true
    });
    Models.ConfigApi _config = new Models.ConfigApi();
    string role;
    public ItemToMake()
    {
        Authoriz();
        InitializeComponent();
        BindingContext = new ItemToMakeListViewModel();
        // Mock Notifications
        LoadNotifications();

        CalculateRemainingDays();
        //ClearOrderDetailsPreference();
    }

    private void OnDecrementClicked(object sender, EventArgs e)
    {
        var button = sender as Button;
        if (button?.Parent is HorizontalStackLayout stackLayout)
        {
            var entry = stackLayout.Children.OfType<Entry>().FirstOrDefault();
            if (entry != null && int.TryParse(entry.Text, out int value))
            {
                entry.Text = (value > 0 ? value - 1 : 0).ToString();
            }
            else
            {
                entry.Text = "0";
            }
        }
    }

    private void OnIncrementClicked(object sender, EventArgs e)
    {
        var button = sender as Button;
        if (button?.Parent is HorizontalStackLayout stackLayout)
        {
            var entry = stackLayout.Children.OfType<Entry>().FirstOrDefault();
            if (entry != null && int.TryParse(entry.Text, out int value))
            {
                entry.Text = (value + 1).ToString();
            }
            else
            {
                entry.Text = "1";
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
            await DisplayAlert("Status", "Login info not found.", "OK");
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
        // Reset the MainPage to the login page
        Application.Current.MainPage = new NavigationPage(new MainPage(notificationRegistrationService));
        await Task.CompletedTask; // Ensure the method is still async.
    }

    private async void OnFinishItemClicked(object sender, EventArgs e)
    {
        var button = sender as Button;
        var orderDetail = button?.CommandParameter as OrderDetail; // Cast to your Order type
        if (orderDetail != null)
        {
            // Get the order details with the same order date, item name, sugar, ice, and topping
            var viewModel = BindingContext as ItemToMakeListViewModel;

            viewModel.LoadOrderDetails();

            var matchingOrderDetails = viewModel?.AllOrderDetails
                .Where(od => od.Order?.OrderDate == orderDetail.Order?.OrderDate &&
                             od.MenuItem?.ItemName == orderDetail.MenuItem?.ItemName &&
                             od.Sugar == orderDetail.Sugar &&
                             od.Ice == orderDetail.Ice &&
                             od.Topping == orderDetail.Topping &&
                             (bool)od.Status == false).ToList();

            if (matchingOrderDetails.Count == 0)
            {
                await DisplayAlert("Conflict", "Item may has already been confirmed finish by other staffs.", "OK");
                return;
            }

            if (matchingOrderDetails != null)
            {
                foreach (var detail in matchingOrderDetails)
                {
                    // Update the status of each matching order detail
                    detail.Status = true;
                    var uri = new Uri(_config.BaseAddress + $"OrderDetail/{detail.OrderDetailId}");
                    var content = new StringContent(JsonConvert.SerializeObject(detail), Encoding.UTF8, "application/json");
                    HttpResponseMessage response = await _client.PutAsync(uri, content);

                    if (!response.IsSuccessStatusCode)
                    {
                        DisplayAlert("Error", $"Failed to update item {detail.MenuItem?.ItemName}.", "OK");
                        return;
                    }
                }

                // Check if all order details have status = true
                var order = matchingOrderDetails.FirstOrDefault()?.Order;
                if (order != null)
                {
                    var uri = new Uri(_config.BaseAddress + $"Order/{order.OrderId}");
                    HttpResponseMessage response1 = await _client.GetAsync(uri);
                    if (response1.IsSuccessStatusCode)
                    {
                        string data = await response1.Content.ReadAsStringAsync();
                        order = JsonConvert.DeserializeObject<Order>(data);
                        order.OrderDate = DateTime.Now;
                        var orderDetails = order?.OrderDetails;
                        if (orderDetails != null && orderDetails.All(od => od.Status == true))
                        {
                            // Update the status of the order
                            order.Status = true;
                            uri = new Uri(_config.BaseAddress + $"Order/{order.OrderId}");
                            var content = new StringContent(JsonConvert.SerializeObject(order), Encoding.UTF8, "application/json");
                            response1 = await _client.PutAsync(uri, content);
                        }
                    }
                }

                // Handle the ProcessingItem object here
                await DisplayAlert("Item Finished", $"Finished item {orderDetail.MenuItem?.ItemName}.", "OK");


                if (matchingOrderDetails.Count() != 0)
                {
                    var order2 = matchingOrderDetails.FirstOrDefault()?.Order;
                    var uri1 = new Uri(_config.BaseAddress + $"Order/{order2.OrderId}");
                    var response2 = await _client.GetAsync(uri1);
                    if (response2.IsSuccessStatusCode)
                    {
                        string data = await response2.Content.ReadAsStringAsync();
                        var order1 = JsonConvert.DeserializeObject<Order>(data);
                        //Send to client when order finished
                        if (matchingOrderDetails.Count > 0)
                        {
                            await SendNotificationAsync(order.Table.Qr, $"Finished item {orderDetail.MenuItem?.ItemName}.");
                        }
                    }
                }
                SendOrderConfirmationNotificationAsync();
                viewModel?.LoadOrderDetails();
            }
        }
    }

    //0395746221
    //0388536414
    private async void OnFinishNumberOfItemsClicked(object sender, EventArgs e)
    {
        var button = sender as Button;
        var orderDetail = button?.CommandParameter as OrderDetail;
        int input = 0;

        if (button?.Parent is HorizontalStackLayout stackLayout)
        {
            var entry = stackLayout.Children.OfType<Entry>().FirstOrDefault();
            if (entry != null && int.TryParse(entry.Text, out int value))
            {
                input = value;
            }
        }

        if (orderDetail != null && input > 0)
        {
            var viewModel = BindingContext as ItemToMakeListViewModel;

            viewModel.LoadOrderDetails();

            var matchingOrderDetails = viewModel?.AllOrderDetails
                .Where(od => od.Order?.OrderDate == orderDetail.Order?.OrderDate &&
                             od.MenuItem?.ItemName == orderDetail.MenuItem?.ItemName &&
                             od.Sugar == orderDetail.Sugar &&
                             od.Ice == orderDetail.Ice &&
                             od.Topping == orderDetail.Topping && !(bool)od.Status)
                .ToList();

            if (matchingOrderDetails.Count == 0)
            {
                await DisplayAlert("Conflict", "Item may has already been confirmed finish by other staffs.", "OK");
                return;
            }

            if (matchingOrderDetails != null)
            {
                foreach (var detail in matchingOrderDetails)
                {
                    detail.FinishedItem += input;

                    if (detail.FinishedItem < detail.Quantity)
                    {
                        SaveToPreference(detail);
                    }
                    else
                    {
                        detail.Status = true;
                        int remainder = (int)(detail.FinishedItem - detail.Quantity);
                        RemoveFromPreference(detail);

                        if (remainder > 0)
                        {
                            DistributeRemainder(matchingOrderDetails, detail, remainder);
                        }

                        var uri = new Uri(_config.BaseAddress + $"OrderDetail/{detail.OrderDetailId}");
                        var content = new StringContent(JsonConvert.SerializeObject(detail), Encoding.UTF8, "application/json");
                        HttpResponseMessage response = await _client.PutAsync(uri, content);
                        if (!response.IsSuccessStatusCode)
                        {
                            await DisplayAlert("Error", $"Failed to update item {detail.MenuItem?.ItemName}.", "OK");
                            return;
                        }
                        else
                        {
                            await DisplayAlert("Item Updated", $"Finished item " + detail.MenuItem.ItemName, "OK");
                            var order2 = matchingOrderDetails.FirstOrDefault()?.Order;
                            uri = new Uri(_config.BaseAddress + $"Order/{order2.OrderId}");
                            response = await _client.GetAsync(uri);
                            if (response.IsSuccessStatusCode)
                            {
                                string data = await response.Content.ReadAsStringAsync();
                                var order1 = JsonConvert.DeserializeObject<Order>(data);
                                await SendNotificationAsync(order1.Table.Qr, "Order has been finished !");
                            }
                        }
                    }
                }

                //check if the entire order is finished
                var order = matchingOrderDetails.FirstOrDefault()?.Order;
                if (order != null)
                {
                    var uri = new Uri(_config.BaseAddress + $"Order/{order.OrderId}");
                    HttpResponseMessage response = await _client.GetAsync(uri);
                    if (response.IsSuccessStatusCode)
                    {
                        string data = await response.Content.ReadAsStringAsync();
                        order = JsonConvert.DeserializeObject<Order>(data);
                        order.OrderDate = DateTime.Now;
                        var orderDetails = order?.OrderDetails;
                        if (orderDetails != null && orderDetails.All(od => od.Status == true))
                        {
                            order.Status = true;
                            await SendNotificationAsync(order.Table.Qr, "Order has been finished !");
                            uri = new Uri(_config.BaseAddress + $"Order/{order.OrderId}");
                            var content = new StringContent(JsonConvert.SerializeObject(order), Encoding.UTF8, "application/json");
                            response = await _client.PutAsync(uri, content);
                        }
                    }
                }

                if (matchingOrderDetails.Count > 0)
                {
                    await SendNotificationAsync(order.Table.Qr, $"{matchingOrderDetails.FirstOrDefault()?.MenuItem?.ItemName} has been finished");
                    SendOrderConfirmationNotificationAsync();
                }
                viewModel?.LoadOrderDetails();
            }
        }
    }

    //Send from staff to bartend
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
    // Method to display alerts
    private Task DisplayAlert(string title, string message, string cancel)
	{
		return Application.Current.MainPage.DisplayAlert(title, message, cancel);
	}

    private async void DistributeRemainder(List<OrderDetail> orderDetails, OrderDetail currentDetail, int remainder)
    {
        foreach (var detail in orderDetails)
        {
            if (remainder <= 0)
                break;

            if (detail.Order.OrderDate == currentDetail.Order.OrderDate &&
                detail.MenuItem.ItemName == currentDetail.MenuItem.ItemName &&
                detail.Sugar == currentDetail.Sugar &&
                detail.Ice == currentDetail.Ice &&
                detail.Topping == currentDetail.Topping &&
                !(bool)detail.Status)
            {
                int available = (int)(detail.Quantity - detail.FinishedItem);

                if (available >= remainder)
                {
                    detail.FinishedItem += remainder;
                    if (detail.FinishedItem == detail.Quantity)
                    {
                        detail.Status = true;
                    }
                    remainder = 0;
                }
                else
                {
                    detail.FinishedItem += available;
                    detail.Status = true;
                    remainder -= available;
                }
            }
        }
    }

    private void ClearOrderDetailsPreference()
    {
        // Remove the key "OrderDetails" from preferences
        if (Preferences.ContainsKey("OrderDetails"))
        {
            Preferences.Remove("OrderDetails");
            Console.WriteLine("OrderDetails preference cleared successfully.");
        }
        else
        {
            Console.WriteLine("No OrderDetails preference found to clear.");
        }
    }

    private void RemoveFromPreference(OrderDetail orderDetail)
    {
        var orderDetails = GetOrderDetailsFromPreference();
        orderDetails.RemoveAll(detail =>
            detail.Order.OrderDate == orderDetail.Order.OrderDate &&
            detail.MenuItem.ItemName == orderDetail.MenuItem.ItemName &&
            detail.Sugar == orderDetail.Sugar &&
            detail.Ice == orderDetail.Ice &&
            detail.Topping == orderDetail.Topping);
        SaveAllToPreference(orderDetails);
    }

    private void SaveToPreference(OrderDetail orderDetail)
    {
        var orderDetails = GetOrderDetailsFromPreference();
        var existingDetail = orderDetails.FirstOrDefault(detail =>
            detail.Order.OrderDate == orderDetail.Order.OrderDate &&
            detail.MenuItem.ItemName == orderDetail.MenuItem.ItemName &&
            detail.Sugar == orderDetail.Sugar &&
            detail.Ice == orderDetail.Ice &&
            detail.Topping == orderDetail.Topping);

        if (existingDetail != null)
        {
            orderDetails.Remove(existingDetail);
        }

        orderDetails.Add(orderDetail);
        SaveAllToPreference(orderDetails);
    }


    private List<OrderDetail> GetOrderDetailsFromPreference()
    {
        var serialized = Preferences.Get("OrderDetails", "[]");
        var anonymousObjects = JsonConvert.DeserializeObject<List<dynamic>>(serialized);

        var orderDetails = new List<OrderDetail>();

        foreach (var obj in anonymousObjects)
        {
            var orderDetail = new OrderDetail
            {
                FinishedItem = obj.FinishedItem,
                Sugar = obj.Sugar,
                Ice = obj.Ice,
                Topping = obj.Topping,
                IsCurrentItem = obj.IsCurrentItem,
                Quantity = obj.Quantity,
                Status = obj.Status,
                Order = new Order { OrderDate = obj.OrderDate },
                MenuItem = new Models.MenuItem { ItemName = obj.ItemName }
            };

            orderDetails.Add(orderDetail);
        }

        return orderDetails;
    }

    private void SaveAllToPreference(List<OrderDetail> orderDetails)
    {
        var settings = new JsonSerializerSettings
        {
            ReferenceLoopHandling = ReferenceLoopHandling.Ignore,
            NullValueHandling = NullValueHandling.Ignore,
            ContractResolver = new DefaultContractResolver
            {
                IgnoreSerializableAttribute = false,
            }
        };

        // Create a list of anonymous objects for serialization
        var serializedObjects = orderDetails.Select(detail => new
        {
            detail.FinishedItem,
            detail.Sugar,
            detail.Ice,
            detail.Topping,
            detail.IsCurrentItem,
            detail.Quantity,
            detail.Status,
            OrderDate = detail.Order.OrderDate, // Extract nested property
            ItemName = detail.MenuItem.ItemName // Extract nested property
        }).ToList();

        // Serialize the anonymous objects to JSON
        var serialized = JsonConvert.SerializeObject(serializedObjects, settings);

        // Save the JSON string to preferences
        Preferences.Set("OrderDetails", serialized);
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
        /*var popup = new NotificationPopup(Notifications);
         * 
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

public class ItemToMakeListViewModel : INotifyPropertyChanged
{
    private readonly HttpClient _client = new HttpClient(new HttpClientHandler
    {
        ServerCertificateCustomValidationCallback = (message, cert, chain, sslPolicyErrors) => true
    });
    private readonly ConfigApi _config = new ConfigApi();

    public ObservableCollection<GroupedMenuItem> GroupedMenuItems { get; set; } = new ObservableCollection<GroupedMenuItem>();

    public ObservableCollection<OrderDetail> FirstItemToMake { get; set; } = new ObservableCollection<OrderDetail>();
    public ObservableCollection<OrderDetail> SecondItemToMake { get; set; } = new ObservableCollection<OrderDetail>();

    public ObservableCollection<OrderDetail> DoneItems { get; set; } = new ObservableCollection<OrderDetail>();
    public List<OrderDetail> AllOrderDetails { get; set; } = new List<OrderDetail>(); // New list to store all order details

    private int _notFinished;
    public int notFinished
    {
        get => _notFinished;
        set
        {
            if (_notFinished != value)
            {
                _notFinished = value;
                OnPropertyChanged(nameof(notFinished));
            }
        }
    }

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
                var orderDetails = JsonConvert.DeserializeObject<List<OrderDetail>>(data)
                    .OrderBy(od => od.Order?.OrderDate)
                    .ThenBy(od => od.MenuItemId)
                    .ThenBy(od => od.Sugar)
                    .ThenBy(od => od.Ice)
                    .ThenBy(od => od.Topping).ToList();

                if (orderDetails != null)
                {
                    foreach (var orderDetail in orderDetails)
                    {
                        ParseOrderDetails(orderDetail);
                    }
                    AllOrderDetails.Clear();
                    AllOrderDetails.AddRange(orderDetails);

                    foreach (var orderDetail in orderDetails)
                    {
                        orderDetail.PropertyChanged += (s, e) => OnPropertyChanged(nameof(GroupedMenuItems));
                    }

                    // Get the saved started item from Preferences
                    string jsonStartedItems = Preferences.Get("IStartedThis", string.Empty);
                    OrderDetail startedItem = null;
                    if (!string.IsNullOrEmpty(jsonStartedItems))
                        startedItem = JsonConvert.DeserializeObject<OrderDetail>(jsonStartedItems);

                    // Group only ItemToMake by MenuItemId, Topping, Ice, and Sugar and sum the quantities
                    var firstPendingItems = orderDetails
                        .Where(od => (od.Status == null || od.Status == false) && od.Order.Status == false )
                        .GroupBy(od => new { od.MenuItemId, od.Topping, od.Ice, od.Sugar, od.FinishedItem }) // Include FinishedItem
                        .Select(g => new OrderDetail
                        {
                            MenuItemId = g.Key.MenuItemId,
                            MenuItem = g.First().MenuItem,
                            Quantity = g.Sum(od => od.Quantity),
                            Status = g.First().Status,
                            Description = string.Join(", ", g.Select(od => od.Description)),
                            Topping = g.Key.Topping,
                            Ice = g.Key.Ice,
                            Sugar = g.Key.Sugar,
                            FinishedItem = g.Key.FinishedItem, // Access FinishedItem
                            Order = new Order
                            {
                                OrderDate = g.Min(od => od.Order?.OrderDate)
                            }
                        }).Take(1).ToList(); // Only take the first item

                    var subsequentPendingItems = orderDetails
                        .Where(od => (od.Status == null /*&& !firstPendingItems.Contains(od)*/  && od.Order.Status == false))
                        .GroupBy(od => new { od.MenuItemId, od.Topping, od.Ice, od.Sugar, od.FinishedItem }) // Include FinishedItem
                        .Select(g => new OrderDetail
                        {
                            MenuItemId = g.Key.MenuItemId,
                            MenuItem = g.First().MenuItem,
                            Quantity = g.Sum(od => od.Quantity),
                            Status = g.First().Status,
                            Description = string.Join(", ", g.Select(od => od.Description)),
                            Topping = g.Key.Topping,
                            Ice = g.Key.Ice,
                            Sugar = g.Key.Sugar,
                            FinishedItem = g.Key.FinishedItem, // Access FinishedItem
                            Order = new Order
                            {
                                OrderDate = g.Min(od => od.Order?.OrderDate)
                            }
                        }).ToList();

                    if (firstPendingItems.Count > 0)
                    {
                        subsequentPendingItems.RemoveAll(od => od.MenuItemId == firstPendingItems[0].MenuItemId && od.Order.OrderDate == firstPendingItems[0].Order.OrderDate && od.Sugar == firstPendingItems[0].Sugar && od.Ice == firstPendingItems[0].Ice && od.Topping == firstPendingItems[0].Topping);
                    }

                    // Combine the two lists
                    var pendingItems = firstPendingItems.Concat(subsequentPendingItems).ToList();
                    notFinished = pendingItems.Count();

                    if (pendingItems.Count() > 3)
                    {
                        pendingItems = pendingItems.Take(3).ToList();
                    }

                    // Check if the first pending item matches the saved started item and skip if not
                    if (startedItem != null)
                    {
                        if (pendingItems.Any())
                        {
                            var firstPendingItem = pendingItems.First();
                            if ((firstPendingItem.MenuItem?.ItemName != startedItem.MenuItem?.ItemName ||
                                firstPendingItem.Description != startedItem.Description ||
                                firstPendingItem.Order?.OrderDate != startedItem.Order?.OrderDate) &&
                                firstPendingItem.Status == false)
                            {
                                if (pendingItems.Count > 0)
                                    pendingItems.RemoveAt(0);
                            }
                        }
                    }
                    else
                    {
                        if (pendingItems.Count > 0)
                            pendingItems.RemoveAt(0);
                    }


                    // Ensure there are always exactly 2 items in the pendingItems list
                    if (pendingItems.Count < 2)
                    {
                        // If fewer than 2 items after removal, add more from the original collection
                        var additionalItems = orderDetails
                            .Where(od => ((od.Status == null || od.Status == false) && !pendingItems.Contains(od) && od.Order.Status == false))
                            .GroupBy(od => new { od.MenuItemId, od.Topping, od.Ice, od.Sugar, od.FinishedItem })
                            .Select(g => new OrderDetail
                            {
                                MenuItemId = g.Key.MenuItemId,
                                MenuItem = g.First().MenuItem,
                                Quantity = g.Sum(od => od.Quantity),
                                Status = g.First().Status,
                                Description = string.Join(", ", g.Select(od => od.Description)),
                                Topping = g.Key.Topping,
                                Ice = g.Key.Ice,
                                Sugar = g.Key.Sugar,
                                FinishedItem = g.Key.FinishedItem,
                                Order = new Order
                                {
                                    OrderDate = g.Min(od => od.Order?.OrderDate)
                                }
                            })
                            .Take(2 - pendingItems.Count)
                            .ToList();
                        // Add the additional items to pendingItems
                        if (pendingItems.Count > 0)
                            if (pendingItems[0].MenuItemId != additionalItems[0].MenuItemId && pendingItems[0].Sugar != additionalItems[0].Sugar && pendingItems[0].Ice != additionalItems[0].Ice && pendingItems[0].Topping != additionalItems[0].Topping && pendingItems[0].Order.OrderDate != additionalItems[0].Order.OrderDate)
                                pendingItems.AddRange(additionalItems);
                    }

                    if (pendingItems.Count() > 2)
                    {
                        pendingItems = pendingItems.Take(2).ToList();
                    }

                    if (pendingItems.Count > 1)
                    {
                        var firstItem = pendingItems[0];
                        var secondItem = pendingItems[1];

                        bool areSimilar = firstItem.MenuItemId == secondItem.MenuItemId &&
                                          firstItem.Sugar == secondItem.Sugar &&
                                          firstItem.Ice == secondItem.Ice &&
                                          firstItem.Topping == secondItem.Topping &&
                                          firstItem.Status == secondItem.Status;

                        if (areSimilar)
                        {
                            pendingItems.RemoveAt(1);
                        }
                    }

                    /*foreach (var pendingItem in pendingItems)
                    {
                        var oldestOrderDetail = AllOrderDetails
                            .Where(od => od.MenuItemId == pendingItem.MenuItemId &&
                                         od.Sugar == pendingItem.Sugar &&
                                         od.Ice == pendingItem.Ice &&
                                         od.Topping == pendingItem.Topping &&
                                         od.Status == pendingItem.Status)
                            .OrderBy(od => od.Order.OrderDate)
                            .FirstOrDefault();

                        if (oldestOrderDetail != null)
                        {
                            pendingItem.Order.OrderDate = oldestOrderDetail.Order.OrderDate;
                        }
                    }*/


                    // Set IsCurrentItem property
                    if (pendingItems.Count > 0)
                    {
                        pendingItems[0].IsCurrentItem = true;
                    }

                    // Check if any item has Status = false and assign the first pendingItem's Status to false
                    if (pendingItems.Any(od => od.Status == false) || pendingItems.Any(od => od.Status == null))
                    {
                        if (pendingItems.Count > 0)
                        {
                            if (pendingItems[0].Status == false)
                            {
                                pendingItems[0].IsStartEnabled = false;
                                pendingItems[0].StatusText = "Processing";
                            }
                            else if (pendingItems[0].Status == null)
                            {
                                pendingItems[0].IsStartEnabled = true;
                                pendingItems[0].StatusText = "Start Item";
                            }
                        }
                    }

                    // Group ProcessingItems by MenuItemId, Topping, Ice, and Sugar and sum the quantities
                    var processingItems = orderDetails
                        .Where(od => od.Status == false)
                        .GroupBy(od => new { od.MenuItemId, od.Topping, od.Ice, od.Sugar, od.FinishedItem })
                        .Select(g => new OrderDetail
                        {
                            MenuItemId = g.Key.MenuItemId,
                            MenuItem = g.First().MenuItem,
                            Quantity = g.Sum(od => od.Quantity),
                            Status = g.First().Status,
                            Description = string.Join(", ", g.Select(od => od.Description)),
                            Topping = g.Key.Topping,
                            Ice = g.Key.Ice,
                            Sugar = g.Key.Sugar,
                            FinishedItem = g.Sum(od => od.FinishedItem ?? 0),
                            Order = new Order
                            {
                                OrderDate = g.Min(od => od.Order?.OrderDate)
                            }
                        }).ToList();

                    // Separate DoneItems without grouping
                    //var doneItems = orderDetails.Where(od => od.Status == true && od.Order?.OrderDate?.Date == DateTime.Today).ToList();
                    var doneItems = orderDetails.Where(od => od.Status == true && od.Order?.OrderDate >= DateTime.Now.AddHours(-24)).ToList();

                    GroupedMenuItems.Clear();

                    var allItems = pendingItems.Concat(processingItems);

                    foreach (var groupedItem in allItems.GroupBy(o => o.MenuItem?.ItemName).Select(g => new GroupedMenuItem
                    {
                        ItemToMake = g.Where(o => pendingItems.Contains(o)).ToList(),
                        ProcessingItems = g.Where(o => processingItems.Contains(o)).ToList()
                    }))
                    {
                        GroupedMenuItems.Add(groupedItem);
                    }

                    FirstItemToMake.Clear();
                    SecondItemToMake.Clear();

                    if (pendingItems.Count > 0)
                    {
                        FirstItemToMake.Add(pendingItems[0]);
                    }
                    if (pendingItems.Count > 1)
                    {
                        SecondItemToMake.Add(pendingItems[1]);
                    }

                    DoneItems.Clear();
                    foreach (var doneItem in doneItems)
                    {
                        DoneItems.Add(doneItem);
                    }
                }
            }
        }
        catch (Exception ex)
        {
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
        orderDetail.Ice = string.IsNullOrEmpty(orderDetail.Ice) ? "normal" : orderDetail.Ice;
        orderDetail.Sugar = string.IsNullOrEmpty(orderDetail.Sugar) ? "normal" : orderDetail.Sugar;
        orderDetail.Topping = string.IsNullOrEmpty(orderDetail.Topping) ? "none" : orderDetail.Topping;

        var orderDetails = GetOrderDetailsFromPreference();

        if (orderDetail != null && orderDetail.Order != null)
        {
            var finishedItem = orderDetails
                .FirstOrDefault(detail => detail.Order != null &&
                                          detail.Order.OrderDate == orderDetail.Order.OrderDate &&
                                          detail.Sugar == orderDetail.Sugar &&
                                          detail.Ice == orderDetail.Ice &&
                                          detail.Topping == orderDetail.Topping);

            if (finishedItem == null || finishedItem.FinishedItem == null)
            {
                orderDetail.FinishedItem = 0;
            }
            else
            {
                orderDetail.FinishedItem = finishedItem.FinishedItem;
            }
        }
        else
        {
            // Handle the case where orderDetail or orderDetail.Order is null
            orderDetail.FinishedItem = 0;
        }
    }

    private List<OrderDetail> GetOrderDetailsFromPreference()
    {
        var serialized = Preferences.Get("OrderDetails", "[]");
        var anonymousObjects = JsonConvert.DeserializeObject<List<dynamic>>(serialized);

        var orderDetails = new List<OrderDetail>();

        foreach (var obj in anonymousObjects)
        {
            var orderDetail = new OrderDetail
            {
                FinishedItem = obj.FinishedItem,
                Sugar = obj.Sugar,
                Ice = obj.Ice,
                Topping = obj.Topping,
                IsCurrentItem = obj.IsCurrentItem,
                Quantity = obj.Quantity,
                Status = obj.Status,
                Order = new Order { OrderDate = obj.OrderDate },
                MenuItem = new Models.MenuItem { ItemName = obj.ItemName }
            };

            orderDetails.Add(orderDetail);
        }

        return orderDetails;
    }




    public event PropertyChangedEventHandler? PropertyChanged;

    protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = "")
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    public class GroupedMenuItem
    {
        public List<OrderDetail> ItemToMake { get; set; } = new List<OrderDetail>();
        public List<OrderDetail> ProcessingItems { get; set; } = new List<OrderDetail>();
    }
}
