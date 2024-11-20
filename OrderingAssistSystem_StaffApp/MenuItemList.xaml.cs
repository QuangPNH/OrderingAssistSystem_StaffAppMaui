using Newtonsoft.Json;
using OrderingAssistSystem_StaffApp.Models;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;

namespace OrderingAssistSystem_StaffApp;

public partial class MenuItemList : ContentPage
{
	public ObservableCollection<CartItem> CartItems { get; set; }
	public MenuItemList()
	{

		InitializeComponent();

		CartItems = new ObservableCollection<CartItem>
	{
		new CartItem
		{
			ItemName = "Pizza",
			Quantity = 1,
			Price = 10.0,
			Discount = 1.0
		},
		new CartItem
		{
			ItemName = "Burger",
			Quantity = 2,
			Price = 5.0,
			Discount = 0.5
		}
	};

		// Parse the attributes for example descriptions
		CartItems[0].ParseDescription("less Ice, normal Sugar, Hạt Bí");
		CartItems[1].ParseDescription("normal Ice, less Sugar, Hướng Duong");

		CartList.ItemsSource = CartItems;
		//BindingContext = CartItems;
	}

	private void ShowCartPopup(object sender, EventArgs e)
	{
		CartPopup.IsVisible = true;
	}

	private void HideCartPopup(object sender, EventArgs e)
	{
		CartPopup.IsVisible = false;
	}

	private void UpdateTotalPrice()
	{
		var totalPrice = CartItems.Sum(item => (item.Price - item.Discount) * item.Quantity);
		TotalPriceLabel.Text = totalPrice.ToString("C");
	}



	// You can bind this to your command in a real application
	private void CreateOrder()
	{
		// Logic to create the order
	}
}

public class CartItem
{
	public string ItemName { get; set; }
	public int Quantity { get; set; }
	public string Sugar { get; set; } = "Normal"; // Default value
	public string Ice { get; set; } = "Normal";   // Default value
	public string Topping { get; set; } = string.Empty;
	public double Price { get; set; }
	public double Discount { get; set; }

	public string Attributes => $"{Ice} Ice, {Sugar} Sugar{(string.IsNullOrEmpty(Topping) ? "" : $", {Topping}")}";

	public Command RemoveCommand => new Command(() => App.Current.MainPage.DisplayAlert("Remove", "Removing Item!", "OK"));

	// Parse description to fill Sugar, Ice, and Topping
	public void ParseDescription(string description)
	{
		string[] attributes = description.Split(',');

		foreach (var attribute in attributes)
		{
			string trimmed = attribute.Trim(); // Remove whitespace

			if (trimmed.Contains("Ice", StringComparison.OrdinalIgnoreCase))
			{
				Ice = trimmed.Replace("Ice", "", StringComparison.OrdinalIgnoreCase).Trim();
			}
			else if (trimmed.Contains("Sugar", StringComparison.OrdinalIgnoreCase))
			{
				Sugar = trimmed.Replace("Sugar", "", StringComparison.OrdinalIgnoreCase).Trim();
			}
			else
			{
				Topping += (string.IsNullOrEmpty(Topping) ? "" : ", ") + trimmed;
			}
		}
	}
}

public class MenuItemListViewModel : INotifyPropertyChanged
{
	private string _searchText;
	private ItemCategory _selectedCategory;

	public ObservableCollection<MenuItemViewModel> MenuItems { get; set; }
	public ObservableCollection<ItemCategory> Categories { get; set; }

	public string SearchText
	{
		get => _searchText;
		set
		{
			_searchText = value;
			OnPropertyChanged();
			FilterMenuItems();
		}
	}

	public ItemCategory SelectedCategory
	{
		get => _selectedCategory;
		set
		{
			_selectedCategory = value;
			OnPropertyChanged();
			FilterMenuItems();
		}
	}

	public ObservableCollection<MenuItemViewModel> FilteredMenuItems { get; set; }

	public ICommand AddToCartCommand { get; set; }

	public MenuItemListViewModel()
	{
		// Initialize collections
		MenuItems = new ObservableCollection<MenuItemViewModel>();
		FilteredMenuItems = new ObservableCollection<MenuItemViewModel>();
		Categories = new ObservableCollection<ItemCategory>();

		// Load data (replace with actual data fetching)
		LoadCategories();
		LoadMenuItems();

		// Initialize commands
		AddToCartCommand = new Command<MenuItemViewModel>(AddToCart);

		// Initially, filtered items are all items
		FilterMenuItems();
	}

	private void LoadCategories()
	{
		// Mock data for categories
		Categories.Add(new ItemCategory { ItemCategoryName = "Beverage" });
		Categories.Add(new ItemCategory { ItemCategoryName = "Food" });
		Categories.Add(new ItemCategory { ItemCategoryName = "Dessert" });
		// Add more categories as needed
	}

	private void LoadMenuItems()
	{
		// Mock data for menu items
		MenuItems.Add(new MenuItemViewModel
		{
			MenuItemId = 1,
			ItemName = "Latte",
			Price = 3.50,
			Category = "Beverage",
			AvailableToppings = new ObservableCollection<ToppingViewModel>
				{
					new ToppingViewModel { ItemName = "Whipped Cream" },
					new ToppingViewModel { ItemName = "Caramel" },
					new ToppingViewModel { ItemName = "Chocolate" }
				}
		});

		MenuItems.Add(new MenuItemViewModel
		{
			MenuItemId = 2,
			ItemName = "Burger",
			Price = 5.00,
			Category = "Food",
			AvailableToppings = new ObservableCollection<ToppingViewModel>
				{
					new ToppingViewModel { ItemName = "Cheese" },
					new ToppingViewModel { ItemName = "Bacon" },
					new ToppingViewModel { ItemName = "Lettuce" }
				}
		});

		MenuItems.Add(new MenuItemViewModel
		{
			MenuItemId = 3,
			ItemName = "Ice Cream",
			Price = 2.50,
			Category = "Dessert",
			AvailableToppings = new ObservableCollection<ToppingViewModel>
				{
					new ToppingViewModel { ItemName = "Sprinkles" },
					new ToppingViewModel { ItemName = "Hot Fudge" },
					new ToppingViewModel { ItemName = "Cherries" }
				}
		});

		// Add more MenuItems as needed
	}

	private void FilterMenuItems()
	{
		var filtered = MenuItems.Where(mi =>
			(string.IsNullOrWhiteSpace(SearchText) || mi.ItemName.ToLower().Contains(SearchText.ToLower())) &&
			(SelectedCategory == null || mi.Category == SelectedCategory.ItemCategoryName));

		FilteredMenuItems.Clear();

		foreach (var item in filtered)
			FilteredMenuItems.Add(item);
	}

	private void AddToCart(MenuItemViewModel menuItem)
	{
		if (!menuItem.IsAvailable)
		{
			// Optionally notify user that item is disabled
			return;
		}

		if (menuItem.Quantity <= 0)
		{
			// Optionally notify user to enter a valid quantity
			return;
		}

		// Create OrderDetail
		var orderDetail = new OrderDetail
		{
			MenuItemId = menuItem.MenuItemId,
			Quantity = menuItem.Quantity,
			Sugar = menuItem.Sugar,
			Ice = menuItem.Ice,
			Topping = string.Join(", ", menuItem.AvailableToppings.Where(t => t.IsSelected).Select(t => t.ItemName)),
			Description = $"{menuItem.Ice ?? "Normal"} Ice, {menuItem.Sugar ?? "Normal"} Sugar, {string.Join(", ", menuItem.AvailableToppings.Where(t => t.IsSelected).Select(t => t.ItemName))}",
			Status = null // Not processed yet
		};

		// Add to cart using Preferences
		// Serialize the OrderDetail and store it in Preferences
		string cartJson = Preferences.Get("Cart", "[]");
		var cart = JsonConvert.DeserializeObject<ObservableCollection<OrderDetail>>(cartJson) ?? new ObservableCollection<OrderDetail>();
		cart.Add(orderDetail);
		Preferences.Set("Cart", JsonConvert.SerializeObject(cart));

		// Optionally notify user that item was added
		Application.Current.MainPage.DisplayAlert("Success", $"{orderDetail.MenuItem.ItemName} added to cart.", "OK");
	}

	public event PropertyChangedEventHandler PropertyChanged;

	protected void OnPropertyChanged([CallerMemberName] string name = null)
	{
		PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
	}


}

public class MenuItemViewModel : INotifyPropertyChanged
{
	private bool _isAvailable = true;
	private int _quantity = 1;
	private string _sugar = "Normal";
	private string _ice = "Normal";

	public int MenuItemId { get; set; }
	public string ItemName { get; set; }
	public double Price { get; set; }
	public string Category { get; set; }

	public bool IsAvailable
	{
		get => _isAvailable;
		set
		{
			_isAvailable = value;
			OnPropertyChanged();
		}
	}

	public int Quantity
	{
		get => _quantity;
		set
		{
			_quantity = value;
			OnPropertyChanged();
		}
	}

	public string Sugar
	{
		get => _sugar;
		set
		{
			_sugar = value;
			OnPropertyChanged();
			UpdateDescription();
		}
	}

	public string Ice
	{
		get => _ice;
		set
		{
			_ice = value;
			OnPropertyChanged();
			UpdateDescription();
		}
	}

	public ObservableCollection<ToppingViewModel> AvailableToppings { get; set; }

	public ICommand AddToCartCommand { get; set; }

	public MenuItemViewModel()
	{
		AddToCartCommand = new Command<MenuItemViewModel>(AddToCart);
	}

	private void AddToCart(MenuItemViewModel menuItem)
	{
		// The actual implementation is handled in the parent ViewModel
	}

	private void UpdateDescription()
	{
		// Combine Sugar, Ice, and Toppings into Description
		var selectedToppings = AvailableToppings.Where(t => t.IsSelected).Select(t => t.ItemName);
		Description = $"{Ice} Ice, {Sugar} Sugar, {string.Join(", ", selectedToppings)}";
		OnPropertyChanged(nameof(Description));
	}

	private string _description;
	public string Description
	{
		get => _description;
		set
		{
			_description = value;
			OnPropertyChanged();
		}
	}

	public event PropertyChangedEventHandler PropertyChanged;

	protected void OnPropertyChanged([CallerMemberName] string name = null)
	{
		PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
	}
}

public class ToppingViewModel : INotifyPropertyChanged
{
	private bool _isSelected = false;
	public string ItemName { get; set; }

	public bool IsSelected
	{
		get => _isSelected;
		set
		{
			_isSelected = value;
			OnPropertyChanged();
		}
	}

	public event PropertyChangedEventHandler PropertyChanged;

	protected void OnPropertyChanged([CallerMemberName] string name = null)
	{
		PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
	}
}

public class ItemCategory
{
	public int ItemCategoryId { get; set; }
	public string ItemCategoryName { get; set; }
	public string Description { get; set; }
	public double Discount { get; set; }
	public string Image { get; set; }
	public bool IsDelete { get; set; }
}


