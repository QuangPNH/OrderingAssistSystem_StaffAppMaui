using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OrderingAssistSystem_StaffApp.Services
{
    public interface INotificationActionService
    {
        void TriggerAction(string action);
    }
}
