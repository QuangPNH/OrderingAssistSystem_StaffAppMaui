using Newtonsoft.Json;
using System.Text;

namespace OrderingAssistSystem_StaffApp;

public partial class OTPPage : ContentPage
{
    private readonly string _phoneNumber;


    public OTPPage(string phoneNumber)
    {
        InitializeComponent();
        _phoneNumber = phoneNumber;
    }
    private async void OnVerifyOtpClicked(object sender, EventArgs e)
    {
        var otpInput = OtpEntry.Text;

        string otp = Preferences.Get("otp", string.Empty);

        if (string.IsNullOrWhiteSpace(otpInput))
        {
            await DisplayAlert("Error", "Please enter the OTP.", "OK");
            return;
        }

        if (otp.Equals(otpInput))
        {
            Preferences.Set("LoginInfo", Preferences.Get("TempLoginInfo", string.Empty));
            Preferences.Remove("TempLoginInfo");
            await DisplayAlert("Success", "OTP verified!", "OK");
            //await Navigation.PushAsync(new HomePage());
        }
        else
        {
            await DisplayAlert("Error", "Invalid OTP. Please try again.", "OK");
        }
    }
}