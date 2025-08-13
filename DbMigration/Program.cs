namespace DbMigration;

public static class Program
{
    public static string RootUser { get; private set; } = null!;
    public static NpgsqlConnectionStringBuilder AppUser { get; private set; } = null!;

    public static void Main(string[] args)
    {
        try
        {
            using (var appDbContext = new AppDbContext())
            {
                appDbContext.Database.Migrate();
                Console.WriteLine($"Миграция успешно применена для контекста: {nameof(appDbContext)}");
            }

            Task.Delay(-1)
                .Wait();
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex);

            Task.Delay(-1)
                .Wait();
        }
    }

    public static void LoadConfiguration()
    {
        var envName = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT");
        if (envName == null)
        {
            Console.WriteLine("Отсутствует ASPNETCORE_ENVIRONMENT");
            envName = "Development";
        }

        Console.WriteLine($"Установлена среда {envName}");

        var config = new ConfigurationBuilder()
            .SetBasePath(AppDomain.CurrentDomain.BaseDirectory)
            .AddJsonFile("appsettings.json", false)
            .AddJsonFile($"appsettings.{envName}.json", true)
            .AddJsonFile($"appsettings.{Environment.UserName}.json", true)
            .AddEnvironmentVariables()
            .Build();

        RootUser = config.GetConnectionString("RootUserConnectionString")
                   ?? throw new ArgumentNullException($"Отсутствует RootUserConnectionString");
        AppUser = new NpgsqlConnectionStringBuilder(
            config.GetConnectionString("AppUserConnectionString")
            ?? throw new ArgumentNullException($"Отсутствует AppUserConnectionString"));
    }
}