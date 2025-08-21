namespace DbMigration;

public class AppDbContext : DbContext
{
    public AppDbContext()
    {
    }
        
    public AppDbContext(DbContextOptions<AppDbContext> options)
        : base(options)
    {
    }
        
    public DbSet<Gamer> Gamers { get; init; }
    public DbSet<Match> Matches { get; init; }
    public DbSet<Set> Sets { get; init; }
    public DbSet<TelegramInput> TelegramInputs { get; init; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            if (entityType.IsKeyless)
                continue;

            foreach (var property in entityType.GetProperties())
                if (property.ClrType == typeof(DateTimeOffset) || property.ClrType == typeof(DateTimeOffset?))
                    property.SetValueConverter(UtcDateTimeConverter.Instance);
        }

        modelBuilder.ApplyConfiguration(new GamersConfiguration());
        modelBuilder.ApplyConfiguration(new MatchesConfiguration());
        modelBuilder.ApplyConfiguration(new SetsConfiguration());
        modelBuilder.ApplyConfiguration(new TelegramInputsConfiguration());
    }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        base.OnConfiguring(optionsBuilder);

        if (optionsBuilder.IsConfigured)
            return;
        
        Program.LoadConfiguration();
        optionsBuilder.UseNpgsql(Program.RootUser);
    }
}