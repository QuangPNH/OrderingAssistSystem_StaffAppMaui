﻿using Microsoft.Maui.Storage;
using Newtonsoft.Json;
using OrderingAssistSystem_StaffApp.Models;
using Twilio;
using Twilio.Rest.Api.V2010.Account;
using Twilio.Types;
using OrderingAssistSystem_StaffApp.Models;
using System.Text.Json;

namespace OrderingAssistSystem_StaffApp
{
    public partial class MainPage : ContentPage
    {


        private readonly HttpClient _client;
        private readonly JsonSerializerOptions _serializerOptions;



        public MainPage()
        {

            _client = new HttpClient(new HttpClientHandler
                {
                    ServerCertificateCustomValidationCallback = (message, cert, chain, sslPolicyErrors) => true
                });

            _serializerOptions = new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                WriteIndented = true
            };



            InitializeComponent();

            Authoriz();


        }



        public async Task Authoriz()
        {
            DisplayAlert("Status", Preferences.Get("LoginInfo", string.Empty), "OK");

            AuthorizeLogin authorizeLogin = new AuthorizeLogin(_client, _serializerOptions);

            var loginStatus = await authorizeLogin.CheckLogin();
            if (loginStatus.Equals("staff"))
            {
                DisplayAlert("Status", "staff", "OK");
            }
            else if (loginStatus.Equals("bartender"))
            {
                DisplayAlert("Status", "bartender", "OK");
            }
            else if (loginStatus.Equals("employee expired"))
            {
                DisplayAlert("Status", "The owner's subscription has been over for over a week. Contact for more info.", "OK");
            }
            else if(loginStatus.Equals("null"))
            {
                DisplayAlert("Status", Preferences.Get("LoginInfo", string.Empty) +  "null", "OK");
            }
            else
            {
                DisplayAlert("Status", "Nothing much really", "OK");
            }
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

            Employee? emp = null;


            try
            {
                var uri = new Uri($"{config.BaseAddress}Employee/Staff/Phone/{phoneNumber}");
                HttpResponseMessage response = await _client.GetAsync(uri);

                if (response.IsSuccessStatusCode)
                {
                    string content = await response.Content.ReadAsStringAsync();
                    emp = System.Text.Json.JsonSerializer.Deserialize<Employee>(content, _serializerOptions);
                }
                else
                {
                    await DisplayAlert("Error", $"Failed to retrieve employee. Error code: {(int)response.StatusCode}", "OK");
                }
            }
            catch (Exception ex)
            {
                await DisplayAlert("Error", $"Failed to retrieve employee. Exception: {ex.Message}", "OK");
            }





            /*using (HttpClient client = new HttpClient(new HttpClientHandler
            {
                ServerCertificateCustomValidationCallback = (message, cert, chain, sslPolicyErrors) => true
            }))
            {
                try
                {
                    HttpResponseMessage res = await client.GetAsync(config.BaseAddress + "Employee/Staff/Phone/" + phoneNumber);
                    string data = await res.Content.ReadAsStringAsync();
                    int something = (int)res.StatusCode;
                    placeholder = data;
                    emp = JsonConvert.DeserializeObject<Employee?>(data);

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
