namespace DbMigration;

public class SetsConfiguration : IEntityTypeConfiguration<Set>
{
    public void Configure(EntityTypeBuilder<Set> builder)
    {
        builder
            .HasKey(p => new { p.MatchId, p.Num });

        builder
            .Property(p => p.WinnerLogin)
            .HasMaxLength(32);

        builder
            .Property(p => p.ChatId)
            .HasDefaultValue(-1);
        
        builder
            .Property(p => p.MessageId)
            .HasDefaultValue(-1);
        
        builder
            .HasIndex(p => p.Date)
            .IsUnique();
        
        builder
            .HasIndex(p => p.WinnerLogin);
    }
}