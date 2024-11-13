using Microsoft.Maui.Storage;
using Newtonsoft.Json;
using OrderingAssistSystem_StaffApp.Models;
using Twilio;
using Twilio.Rest.Api.V2010.Account;
using Twilio.Types;
using OrderingAssistSystem_StaffApp.Models;
using Twilio.TwiML.Voice;
using System.Text.Json;
using Config = OrderingAssistSystem_StaffApp.Models.Config;


namespace OrderingAssistSystem_StaffApp
{
    public partial class MainPage : ContentPage
    {
        HttpClient _client;
        JsonSerializerOptions _serializerOptions;


        public MainPage()
        {
            InitializeComponent();
            _client = new HttpClient();
            _serializerOptions = new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                WriteIndented = true
            };
        }

        private async void OnSendOtpClicked(object sender, EventArgs e)
        {
            Config config = new Config();
            var phoneNumber = PhoneNumberEntry.Text?.Replace("\0", "").Trim();
            string placeholder = "";

            if (string.IsNullOrWhiteSpace(phoneNumber))
            {
                await DisplayAlert("Error", "Please enter a phone number.", "OK");
                return;
            }

            Employee emp = null;


            Uri uri = new Uri(string.Format(config._apiUrl, string.Empty));
            try
            {
                HttpResponseMessage response = await _client.GetAsync(uri);
                if (response.IsSuccessStatusCode)
                {
                    string content = await response.Content.ReadAsStringAsync();
                    emp = Newtonsoft.Json.JsonSerializer.Deserialize<Employee>(content, _serializerOptions);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(@"\tERROR {0}", ex.Message);
            }


            /*using (HttpClient client = new HttpClient(new HttpClientHandler
            {
                ServerCertificateCustomValidationCallback = (message, cert, chain, sslPolicyErrors) => true
            }))
            {
                try
                {
                    HttpResponseMessage res = await client.GetAsync(config._apiUrl + "Employee/Staff/Phone/" + phoneNumber);
                    string data = await res.Content.ReadAsStringAsync();
                    placeholder = data;
                    emp = JsonConvert.DeserializeObject<Employee>(data);

                    if (!res.IsSuccessStatusCode)
                    {
                        await DisplayAlert("Error", $"Failed to send OTP. Error code: {(int)res.StatusCode}", "OK");
                        return;
                    }
                }
                catch (Exception ee)
                {
                    await DisplayAlert("Error", $"Failed to send OTP. Exception: {ee.Message}", "OK");
                    return;
                }
            }*/

            if (emp == null)
            {
                await DisplayAlert("Error", "Failed to send OTP. Try again.\n" + placeholder + phoneNumber, "OK");
            }
            else
            {
                string otp = new Random().Next(000000, 999999).ToString();

                otp = "123456"; // For testing only, remove this line in production

                Preferences.Set("otp", otp);

                // Redirect to the OTP input page
                var empJson = JsonConvert.SerializeObject(emp);
                Preferences.Set("TempLoginInfo", empJson);

                await Navigation.PushAsync(new OTPPage(phoneNumber));
            }
        }
    }

}
