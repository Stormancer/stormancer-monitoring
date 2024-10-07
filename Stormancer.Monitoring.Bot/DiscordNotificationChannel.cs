using Discord;
using Discord.WebSocket;
using Microsoft.Extensions.Options;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace Stormancer.Monitoring.Bot
{


    internal class DiscordNotificationChannel : INotificationChannel
    {
        private readonly DiscordSocketClient _discord = new DiscordSocketClient();
        private readonly ConnectivityTestsRepository states;
        private readonly ILogger<DiscordNotificationChannel> logger;
        private readonly IOptions<BotConfigurationSection> options;


        public DiscordNotificationChannel(ConnectivityTestsRepository states, ILogger<DiscordNotificationChannel> logger, IOptions<BotConfigurationSection> options)
        {
            this.states = states;
            this.logger = logger;
            this.options = options;
        }



        public async Task RunAsync(CancellationToken cancellationToken)
        {

            var config = options.Value.Discord;
            if (config == null)
            {

                return;
            }
            await _discord.LoginAsync(Discord.TokenType.Bot, config.Token);


            await _discord.StartAsync();
            TaskCompletionSource tcs = new TaskCompletionSource();
            _discord.Ready += () =>
            {
                tcs.SetResult();
                return Task.CompletedTask;
            };
            _discord.SlashCommandExecuted += async (SocketSlashCommand command) =>
            {
                logger.Log(LogLevel.Information, "Received slash command {command}",command.CommandName);
                switch (command.Data.Name)
                {
                    case "app-status":
                        var appId = (string)command.Data.Options.First().Value;
                        var app = states.GetOrAddState(appId);
                        await command.RespondAsync($"`{appId}` is `{app.ServiceStatus}` {GetEmote(app)}. The last time I connected successfully was {(DateTime.UtcNow - app.LastTestSuccessOn).TotalSeconds:F}s ago, it took `{app.LastTestDuration.TotalSeconds:F}s`.");
                        break;
                }
                command.Data.Options.First();
                //await command.RespondAsync($"I'm connecting to `{appId}`. Last time was {lastSuccess}UTC it took me {lastElapsed}");
            };
            _discord.AutocompleteExecuted += _discord_AutocompleteExecuted;
            await tcs.Task.ConfigureAwait(false);

            await SyncCommands();

        }

        private async Task _discord_AutocompleteExecuted(SocketAutocompleteInteraction arg)
        {
            logger.Log(LogLevel.Information, "Received auto complete request for command {command}", arg.Data.CommandName);
            switch (arg.Data.CommandName)
            {
                case "app-status":
                    switch (arg.Data.Current.Name)
                    {
                        case "app":

                            await arg.RespondAsync(options.Value.Applications.Keys.Where(appId => appId.StartsWith((string)arg.Data.Current.Value)).Select(appId => new AutocompleteResult(appId, appId)));
                            break;
                    }
                    break;
            }

        }

        async Task SyncCommands()
        {
            try
            {
                var cmds = await _discord.GetGlobalApplicationCommandsAsync();

                foreach (var currentCmd in cmds)
                {
                    if (currentCmd.Name != "app-status")
                    {
                        await currentCmd.DeleteAsync();
                    }
                }

                if (!cmds.Any(c => c.Name == "app-status"))
                {
                    var cmd = new SlashCommandBuilder();
                    cmd
                        .WithName("app-status")
                        .WithDescription("Returns info about the last connection check of the monitoring bot.")
                        .AddOption("app", ApplicationCommandOptionType.String, "target application.", true, false, true);

                    var result = await _discord.CreateGlobalApplicationCommandAsync(cmd.Build());
                }
            }
            catch (Exception ex)
            {
                logger.Log(LogLevel.Error, ex, "Failed to synchronize bot commands");
            }
            logger.Log(LogLevel.Information, "Synchronized bot commands");
        }

        public async Task OnStateChange(NotificationContext ctx)
        {
            if (options.Value.Applications.TryGetValue(ctx.AppId, out var app) && app.DiscordChannel != 0)
            {
                var channel = _discord.GetChannel(app.DiscordChannel) as SocketTextChannel;
                if (channel != null)
                {
                    var state = states.GetOrAddState(ctx.AppId);
                    //await channel.SendMessageAsync($"{string.Join(' ', app.DiscordRolesToMention.Select(r => $"<@&{r}>"))} `{ctx.AppId}` is in `{state.ServiceStatus}`");
                    await channel.SendMessageAsync($"`{DateTime.UtcNow:G}`: `{ctx.AppId}` is `{state.ServiceStatus}` {GetEmote(state)}");
                }
            }

        }

        private string GetEmote(ConnectivityTestState state) => state switch
        {
            { ServiceStatus: ServiceStatus.Up } => ":slight_smile:",
            { ServiceStatus: ServiceStatus.Degraded } => ":slight_frown:",
            { ServiceStatus: ServiceStatus.Down }=>":rage:",
            _=>""
        };
    }
}
