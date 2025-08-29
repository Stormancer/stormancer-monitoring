using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Stormancer.Monitoring.Bot
{
    public class BotConfigurationSection
    {
        public Dictionary<string, ApplicationConfigurationSection> Applications { get; set; } = new Dictionary<string, ApplicationConfigurationSection>();
        public int DownReminderIntervalSeconds { get; set; } = 60 * 10; // 10 minutes
        public DiscordConfigurationSection? Discord { get; set; }
        public SMSConfigurationSection? SMS { get; set; }
    }

    public class ApplicationConfigurationSection
    {
        public required string Endpoint { get; set; }
        public required string AccountId { get; set; }
        public required string AppId { get; set; }

        public required string ClientVersion { get; set; }

        public ulong DiscordChannel { get; set; }
        public IEnumerable<ulong> DiscordRolesToMention { get; set; } = Enumerable.Empty<ulong>();

        public IEnumerable<string> SMSPhoneNumbers { get; set; } = Enumerable.Empty<string>();

        public int ElevatedAlertThresholdSeconds { get; set; } = 60 * 30; // 30 minutes
        public int ElevatedAlertReminderIntervalSeconds { get; set; } = 60 * 30; // 30 minutes

        public bool JoinParty { get; set; } = true;

    }

    public class DiscordConfigurationSection
    {
        public required string Token { get; set; }
      
    }

    public class SMSConfigurationSection
    {
        public required string Token { get; set; }

   
    }

    
}
