using Newtonsoft.Json;
using System.Text.Json;
using Twilio.TwiML.Voice;
using Twilio.Types;

namespace OrderingAssistSystem_StaffApp.Models
{
    public class AuthorizeLogin
    {
        private readonly string _apiUrl = "https://localhost:7183/api/";
        private readonly HttpClient _client;
        private readonly JsonSerializerOptions _serializerOptions;

        public AuthorizeLogin(HttpClient client, JsonSerializerOptions serializerOptions)
        {
            this._client = client;
            this._serializerOptions = serializerOptions;
        }

        public async Task<string> CheckLogin()
        {

            Config config = new Config();

            string loginInfoJson = Preferences.Get("LoginInfo", string.Empty);
            Employee emp = JsonConvert.DeserializeObject<Employee>(loginInfoJson);

            //Preferences.Set("LoginInfo", Preferences.Get("TempLoginInfo", string.Empty));

            if (emp == null)
            {
                return "null";
            }

            try
            {
                var uri = new Uri($"{config.BaseAddress}Employee/Staff/Phone/{emp.Phone}");
                HttpResponseMessage response = await _client.GetAsync(uri);

                if (response.IsSuccessStatusCode)
                {
                    string data = await response.Content.ReadAsStringAsync();
                    emp = JsonConvert.DeserializeObject<Employee>(data);
                    if (emp.Phone != null)
                    {
                        if (emp.Role.RoleName.ToLower() == "staff")
                        {
                            return "staff";
                        }
                        else if (emp.Role.RoleName.ToLower() == "bartender")
                        {
                            return "bartender";
                        }
                    }
                    if (emp.Owner.SubscribeEndDate < DateTime.Now.AddDays(7))
                    {
                        return "employee expired";
                    }
                }
                else { return "null"; }
            }
            catch (Exception ex)
            {
                
            }

            return "null";
        }
    }
}
