using OrderingAssistSystem_StaffApp.Models;
using System.Net.Http.Json;

namespace OrderingAssistSystem_StaffApp;

public partial class OrderList : ContentPage
{
    private readonly HttpClient _httpClient;

    public OrderList()
    {
        InitializeComponent();
        _httpClient = new HttpClient();
        DisplayOrdersAsync();
    }

    public async Task<List<Order>> GetOrdersAsync()
    {
        Config config = new Config();
        var orders = await _httpClient.GetFromJsonAsync<List<Order>>($"{config.BaseAddress}Order");
        return orders ?? new List<Order>();
    }

    public async void DisplayOrdersAsync()
    {
        var orders = await GetOrdersAsync();
        foreach (var order in orders)
        {
            var orderDetails = new Label
            {
                Text = $"Order ID: {order.OrderId}, Date: {order.OrderDate}, Cost: {order.Cost}, Tax: {order.Tax}, Status: {order.Status}",
                FontSize = 14,
                Margin = new Thickness(5)
            };
            OrdersStackLayout.Children.Add(orderDetails);
        }
    }
}
