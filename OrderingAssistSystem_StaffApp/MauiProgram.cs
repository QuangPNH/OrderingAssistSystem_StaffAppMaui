using CommunityToolkit.Maui;
using Microsoft.Extensions.Logging;
using OrderingAssistSystem_StaffApp.Services;

namespace OrderingAssistSystem_StaffApp
{
    public static class MauiProgram
    {
        public static MauiAppBuilder RegisterServices(this MauiAppBuilder builder)
        {
#if ANDROID
    builder.Services.AddSingleton<IDeviceInstallationService, OrderingAssistSystem_StaffApp.Platforms.Android.DeviceInstallationService>();
#endif

            builder.Services.AddSingleton<IPushDemoNotificationActionService, PushDemoNotificationActionService>();
            builder.Services.AddSingleton<INotificationRegistrationService>(new NotificationRegistrationService(Config.BackendServiceEndpoint, Config.ApiKey));

            return builder;
        }
        public static MauiApp CreateMauiApp()
        {
            var builder = MauiApp.CreateBuilder();
            builder
                .UseMauiApp<App>()
                .ConfigureFonts(fonts =>
                {
                    fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                    fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
                }).
                RegisterServices()
                .RegisterViews().
             UseMauiCommunityToolkit();

            //builder.Services.AddHttpClient("api", httpClient => httpClient.BaseAddress = new Uri("https://localhost:7183"));

#if DEBUG
    		builder.Logging.AddDebug();
#endif

            return builder.Build();
        }
        public static MauiAppBuilder RegisterViews(this MauiAppBuilder builder)
        {
            builder.Services.AddSingleton<MainPage>();
            return builder;
        }
    }
}
