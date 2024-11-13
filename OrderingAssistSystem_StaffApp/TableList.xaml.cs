using OrderingAssistSystem_StaffApp.Models;
using System.Net.Http.Json;

namespace OrderingAssistSystem_StaffApp;

public partial class TableList : ContentPage
{
    private readonly HttpClient _httpClient;

    public TableList()
    {
        InitializeComponent();
        _httpClient = new HttpClient();
        LoadTablesAsync();
    }

    public async Task<List<Table>> GetTablesAsync()
    {
        Config config = new Config();
        var url = $"{config.BaseAddress}Table";
        var tables = await _httpClient.GetFromJsonAsync<List<Table>>(url);
        return tables ?? new List<Table>();
    }

    private async void LoadTablesAsync()
    {
        var tables = await GetTablesAsync();
        foreach (var table in tables)
        {
            var tableRow = new StackLayout
            {
                Orientation = StackOrientation.Horizontal,
                Children =
                {
                    new Label { Text = table.TableId.ToString() },
                    new Label { Text = table.Qr },
                    new Label { Text = table.Status.ToString() },
                    new Label { Text = table.EmployeeId.ToString() }
                }
            };
            TableListLayout.Children.Add(tableRow);
        }
    }
}
