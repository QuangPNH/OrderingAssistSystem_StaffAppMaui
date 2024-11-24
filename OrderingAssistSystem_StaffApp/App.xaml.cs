using OrderingAssistSystem_StaffApp.Models;

namespace OrderingAssistSystem_StaffApp
{
    public partial class App : Application
    {
        public static PageCache PageCache { get; private set; }
        public App()
        {
            InitializeComponent();



			//MainPage = new AppShell();
            MainPage = new NavigationPage(new MainPage());


            //MainPage = new AppTabbedPage();
        }
    }
}
