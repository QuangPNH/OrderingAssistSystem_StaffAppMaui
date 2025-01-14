using Newtonsoft.Json;
using OrderingAssistSystem_StaffApp.Models;
using System.Text;
using Twilio.Rest.Verify.V2.Service;
using Twilio;
using Twilio.TwiML.Voice;

namespace OrderingAssistSystem_StaffApp;

public partial class OTPPage : ContentPage
{
    private readonly string _phoneNumber;
    private readonly Employee _emp;

    public OTPPage(string phoneNumber, Employee emp)
    {
        InitializeComponent();
        _phoneNumber = phoneNumber;
        _emp = emp;
    }
    private async void OnVerifyOtpClicked(object sender, EventArgs e)
    {
        var otpInput = OtpEntry.Text;

        string otp = Preferences.Get("otp", string.Empty);

        if (string.IsNullOrWhiteSpace(otpInput) || otpInput.Length != 6 || !otpInput.All(char.IsDigit))
        {
            await DisplayAlert("Error", "Please enter a valid 6-digit OTP.", "OK");
            return;
        }

        if (otp.Equals(otpInput))
        {
            Preferences.Set("LoginInfo", Preferences.Get("TempLoginInfo", string.Empty));
            Preferences.Remove("TempLoginInfo");

            await Navigation.PushAsync(new PendingOrderList());
            //await DisplayAlert("Success", "Welcome " + _emp.EmployeeName + "!", "OK");
        }
        else
        {
            await DisplayAlert("Error", "Invalid OTP. Please try again.", "OK");
        }


        /*if (VerifySms(otpInput, _phoneNumber))
        {
            Preferences.Set("LoginInfo", Preferences.Get("TempLoginInfo", string.Empty));
            Preferences.Remove("TempLoginInfo");

            await Navigation.PushAsync(new PendingOrderList());
            await DisplayAlert("Success", "OTP verified!", "OK");
        }
        else
        {
            await DisplayAlert("Error", "Invalid OTP. Please try again.", "OK");
        }*/
    }

    private bool VerifySms(string code, string phone)
    {
        try
        {
            ConfigApi config = new ConfigApi();
            string[] parts = phone.Split(new char[] { '0' }, 2);
            string result = parts[1];
            /*var accountSid = nameof(settings.AccountSid);
            var authToken = nameof(settings.AuthToken);*/
            var accountSid = config.accId;
            var authToken = config.accToken;
            TwilioClient.Init(accountSid, authToken);

            var verificationCheck = VerificationCheckResource.Create(
                to: "+84" + result,
                code: code,
                pathServiceSid: "VA3c15cbc73df12d2ada324b4b96781ba7"
            );
            if (verificationCheck.Valid != true)
            {
                return false;
            }
        }
		catch (Exception e)
		{
			DisplayAlert("Error", "Failed to verify OTP: " + e, "OK");
			return false;
		}
		return true;
    }
}