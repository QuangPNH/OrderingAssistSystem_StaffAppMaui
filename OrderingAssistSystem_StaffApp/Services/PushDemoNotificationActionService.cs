using OrderingAssistSystem_StaffApp.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OrderingAssistSystem_StaffApp.Services
{

    public class PushDemoNotificationActionService : IPushDemoNotificationActionService
    {
        readonly Dictionary<string, OasStaffAppAction> _actionMappings = new Dictionary<string, OasStaffAppAction>
    {
        { "action_a", OasStaffAppAction.ActionA },
        { "action_b", OasStaffAppAction.ActionB }
    };

        public event EventHandler<OasStaffAppAction> ActionTriggered = delegate { };

        public void TriggerAction(string action)
        {
            if (!_actionMappings.TryGetValue(action, out var pushDemoAction))
                return;

            List<Exception> exceptions = new List<Exception>();

            foreach (var handler in ActionTriggered?.GetInvocationList())
            {
                try
                {
                    handler.DynamicInvoke(this, pushDemoAction);
                }
                catch (Exception ex)
                {
                    exceptions.Add(ex);
                }
            }

            if (exceptions.Any())
                throw new AggregateException(exceptions);
        }
    }

}
