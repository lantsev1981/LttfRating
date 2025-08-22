namespace DbMigration;

public class GamersConfiguration: IEntityTypeConfiguration<Gamer>
{
    public void Configure(EntityTypeBuilder<Gamer> builder)
    {
        builder
            .HasKey(p => p.Login);

        builder.Property(p => p.Login)
            .HasMaxLength(32);
        
        builder
            .HasMany(g => g.Matches)
            .WithMany(m => m.Gamers)
            .UsingEntity<GamerMatch>(
                j => j
                    .HasOne(gm => gm.Match)
                    .WithMany(),
                j => j
                    .HasOne(gm => gm.Gamer)
                    .WithMany(),
                j =>
                {
                    j.HasKey(gm => new { gm.Login, gm.MatchId });
                    j.ToTable("GamerMatch");
                    j.Property(p => p.Login).HasMaxLength(32);
                });

        builder
            .HasIndex(p => p.UserId)
            .IsUnique();

        builder
            .HasIndex(p => p.Rating);
    }
}