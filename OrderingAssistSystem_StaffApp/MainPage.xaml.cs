using Microsoft.Maui.Storage;
using Newtonsoft.Json;
using OrderingAssistSystem_StaffApp.Models;
using Twilio;
using Twilio.Rest.Api.V2010.Account;
using Twilio.Types;
using System.Text.Json;
using OrderingAssistSystem_StaffApp.Services;
using CommunityToolkit.Maui.Alerts;
using CommunityToolkit.Maui.Core;
using System.Net.Http;
namespace OrderingAssistSystem_StaffApp
{
    public partial class MainPage : ContentPage
    {
        private readonly HttpClient _client;
        private readonly JsonSerializerOptions _serializerOptions;
        readonly INotificationRegistrationService _notificationRegistrationService;

        public MainPage(INotificationRegistrationService service)
        {
            _client = new HttpClient(new HttpClientHandler
                {
                    ServerCertificateCustomValidationCallback = (message, cert, chain, sslPolicyErrors) => true
                });

			InitializeComponent();
            InitializeFirebaseToken();
            //Navigation.PushAsync(new PendingOrderList());

            Authoriz();
            _notificationRegistrationService = service;
            CalculateRemainingDays();
            InitializeFirebaseToken();
        }

        private void InitializeFirebaseToken()
        {
            var token = Preferences.Get("FirebaseToken", string.Empty);
            if (!string.IsNullOrEmpty(token))
            {
                var deviceInstallationService = Application.Current.Windows[0].Page.Handler.MauiContext.Services.GetService<IDeviceInstallationService>();
                if (deviceInstallationService != null)
                {
                    deviceInstallationService.Token = token;
                    if (_notificationRegistrationService != null)
                    {
                        _notificationRegistrationService.RegisterDeviceAsync()
                            .ContinueWith((task) =>
                            {
                                ShowAlert(task.IsFaulted ? task.Exception.Message : $"Device registered");
                            });
                    }
                    else
                    {
                        Console.WriteLine("NotificationRegistrationService is not available.");
                    }
                }
                else
                {
                    Console.WriteLine("DeviceInstallationService is not available.");
                }
            }
            else
            {
                Console.WriteLine("Firebase token is not available.");
            }
        }

        private void CalculateRemainingDays()
        {
            string loginInfoJson = Preferences.Get("LoginInfo", string.Empty);
            if (string.IsNullOrEmpty(loginInfoJson))
            {
                //DisplayAlert("Error", "Login information is missing.", "Ok");
                return;
            }
            Employee emp = JsonConvert.DeserializeObject<Employee>(loginInfoJson);
            if (emp?.Owner == null)
            {
                DisplayAlert("Error", "Owner's information is missing.", "Ok");
                return;
            }

            DateTime? subscribeEndDate = emp.Owner.SubscribeEndDate;
            if (subscribeEndDate.HasValue)
            {
                DateTime endDateWithGracePeriod = subscribeEndDate.Value.AddDays(7);
                TimeSpan remainingTime = endDateWithGracePeriod - DateTime.Now;
                if (remainingTime.Days <= 0)
                {
                    DisplayAlert("Expired", $"Your owner's subscription to the service is expired for over a week.", "Ok");
                }
            }
        }

        
        public async Task Authoriz()
        {
            // Get login info from shared preferences
            var loginInfoJson = Preferences.Get("LoginInfo", string.Empty);
            var employee = JsonConvert.DeserializeObject<Employee>(loginInfoJson);

            if (employee != null && employee.RoleId == 2)
            {
				//Application.Current.MainPage = new NavigationPage(new ContentPage());
				await Navigation.PushAsync(new PendingOrderList());
			}
            else if (employee != null && employee.RoleId == 3)
            {
				//Application.Current.MainPage = new NavigationPage(new ContentPage());
				await Navigation.PushAsync(new PendingOrderList());
			}
            else
            {
                await DisplayAlert("Status", "Something went wrong.The owner's subscription may have been over for over a week. Contact for more info.", "OK");
            }
        }

        void OnRegisterButtonClicked(object sender, EventArgs e)
        {
            _notificationRegistrationService.RegisterDeviceAsync()
                .ContinueWith((task) =>
                {
                    ShowAlert(task.IsFaulted ? task.Exception.Message : $"Device registered");
                });
        }

        void ShowAlert(string message)
        {
            MainThread.BeginInvokeOnMainThread(async () =>
            {
                var toast = Toast.Make(message, ToastDuration.Long);
                await toast.Show();
            });
        }
#if ANDROID
        

        void OnDeregisterButtonClicked(object sender, EventArgs e)
        {
            _notificationRegistrationService?.DeregisterDeviceAsync()
                .ContinueWith((task) =>
                {
                    ShowAlert(task.IsFaulted ? task.Exception?.Message : $"Device deregistered");
                });
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();

            PermissionStatus status = await Permissions.RequestAsync<Permissions.PostNotifications>();
        }
#endif
        private async void OnSendOtpClicked(object sender, EventArgs e)
        {
            ConfigApi config = new ConfigApi();
            var phoneNumber = PhoneNumberEntry?.Text?.Trim();

            if (string.IsNullOrWhiteSpace(phoneNumber))
            {
                await DisplayAlert("Error", "Please enter a phone number.", "OK");
                return;
            }

            if (!System.Text.RegularExpressions.Regex.IsMatch(phoneNumber, @"^0[1-9]\d{8,14}$"))
            {
                await DisplayAlert("Error", "Invalid phone number format.", "OK");
                return;
            }

            Employee? emp = null;

            try
            {
                var uri = new Uri($"{config.BaseAddress}Employee/Staff/Phone/{phoneNumber}");
                HttpResponseMessage response = await _client.GetAsync(uri);
                if (response.IsSuccessStatusCode)
                {
                    string content = await response.Content.ReadAsStringAsync();
                    emp = JsonConvert.DeserializeObject<Employee>(content);
                }
            }
            catch (Exception ex)
            {
                await DisplayAlert("Error", $"Failed to retrieve employee. Exception: {ex.Message}", "OK");
            }

            if (emp == null)
            {
                await DisplayAlert("Error", phoneNumber + " is not found. Please enter an existing number.\n", "OK");
            }
            else
            {
                string otp = new Random().Next(000000, 999999).ToString();
                otp = "123456"; // For testing only, remove this line in production
                Preferences.Set("otp", otp);

                /*var accountSid = Environment.GetEnvironmentVariable("ACCOUNTSID");
                var authToken = Environment.GetEnvironmentVariable("AUTHTOKEN");

                TwilioClient.Init(accountSid, authToken);
                var messageOptions = new CreateMessageOptions(new PhoneNumber("+84" + phoneNumber.Split("0")[1]))
                {
                    From = new PhoneNumber("+13204336563"),
                    Body = "Your OTP is " + otp
                };
                MessageResource.Create(messageOptions);*/

                // Redirect to the OTP input page
                var empJson = JsonConvert.SerializeObject(emp);
                Preferences.Set("TempLoginInfo", empJson);

                await Navigation.PushAsync(new OTPPage(phoneNumber, emp));
            }
        }
    }

}
