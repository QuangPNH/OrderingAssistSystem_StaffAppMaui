using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OrderingAssistSystem_StaffApp.Models
{
    public class Config
    {
        public readonly string _apiUrl = "https://10.0.2.2:7183/api/";
        public string BaseAddress = DeviceInfo.Platform == DevicePlatform.Android ? "https://10.0.2.2:7183/api/" : "https://localhost:7183/api/";
    }
}