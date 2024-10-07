using Stormancer.Monitoring.Bot;

var builder = Host.CreateApplicationBuilder(args);
builder.Services.AddHostedService<Worker>();
builder.Services.AddSingleton<ConnectivityTest>();
builder.Services.AddSingleton<INotificationChannel, DiscordNotificationChannel>();
builder.Services.AddSingleton<ConnectivityTestsRepository>();
builder.Services.AddSingleton<INotificationChannel,GatewayApiSMSNotificationChannel>();
builder.Configuration.AddJsonFile("appsettings.json", optional: false, reloadOnChange: false);
builder.Services.Configure<BotConfigurationSection>(builder.Configuration.GetSection("bot"));
var host = builder.Build();
host.Run();
