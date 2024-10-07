using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Stormancer.Monitoring.Bot
{
    public class NotificationContext
    {
        public required string AppId { get; init; }
        public required ServiceStatus NewState { get; init; }
    }

    public enum ServiceStatus
    {
        Up,
        Degraded,
        Down
    }

    public interface INotificationChannel
    {
      
        Task RunAsync(CancellationToken cancellationToken);
        Task OnStateChange(NotificationContext ctx);
    }
}
