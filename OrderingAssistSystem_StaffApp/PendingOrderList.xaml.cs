using OrderingAssistSystem_StaffApp.Models;
using System.Collections.ObjectModel;
using System.Globalization;
using Microsoft.Maui.Controls;
using MenuItem = OrderingAssistSystem_StaffApp.Models.MenuItem;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace OrderingAssistSystem_StaffApp;

public partial class PendingOrderList : ContentPage
{
	public PendingOrderList()
	{
		InitializeComponent();
        BindingContext = new CombinedViewModel();
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

public class PendingOrderViewModel
{
    public ObservableCollection<Order> Orders { get; set; }

    public PendingOrderViewModel()
    {
        // Sample Data
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
                    new OrderDetail { OrderDetailId = 1, MenuItem = new MenuItem { ItemName = "Pizza" }, Quantity = 2, Status = true },
                    new OrderDetail { OrderDetailId = 2, MenuItem = new MenuItem { ItemName = "Pasta" }, Quantity = 1, Status = false },
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
                    new OrderDetail { OrderDetailId = 3, MenuItem = new MenuItem { ItemName = "Burger" }, Quantity = 3, Status = true },
                    new OrderDetail { OrderDetailId = 4, MenuItem = new MenuItem { ItemName = "Fries" }, Quantity = 2, Status = true },
                }
            }
        };
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
            new OrderDetail { OrderDetailId = 1, ItemName = "Pizza", Quantity = 2, Status = null, MenuItem = new MenuItem { ItemName = "Pizza" } },
            new OrderDetail { OrderDetailId = 2, ItemName = "Pizza", Quantity = 1, Status = false, MenuItem = new MenuItem { ItemName = "Pizza" } },
            new OrderDetail { OrderDetailId = 3, ItemName = "Pasta", Quantity = 3, Status = true, MenuItem = new MenuItem { ItemName = "Pasta" } },
            new OrderDetail { OrderDetailId = 4, ItemName = "Pasta", Quantity = 1, Status = null, MenuItem = new MenuItem { ItemName = "Pasta" } },
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