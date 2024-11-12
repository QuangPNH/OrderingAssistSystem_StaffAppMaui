using Newtonsoft.Json;
using System.Text;

namespace OrderingAssistSystem_StaffApp;

public partial class OTPPage : ContentPage
{
    private readonly string _apiUrl = "https://localhost:7183/api/";
    private readonly string _phoneNumber;


    public OTPPage(string phoneNumber)
	{
		InitializeComponent();
        _phoneNumber = phoneNumber;
    }
    private async void OnVerifyOtpClicked(object sender, EventArgs e)
    {
        var otp = OtpEntry.Text;
        if (string.IsNullOrWhiteSpace(otp))
        {
            await DisplayAlert("Error", "Please enter the OTP.", "OK");
            return;
        }

        using (HttpClient client = new HttpClient())
        {
            var response = await client.PostAsync($"{_apiUrl}VerifyOtp", new StringContent(JsonConvert.SerializeObject(new { otp }), Encoding.UTF8, "application/json"));
            if (response.IsSuccessStatusCode)
            {
                await DisplayAlert("Success", "OTP verified!", "OK");
                // Navigate to next page or home page after successful login
                //await Navigation.PushAsync(new HomePage());
            }
            else
            {
                await DisplayAlert("Error", "Invalid OTP. Please try again.", "OK");
            }
        }
    }
}