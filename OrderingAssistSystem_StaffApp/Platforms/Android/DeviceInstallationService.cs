﻿using Android.Gms.Common;
using OrderingAssistSystem_StaffApp.Models;
using OrderingAssistSystem_StaffApp.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static Android.Provider.Settings;

namespace OrderingAssistSystem_StaffApp.Platforms.Android
{

    public class DeviceInstallationService : IDeviceInstallationService
    {
        public string Token { get; set; }

        public bool NotificationsSupported
            => GoogleApiAvailability.Instance.IsGooglePlayServicesAvailable(Platform.AppContext) == ConnectionResult.Success;

        public string GetDeviceId()
            => Secure.GetString(Platform.AppContext.ContentResolver, Secure.AndroidId);

        public DeviceInstallation GetDeviceInstallation(params string[] tags)
        {
            if (!NotificationsSupported)
                throw new Exception(GetPlayServicesError());

            if (string.IsNullOrWhiteSpace(Token))
                throw new Exception("Unable to resolve token for FCMv1.");

            var installation = new DeviceInstallation
            {
                InstallationId = GetDeviceId(),
                Platform = "fcmv1",
                PushChannel = Token
            };

            installation.Tags.AddRange(tags);

            return installation;
        }

        string GetPlayServicesError()
        {
            int resultCode = GoogleApiAvailability.Instance.IsGooglePlayServicesAvailable(Platform.AppContext);

            if (resultCode != ConnectionResult.Success)
                return GoogleApiAvailability.Instance.IsUserResolvableError(resultCode) ?
                           GoogleApiAvailability.Instance.GetErrorString(resultCode) :
                           "This device isn't supported.";

            return "An error occurred preventing the use of push notifications.";
        }
    }
}
