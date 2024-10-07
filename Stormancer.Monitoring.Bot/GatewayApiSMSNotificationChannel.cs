using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Stormancer.Monitoring.Bot
{
    internal class GatewayApiSMSNotificationChannel : INotificationChannel
    {
        public Task OnStateChange(NotificationContext ctx)
        {
           return Task.CompletedTask;
        }

        public Task RunAsync(CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }
    }
}
