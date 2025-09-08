namespace Domain;

public record TelegramInput(long ChatId, int MessageId, string Text)
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public required TelegramInputSender Sender { get; init; }
}