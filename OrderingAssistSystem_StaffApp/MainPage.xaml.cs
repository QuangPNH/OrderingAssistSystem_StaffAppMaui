
using Twilio;
using Twilio.Rest.Api.V2010.Account;
using Twilio.Types;


namespace OrderingAssistSystem_StaffApp
{
    public partial class MainPage : ContentPage
    {
        private readonly string _apiUrl = "https://localhost:7183/api/";

        public MainPage()
        {
            InitializeComponent();
        }

        private async void OnSendOtpClicked(object sender, EventArgs e)
        {
            var phoneNumber = PhoneNumberEntry.Text;

            if (string.IsNullOrWhiteSpace(phoneNumber))
            {
                await DisplayAlert("Error", "Please enter a phone number.", "OK");
                return;
            }

            string otp = new Random().Next(000000, 999999).ToString();

            otp = "123456"; // For testing only, remove this line in production

            //Save otp to session

            // Send OTP via Twilio
            var accountSid = "ACd5083d30edb839433981a766a0c2e2fd";
            var authToken = "";
            TwilioClient.Init(accountSid, authToken);
            var messageOptions = new CreateMessageOptions(new PhoneNumber("+84388536414"))
            {
                From = new PhoneNumber("+19096555985"),
                Body = "Your OTP is " + otp
            };
            MessageResource.Create(messageOptions);

            if (true)
                {
                    await Navigation.PushAsync(new OTPPage(phoneNumber));
                }
                else
                {
                    await DisplayAlert("Error", "Failed to send OTP. Try again.", "OK");
                }

        }
    }

}
