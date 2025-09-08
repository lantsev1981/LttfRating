namespace LttfRating;

class Program
{
    static async Task Main(string[] args)
    {
        var envName = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Development";
        Console.WriteLine($"ASPNETCORE_ENVIRONMENT={envName}");
        Console.WriteLine($"Environment.UserName={Environment.UserName}");
        
        var host = Host.CreateDefaultBuilder(args)
            .ConfigureAppConfiguration((hostContext, config) =>
            {
                var env = hostContext.HostingEnvironment;
                config
                    .SetBasePath(Directory.GetCurrentDirectory())
                    .AddJsonFile(Path.Combine("config", "appsettings.json"), optional: false, reloadOnChange: true)
                    .AddJsonFile(Path.Combine("config", $"appsettings.{envName}.json"), optional: true, reloadOnChange: true)
                    .AddJsonFile(Path.Combine("config", $"appsettings.{Environment.UserName}.json"), optional: true, reloadOnChange: true)
                    .AddEnvironmentVariables();
            })
            .ConfigureServices((hostContext, services) =>
            {
                // Доступ к конфигурации
                var configuration = hostContext.Configuration;

                services
                    .Configure<ApiConfig>(configuration.GetSection("ApiConfig"))
                    .AddLogging(loggingBuilder => loggingBuilder.AddSerilog(dispose: true))
                    .AddMediatR(cfg => cfg.RegisterServicesFromAssembly(typeof(Program).Assembly))
                    .Decorate(typeof(IRequestHandler<>), typeof(RequestHandlerLogger<>))
                    .Decorate(typeof(IRequestHandler<,>), typeof(RequestHandlerLogger<,>))
                    // .Decorate(typeof(INotificationHandler<>), typeof(NotificationHandlerLogger<>))
                    .AddSingleton<ITelegramBotClient>(_ => new TelegramBotClient(configuration["BotToken"]
                        ?? throw new InvalidOperationException("BotToken is missing")))
                    .AddSingleton<UpdateHandler>()
                    .AddSingleton<ErrorHandler>()
                    .AddScoped<IGamerStore, GamerStore>()
                    .AddScoped<IDomainStore<Gamer>, GamerStore>()
                    .AddScoped<IDomainStore<Match>, MatchStore>()
                    .AddScoped<IDomainStore<Set>, SetStore>()
                    .AddScoped<IDomainStore<TelegramInput>, TelegramInputStore>()
                    .AddScoped<IUnitOfWork, UnitOfWork>()
                    .AddDbContext<AppDbContext>(options =>
                        options.UseNpgsql(
                            configuration.GetConnectionString("AppUserConnectionString"),
                            npgsqlOptions =>
                            {
                                npgsqlOptions.EnableRetryOnFailure(); // повтор при временных ошибках
                                npgsqlOptions.CommandTimeout(30); // таймаут команд
                            }
                        ))
                    .AddHostedService<TelegramInputBackgroundService>()
                    .AddHostedService<SendDayStatisticsBackgroundService>()
                    .AddHostedService<BotService>();
            })
            .UseSerilog((hostContext, loggerConfiguration) =>
            {
                loggerConfiguration
                    .ReadFrom.Configuration(hostContext.Configuration)
                    .Enrich.FromLogContext()
                    .Enrich.WithProperty("Application", "LttfRating")
                    .Enrich.WithProperty("Environment", hostContext.HostingEnvironment.EnvironmentName);
            })
            .Build();

        await host.RunAsync();
    }
}