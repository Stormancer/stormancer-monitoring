using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Json;
using System.Text;
using System.Threading.Tasks;

namespace Stormancer.Monitoring.Bot
{
    internal class GatewayApiSMSNotificationChannel : INotificationChannel
    {
        private bool _running = false;
        private CancellationToken _cancellationToken = CancellationToken.None;
        private readonly Dictionary<string, CancellationTokenSource> _runningAlerts = new();
        private readonly IOptions<BotConfigurationSection> _options;
        private readonly ILogger<GatewayApiSMSNotificationChannel> _logger;
        private readonly IHttpClientFactory _httpClientFactory;

        public GatewayApiSMSNotificationChannel(IOptions<BotConfigurationSection> options,
            ILogger<GatewayApiSMSNotificationChannel> logger
            ,IHttpClientFactory httpClientFactory)
        {
            _options = options;
            _logger = logger;
            _httpClientFactory = httpClientFactory;
        }


        public async Task RunAsync(CancellationToken cancellationToken)
        {
            if (_running)
            {
                throw new InvalidOperationException("The SMS notification channel is already running.");
            }

            if (_options.Value.SMS == null)
            {
                return;
            }
            if(String.IsNullOrWhiteSpace(_options.Value.SMS.Token))
            {
                throw new InvalidOperationException("The SMS notification channel is misconfigured: missing token.");
            }

            _running = true;
            _cancellationToken = cancellationToken;
            try
            {
                await Task.Delay(Timeout.Infinite, cancellationToken);
            }
            finally
            {
                _running = false;
            }
        }

        public Task OnStateChange(NotificationContext ctx)
        {
            if(!_running)
            {
                return Task.CompletedTask;
            }
            if (ctx.NewState == ServiceStatus.Down)
            {
                if (!_runningAlerts.ContainsKey(ctx.AppId) && _options.Value.Applications[ctx.AppId].SMSPhoneNumbers.Any())
                {
                    var cts = new CancellationTokenSource();
                    _runningAlerts.Add(ctx.AppId, cts);
                    _ = StartAlertsAsync(ctx.AppId, cts.Token);
                }
            }
            else
            {
                if (_runningAlerts.TryGetValue(ctx.AppId, out var cts))
                {
                    cts.Cancel();
                    _runningAlerts.Remove(ctx.AppId);
                }
            }

            return Task.CompletedTask;
        }

        private async Task StartAlertsAsync(string appId, CancellationToken token)
        {
            try
            {
                await Task.Delay(TimeSpan.FromSeconds(_options.Value.Applications[appId].ElevatedAlertThresholdSeconds), token);
                while (!token.IsCancellationRequested)
                {
                    await SendAlertAsync(appId);
                    await Task.Delay(TimeSpan.FromSeconds(_options.Value.Applications[appId].ElevatedAlertReminderIntervalSeconds), token);
                }
            }
            catch (OperationCanceledException opCancelledException) when (opCancelledException.CancellationToken == token)
            {
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occured while sending SMS alerts for app {appId}", appId);
            }
        }

        private async Task SendAlertAsync(string appId)
        {
            var alertStart = DateTime.UtcNow;
            var token = _options.Value.SMS!.Token;
            var recipients = _options.Value.Applications[appId].SMSPhoneNumbers;

            using var httpClient = _httpClientFactory.CreateClient();

            var httpRequest = new HttpRequestMessage(HttpMethod.Post, "https://api.gatewayapi.com/rest/sms");
            httpRequest.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Token", token);
            httpRequest.Headers.Accept.Add(new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/json"));
            httpRequest.Content = JsonContent.Create(new
            {
                sender = "Stormancer",
                message = $"Application {appId} is DOWN since {alertStart:u}.",
                recipients = recipients.Select(r => new { msisdn = r }).ToArray()
            });

            var response = await httpClient.SendAsync(httpRequest);
            response.EnsureSuccessStatusCode();
        }
    }
}
