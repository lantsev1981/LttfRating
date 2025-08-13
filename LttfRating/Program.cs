using Serilog;

namespace LttfRating;

class Program
{
    static async Task Main()
    {
        var envName = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Development";
        Console.WriteLine($"ASPNETCORE_ENVIRONMENT={envName}");
        Console.WriteLine($"Environment.UserName={Environment.UserName}");

        var configuration = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())      
            .AddJsonFile(Path.Combine("config", "appsettings.json"), optional: false, reloadOnChange: true)
            .AddJsonFile(Path.Combine("config", $"appsettings.{envName}.json"), optional: true, reloadOnChange: true)
            .AddJsonFile(Path.Combine("config", $"appsettings.{Environment.UserName}.json"), optional: true, reloadOnChange: true)
            .AddEnvironmentVariables()
            .Build();
        
        Log.Logger = new LoggerConfiguration()
            .ReadFrom.Configuration(configuration)
            .Enrich.FromLogContext()
            .Enrich.WithProperty("Application", "LttfRating")
            .Enrich.WithProperty("Environment", envName)
            .CreateLogger();
        
        var services = new ServiceCollection();
        
        services
            .Configure<ApiConfig>(configuration.GetSection("ApiConfig"))
            .AddLogging(loggingBuilder => 
                loggingBuilder.AddSerilog(dispose: true))
            .AddMediatR(cfg => cfg.RegisterServicesFromAssembly(typeof(Program).Assembly))
            .Decorate(typeof(IRequestHandler<>), typeof(RequestHandlerLogger<>))
            .Decorate(typeof(IRequestHandler<,>), typeof(RequestHandlerLogger<,>))
            // .Decorate(typeof(INotificationHandler<>), typeof(NotificationHandlerLogger<>))
            .AddSingleton<ITelegramBotClient>(_ => new TelegramBotClient(configuration["BotToken"]
                ?? throw new InvalidOperationException("BotToken is missing")))
            .AddSingleton<UpdateHandler>()
            .AddSingleton<ErrorHandler>()
            .AddSingleton<IBotService, BotService>()
            .AddTransient<IDomainStore<Gamer>, GamerStore>()
            .AddTransient<IDomainStore<Match>, MatchStore>()
            .AddDbContext<AppDbContext>(options =>
                options.UseNpgsql(
                    configuration.GetConnectionString("AppUserConnectionString"),
                    npgsqlOptions =>
                    {
                        npgsqlOptions.EnableRetryOnFailure(); // опционально: повтор при временных ошибках
                        npgsqlOptions.CommandTimeout(30); // таймаут команд
                    }
                ));

        var serviceProvider = services.BuildServiceProvider();

        using var cts = new CancellationTokenSource();
        var logger = serviceProvider.GetRequiredService<ILogger<Program>>();
        
        try
        {
            var botService = serviceProvider.GetRequiredService<IBotService>();
            await botService.StartAsync(cts.Token);

            logger.LogTrace("Нажмите Ctrl+C для остановки.");
            await Task.Delay(Timeout.Infinite, cts.Token);
        }
        catch (Exception ex)
        {
            logger.LogCritical(ex, ex.Message);
        }
        finally
        {
            if (serviceProvider is IDisposable disposable)
                disposable.Dispose();
        }
    }
}