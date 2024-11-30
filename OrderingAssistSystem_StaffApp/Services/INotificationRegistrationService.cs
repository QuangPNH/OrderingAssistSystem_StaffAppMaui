using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OrderingAssistSystem_StaffApp.Services
{

    public interface INotificationRegistrationService
    {
        Task DeregisterDeviceAsync();
        Task RegisterDeviceAsync(params string[] tags);
        Task RefreshRegistrationAsync();
    }
}
