﻿using System;
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

        public readonly string _apiUrl = "https://oas-api-main-cxhua5dqh7dueqhz.southeastasia-01.azurewebsites.net/api/";
        public string BaseAddress = DeviceInfo.Platform == DevicePlatform.Android ? "https://oas-api-main-cxhua5dqh7dueqhz.southeastasia-01.azurewebsites.net/api/" : "https://oas-api-main-cxhua5dqh7dueqhz.southeastasia-01.azurewebsites.net/api/";
    }
}