using CommunityToolkit.Maui.Views;
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

	// Navigate to Pending Orders List
	private async void OnPendingOrdersClicked(object sender, EventArgs e)
	{
        await Application.Current.MainPage.Navigation.PushAsync(new PendingOrderList());
        Application.Current.MainPage = new NavigationPage(new PendingOrderList());
        //await Navigation.PushAsync(new PendingOrderList());
	}

	// Navigate to Menu Item List
	private async void OnMenuItemsClicked(object sender, EventArgs e)
	{
        await Application.Current.MainPage.Navigation.PushAsync(new MenuItemList());
        Application.Current.MainPage = new NavigationPage(new MenuItemList());
        //await Navigation.PushAsync(new MenuItemList());
	}

	private async void OnItemToMakeClicked(object sender, EventArgs e)
	{
        await Application.Current.MainPage.Navigation.PushAsync(new ItemToMake());
        Application.Current.MainPage = new NavigationPage(new ItemToMake());
        //await Navigation.PushAsync(new ItemToMake());
	}

	private async void OnLogOutClicked(object sender, EventArgs e)
	{
		Preferences.Remove("LoginInfo");
        await Application.Current.MainPage.Navigation.PushAsync(new MainPage());
        Application.Current.MainPage = new NavigationPage(new MainPage());
        //await Navigation.PushAsync(new MainPage());
	}
}



