using CommunityToolkit.Maui.Views;
using Newtonsoft.Json;
using OrderingAssistSystem_StaffApp.Models;
using OrderingAssistSystem_StaffApp.Services;
using System.Collections.ObjectModel;
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
		BindingContext = new CombinedViewModel();
        // Mock Notifications
        LoadNotifications();
    }

    private async void OnStartItemClicked(object sender, EventArgs e)
    {
        var button = sender as Button;
        var orderDetail = button?.CommandParameter as OrderDetail; // Cast to your Order type
        if (orderDetail != null)
        {
            // Update the status of the order and order detail
            orderDetail.Status = false;
            orderDetail.Order.Status = false;

            // Update the status via API
            await UpdateOrderStatus(orderDetail.Order);
            await UpdateOrderDetailStatus(orderDetail);

            // Handle the PendingItem object here
            await DisplayAlert("Item Started", $"Item: {orderDetail.MenuItem.ItemName} has been started.", "OK");
        }
    }

    private async void OnFinishItemClicked(object sender, EventArgs e)
    {
        var button = sender as Button;
        var orderDetail = button?.CommandParameter as OrderDetail; // Cast to your Order type
        if (orderDetail != null)
        {
            // Update the status of the order and order detail
            orderDetail.Status = true;
            orderDetail.Order.Status = true;

            // Update the status via API
            await UpdateOrderStatus(orderDetail.Order);
            await UpdateOrderDetailStatus(orderDetail);

            // Handle the ProcessingItem object here
            await DisplayAlert("Item Finished", $"Item: {orderDetail.MenuItem.ItemName} has been finished.", "OK");
        }
    }

    private async Task UpdateOrderStatus(Order order)
    {
        var uri = new Uri(_config.BaseAddress + $"Order/{order.OrderId}");
        var content = new StringContent(JsonConvert.SerializeObject(order), Encoding.UTF8, "application/json");
        await _client.PutAsync(uri, content);
    }

    private async Task UpdateOrderDetailStatus(OrderDetail orderDetail)
    {
        var uri = new Uri(_config.BaseAddress + $"OrderDetail/{orderDetail.OrderDetailId}");
        var content = new StringContent(JsonConvert.SerializeObject(orderDetail), Encoding.UTF8, "application/json");
        await _client.PutAsync(uri, content);
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

    /*// Navigate to Pending Orders List
    private async void OnPendingOrdersClicked(object sender, EventArgs e)
    {
        await Application.Current.MainPage.Navigation.PushAsync(new PendingOrderList());
    }

    // Navigate to Menu Item List
    private async void OnMenuItemsClicked(object sender, EventArgs e)
    {
        await Application.Current.MainPage.Navigation.PushAsync(new MenuItemList());
    }

    // Navigate to Items to Make
    private async void OnItemToMakeClicked(object sender, EventArgs e)
    {
        await Application.Current.MainPage.Navigation.PushAsync(new ItemToMake());
    }*/

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
        INotificationRegistrationService notificationRegistrationService = DependencyService.Get<INotificationRegistrationService>();
        // Reset the MainPage to the login page
        Application.Current.MainPage = new NavigationPage(new MainPage(notificationRegistrationService));
        await Task.CompletedTask; // Ensure the method is still async.
    }
}



