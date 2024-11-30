using OrderingAssistSystem_StaffApp.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OrderingAssistSystem_StaffApp.Services
{
    public interface IPushDemoNotificationActionService : INotificationActionService
    {
        event EventHandler<OasStaffAppAction> ActionTriggered;
    }
}
