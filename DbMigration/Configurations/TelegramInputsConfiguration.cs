namespace DbMigration;

public class TelegramInputsConfiguration : IEntityTypeConfiguration<TelegramInput>
{
    public void Configure(EntityTypeBuilder<TelegramInput> builder)
    {
        builder
            .HasKey(p => p.Id);

        // Указываем, что Sender — это owned entity
        builder.OwnsOne(p => p.Sender, sb =>
        {
            sb.Property(s => s.Id).HasColumnName("SenderId");
            sb.Property(s => s.Login).HasColumnName("SenderLogin").HasMaxLength(32);
        });

        builder
            .Property(p => p.UpdateType)
            .HasMaxLength(24);
    }
}