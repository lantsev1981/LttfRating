namespace Domain;

public record TelegramInput(string UpdateType, long ChatId, int MessageId, string Text)
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public required TelegramInputSender Sender { get; init; }
}