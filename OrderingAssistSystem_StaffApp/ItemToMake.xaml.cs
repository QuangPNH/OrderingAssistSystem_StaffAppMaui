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
        // Clear the page cache
        PageCache.Instance.ClearCache();
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
                .Where(od => od.MenuItem.ItemName == orderDetail.MenuItem.ItemName &&
                             od.Sugar == orderDetail.Sugar &&
                             od.Ice == orderDetail.Ice &&
                             od.Topping == orderDetail.Topping &&
                             od.Status == false).ToList();

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
                    detail.FinishedItem = detail.Quantity;
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
                foreach (var orderd in matchingOrderDetails)
                {
                    var order = orderd.Order;
                    if (order != null)
                    {
                        var uri = new Uri(_config.BaseAddress + $"Order/{order.OrderId}");
                        HttpResponseMessage response1 = await _client.GetAsync(uri);
                        if (response1.IsSuccessStatusCode)
                        {
                            string data = await response1.Content.ReadAsStringAsync();
                            order = JsonConvert.DeserializeObject<Order>(data);
                            //order.OrderDate = DateTime.Now;
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
                }
                //notihere send to employee and client
                SendOrderConfirmationNotificationAsync();
                await DisplayAlert("Ok", "Finished " + orderDetail.Quantity + " " + orderDetail.MenuItem.ItemName + ".", "OK");
                PageCache.Instance.ClearCache();
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
        int ogInput = 0;

        if (button?.Parent is HorizontalStackLayout stackLayout)
        {
            var entry = stackLayout.Children.OfType<Entry>().FirstOrDefault();
            if (entry != null && int.TryParse(entry.Text, out int value))
            {
                input = value;
                ogInput = value;
            }
        }
        if (orderDetail != null && input > 0)
        {
            var viewModel = BindingContext as ItemToMakeListViewModel;
            List<OrderDetail> all = viewModel?.AllOrderDetails;
            List<OrderDetail> matchingOrderDetails = all.Where(od => od.MenuItem.ItemName.Equals(orderDetail.MenuItem.ItemName) &&
                             od.Sugar.Equals(orderDetail.Sugar) &&
                             od.Ice.Equals(orderDetail.Ice) &&
                             od.Topping.Equals(orderDetail.Topping) && od.Status == false).ToList();

            if (matchingOrderDetails.Count == 0)
            {
                await DisplayAlert("Conflict", "Item may has already been confirmed finish by other staffs.", "OK");
                return;
            }

            if (matchingOrderDetails != null)
            {
                foreach (var detail in matchingOrderDetails)
                {
                    if (input <= 0)
                        break;

                    if (detail.FinishedItem == null)
                        detail.FinishedItem = 0;

                    if (input + detail.FinishedItem >= detail.Quantity)
                    {
                        detail.FinishedItem = detail.Quantity;
                        detail.Status = true;
                        input = (int)(input - detail.Quantity);

                        var uri = new Uri(_config.BaseAddress + $"OrderDetail/{detail.OrderDetailId}");
                        var content = new StringContent(JsonConvert.SerializeObject(detail), Encoding.UTF8, "application/json");
                        HttpResponseMessage response = await _client.PutAsync(uri, content);
                        if (!response.IsSuccessStatusCode)
                        {
                            await DisplayAlert("Error", $"Failed to update item {detail.MenuItem?.ItemName}.", "OK");
                            return;
                        }
                    }
                    else
                    {
                        detail.FinishedItem += input;
                        detail.Status = false;
                        var uri = new Uri(_config.BaseAddress + $"OrderDetail/{detail.OrderDetailId}");
                        var content = new StringContent(JsonConvert.SerializeObject(detail), Encoding.UTF8, "application/json");
                        HttpResponseMessage response = await _client.PutAsync(uri, content);
                        if (!response.IsSuccessStatusCode)
                        {
                            await DisplayAlert("Error", $"Failed to update item {detail.MenuItem?.ItemName}.", "OK");
                            return;
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
                        //order.OrderDate = DateTime.Now;
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

                /*if (matchingOrderDetails.Count > 0)
				{
					await SendNotificationAsync(order.Table.Qr, ogInput + " " + matchingOrderDetails.FirstOrDefault()?.MenuItem?.ItemName + " has been finished!");
					SendOrderConfirmationNotificationAsync();
				}*/
                //notihere send to employee and client
                PageCache.Instance.ClearCache();
                viewModel?.LoadOrderDetails();
                await DisplayAlert("Done", ogInput + " " + matchingOrderDetails.FirstOrDefault()?.MenuItem?.ItemName + " has been finished!", "OK");
                await SendNotificationAsync(matchingOrderDetails.FirstOrDefault()?.Order.Table.Qr, ogInput + " " + matchingOrderDetails.FirstOrDefault()?.MenuItem?.ItemName + " has been finished!");
                SendOrderConfirmationNotificationAsync();
            }
        }
    }

    //Send from staff to bartend
    private async Task SendOrderConfirmationNotificationAsync()
    {
        var requestBody = new
        {
            text = "Order Finished !",
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
    // Method to display alerts
    private Task DisplayAlert(string title, string message, string cancel)
    {
        return Application.Current.MainPage.DisplayAlert(title, message, cancel);
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
        var viewModel = BindingContext as CombinedViewModel;
        viewModel?.CalculateRemainingDays();
        SwitchToPage("ItemToMakeBartender", () => new ItemToMakeBartender());
    }

    private void OnProcessingClicked(object sender, EventArgs e)
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
    public string Role { get; set; }
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
        string loginInfoJson = Preferences.Get("LoginInfo", string.Empty);
        Employee emp = JsonConvert.DeserializeObject<Employee>(loginInfoJson);
        Role = emp.Role.RoleName;
        LoadOrderDetails();
    }



    public async void LoadOrderDetails()
    {
        try
        {
            var loginInfoJson = Preferences.Get("LoginInfo", string.Empty);
            var employee = JsonConvert.DeserializeObject<Employee>(loginInfoJson);
            var managerId = employee?.ManagerId ?? employee.EmployeeId;
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

                    // Settings for serialization and deserialization
                    var settings = new JsonSerializerSettings
                    {
                        ContractResolver = new IncludeIgnoredPropertiesResolver(),
                        DefaultValueHandling = DefaultValueHandling.Include
                    };

                    // Deserialize with the custom settings
                    string jsonStartedItems = Preferences.Get("IStartedThis", string.Empty);
                    OrderDetail startedItem = null;
                    if (!string.IsNullOrEmpty(jsonStartedItems))
                    {
                        startedItem = JsonConvert.DeserializeObject<OrderDetail>(jsonStartedItems, settings);
                    }

                    List<OrderDetail> list = orderDetails.Where(od => ((od.MenuItem.ItemName == startedItem.MenuItem.ItemName &&
                    od.Description == startedItem.Description &&
                    (od.Order.OrderDate >= startedItem.EarliestTime) &&
                    (od.Order.OrderDate <= startedItem.LatestTime) &&
                    od.Status == false)) || od.Status == null).ToList();


                    // Group only ItemToMake by MenuItemId, Topping, Ice, and Sugar and sum the quantities
                    var firstPendingItems = list
                        .Where(od => (od.Status == null || od.Status == false) && od.Order.Status == false)
                        .GroupBy(od => new { od.MenuItemId, od.Topping, od.Ice, od.Sugar }) // Include FinishedItem
                        .Select(g => new OrderDetail
                        {
                            MenuItemId = g.Key.MenuItemId,
                            MenuItem = g.First().MenuItem,
                            Quantity = g.Sum(od => od.Quantity),
                            //Status = g.First().Status,
                            Status = g.Any(od => od.Status == false) ? false : g.All(od => od.Status == null) ? (bool?)null : true,
                            Description = g.First().Description,
                            Topping = g.Key.Topping,
                            Ice = g.Key.Ice,
                            Sugar = g.Key.Sugar,
                            FinishedItem = g.Sum(od => od.FinishedItem ?? 0),
                            EarliestTime = g.Min(od => od.Order?.OrderDate),
                            LatestTime = g.Max(od => od.Order?.OrderDate),
                            Order = new Order
                            {
                                OrderDate = g.Min(od => od.Order?.OrderDate)
                            }
                        }).Take(1).ToList(); // Only take the first item

                    var subsequentPendingItems = list
                        .Where(od => (od.Status == null /*&& !firstPendingItems.Contains(od)*/  && od.Order.Status == false))
                        .GroupBy(od => new { od.MenuItemId, od.Topping, od.Ice, od.Sugar }) // Include FinishedItem
                        .Select(g => new OrderDetail
                        {
                            MenuItemId = g.Key.MenuItemId,
                            MenuItem = g.First().MenuItem,
                            Quantity = g.Sum(od => od.Quantity),
                            Status = g.Any(od => od.Status == false) ? false : g.All(od => od.Status == null) ? (bool?)null : true,
                            Description = g.First().Description,
                            Topping = g.Key.Topping,
                            Ice = g.Key.Ice,
                            Sugar = g.Key.Sugar,
                            EarliestTime = g.Min(od => od.Order?.OrderDate),
                            LatestTime = g.Max(od => od.Order?.OrderDate),
                            FinishedItem = g.Sum(od => od.FinishedItem ?? 0),
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

                    if (pendingItems.Count() > 3)
                    {
                        pendingItems = pendingItems.Take(3).ToList();
                    }

                    // Check if the first pending item matches the saved started item and skip if not
                    /*if (startedItem != null)
                    {
                        if (pendingItems.Any())
                        {
                            var firstPendingItem = pendingItems.First();
                            try
                            {
                                if (firstPendingItem.MenuItem.ItemName != startedItem.MenuItem.ItemName ||
                                    firstPendingItem.Description != startedItem.Description || startedItem.LatestTime == null ||
                                     !((firstPendingItem.Order.OrderDate >= startedItem.EarliestTime) && (firstPendingItem.Order.OrderDate <= startedItem.LatestTime)) &&
                                    firstPendingItem.Status == false)
                                {
                                    if (pendingItems.Count > 0)
                                        pendingItems.RemoveAt(0);
                                }
                            }
                            catch
                            {

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
                        var additionalItems = list
                            .Where(od => ((od.Status == null || od.Status == false) && !pendingItems.Contains(od) && od.Order.Status == false))
                            .GroupBy(od => new { od.MenuItemId, od.Topping, od.Ice, od.Sugar, od.FinishedItem })
                            .Select(g => new OrderDetail
                            {
                                MenuItemId = g.Key.MenuItemId,
                                MenuItem = g.First().MenuItem,
                                Quantity = g.Sum(od => od.Quantity),
                                Status = g.Any(od => od.Status == false) ? false : g.All(od => od.Status == null) ? (bool?)null : true,
                                Description = g.First().Description,
                                Topping = g.Key.Topping,
                                Ice = g.Key.Ice,
                                Sugar = g.Key.Sugar,
                                EarliestTime = g.Min(od => od.Order?.OrderDate),
                                LatestTime = g.Max(od => od.Order?.OrderDate),
                                FinishedItem = g.Sum(od => od.FinishedItem ?? 0),
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
                            Description = g.First().Description,
                            Topping = g.Key.Topping,
                            Ice = g.Key.Ice,
                            Sugar = g.Key.Sugar,
                            EarliestTime = g.Min(od => od.Order?.OrderDate),
                            LatestTime = g.Max(od => od.Order?.OrderDate),
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
                        pendingItems[0].FinishedItem = pendingItems[0].FinishedItem == null ? 0 : pendingItems[0].FinishedItem;
                        pendingItems[0].Quantity = pendingItems[0].Quantity - pendingItems[0].FinishedItem;
                        FirstItemToMake.Add(pendingItems[0]);
                    }
                    if (pendingItems.Count > 1)
                    {
                        pendingItems[1].FinishedItem = pendingItems[1].FinishedItem == null ? 0 : pendingItems[1].FinishedItem;
                        pendingItems[1].Quantity = pendingItems[1].Quantity - pendingItems[1].FinishedItem;
                        SecondItemToMake.Add(pendingItems[1]);
                    }

                    DoneItems.Clear();
                    foreach (var doneItem in doneItems)
                    {
                        DoneItems.Add(doneItem);
                    }
                    notFinished = AllOrderDetails
             .Where(orderDetail => orderDetail.Status != true)
             .Sum(orderDetail => orderDetail.Quantity ?? 0);
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

        /*var orderDetails = GetOrderDetailsFromPreference();

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
        }*/
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



public class IncludeIgnoredPropertiesResolver : DefaultContractResolver
{
    protected override IList<JsonProperty> CreateProperties(Type type, MemberSerialization memberSerialization)
    {
        // Get all properties, including ones marked with [JsonIgnore]
        var properties = base.CreateProperties(type, memberSerialization);
        foreach (var prop in properties)
        {
            // Ignore the [JsonIgnore] attribute for deserialization
            prop.Ignored = false;
        }
        return properties;
    }
}