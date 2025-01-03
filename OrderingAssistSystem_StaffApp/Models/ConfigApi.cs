using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OrderingAssistSystem_StaffApp.Models
{
    public class ConfigApi
    {
        //public readonly string _apiUrl = "https://10.0.2.2:7183/api/";
        //public string BaseAddress = DeviceInfo.Platform == DevicePlatform.Android ? "https://10.0.2.2:7183/api/" : "https://localhost:7183/api/";

        /*public readonly string _apiUrl = "https://oas-api-main-cxhua5dqh7dueqhz.southeastasia-01.azurewebsites.net/api/";
        public string BaseAddress = DeviceInfo.Platform == DevicePlatform.Android ? "https://oas-api-main-cxhua5dqh7dueqhz.southeastasia-01.azurewebsites.net/api/" : "https://oas-api-main-cxhua5dqh7dueqhz.southeastasia-01.azurewebsites.net/api/";
*/
        public readonly string _apiUrl = "https://oas-main-api-cwf5hnd9apbhgnhn.southeastasia-01.azurewebsites.net/api/";
        public string BaseAddress = DeviceInfo.Platform == DevicePlatform.Android ? "https://oas-main-api-cwf5hnd9apbhgnhn.southeastasia-01.azurewebsites.net/api/" : "https://oas-main-api-cwf5hnd9apbhgnhn.southeastasia-01.azurewebsites.net/api/";

        public string accId = "AC4f52abeaebacc8995cdd7c274fedf7ab";
        public string accToken = "72c203b036e03395ff66566740745cf6";
    }
}