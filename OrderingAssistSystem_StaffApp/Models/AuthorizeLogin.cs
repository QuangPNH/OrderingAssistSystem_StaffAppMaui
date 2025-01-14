using Newtonsoft.Json;
using System.Text.Json;
using Twilio.TwiML.Voice;
using Twilio.Types;

namespace OrderingAssistSystem_StaffApp.Models
{
    public class AuthorizeLogin
    {
        private readonly HttpClient _client;

        public AuthorizeLogin(HttpClient client)
        {
            this._client = client;
        }

        public async Task<string> CheckLogin()
        {
            ConfigApi config = new ConfigApi();

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

                if (!response.IsSuccessStatusCode)
                {
					uri = new Uri($"{config.BaseAddress}Employee/Manager/Phone/{emp.Phone}");
				    response = await _client.GetAsync(uri);
					string data = await response.Content.ReadAsStringAsync();
					emp = JsonConvert.DeserializeObject<Employee>(data);
					Preferences.Set("LoginInfo", JsonConvert.SerializeObject(emp));
					if (emp.Phone != null)
					{
						if (emp.Owner.SubscribeEndDate.AddDays(7) < DateTime.Now)
						{
							return "employee expired";
						}
						if (emp.Role.RoleName.ToLower() == "manager")
						{
							return "manager";
						}
					}
					
				}

                if (response.IsSuccessStatusCode)
                {
                    string data = await response.Content.ReadAsStringAsync();
                    emp = JsonConvert.DeserializeObject<Employee>(data);
                    Preferences.Set("LoginInfo", JsonConvert.SerializeObject(emp));
                    if (emp.Phone != null)
                    {
						if (emp.Owner.SubscribeEndDate.AddDays(7) < DateTime.Now)
						{
							return "employee expired";
						}
						if (emp.Role.RoleName.ToLower() == "staff")
                        {
                            return "staff";
                        }
                        else if (emp.Role.RoleName.ToLower() == "bartender")
                        {
                            return "bartender";
                        }
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
