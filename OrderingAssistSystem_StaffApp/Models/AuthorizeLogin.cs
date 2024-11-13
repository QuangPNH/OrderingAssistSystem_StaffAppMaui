using Newtonsoft.Json;

namespace OrderingAssistSystem_StaffApp.Models
{
    public class AuthorizeLogin
    {
        private readonly string _apiUrl = "https://localhost:7183/api/";

        public async Task<string> CheckLogin()
        {
            string loginInfoJson = Preferences.Get("LoginInfo", string.Empty);
            Employee emp = JsonConvert.DeserializeObject<Employee>(loginInfoJson);

            //Preferences.Set("LoginInfo", Preferences.Get("TempLoginInfo", string.Empty));

            if (emp == null)
            {
                return "null";
            }
            using (HttpClient client = new HttpClient())
            {
                try
                {
                    HttpResponseMessage res = await client.GetAsync(_apiUrl + "Employee/Staff/Phone/" + emp.Phone);
                    if (res.IsSuccessStatusCode)
                    {
                        string data = await res.Content.ReadAsStringAsync();
                        emp = JsonConvert.DeserializeObject<Employee>(data);
                        if (emp.Phone != null)
                        {
                            if (emp.Role.RoleName.ToLower() == "staff")
                            {
                                return "staff";
                            }else if (emp.Role.RoleName.ToLower() == "bartender")
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
                catch (HttpRequestException e) { }
            }


            return "null";
        }
    }
}
