namespace DbMigration;

public class MatchesConfiguration: IEntityTypeConfiguration<Match>
{
    public void Configure(EntityTypeBuilder<Match> builder)
    {
        builder
            .HasKey(p => p.Id);

        builder
            .Property(p => p.Id)
            .ValueGeneratedOnAdd();

        builder
            .HasMany(p => p.Sets)
            .WithOne(p => p.Match)
            .OnDelete(DeleteBehavior.Cascade);

        builder
            .HasIndex(p => p.IsPending);
    }
}