using CommunityToolkit.Maui.Views;
using OrderingAssistSystem_StaffApp.Models;
using System.Collections.ObjectModel;

namespace OrderingAssistSystem_StaffApp;

public partial class ItemToMake : ContentPage
{
	private ObservableCollection<Models.Notification> Notifications;
	public ItemToMake()
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

        // Reset the MainPage to the login page
        Application.Current.MainPage = new NavigationPage(new MainPage());
        await Task.CompletedTask; // Ensure the method is still async.
    }
}



