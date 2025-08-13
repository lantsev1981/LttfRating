namespace DbMigration;

public class SetsConfiguration : IEntityTypeConfiguration<Set>
{
    public void Configure(EntityTypeBuilder<Set> builder)
    {
        builder
            .HasKey(p => new { p.MatchId, p.Num });
    }
}