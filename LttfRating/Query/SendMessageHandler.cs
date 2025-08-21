namespace LttfRating;

public record SendMessageQuery(
    long ChatId,
    string MessageText,
    string? FileName = null,
    InlineKeyboardMarkup? Buttons = null)
    : IRequest;

public class SendMessageHandler(
    ITelegramBotClient botClient,
    ILogger<SendMessageHandler> logger)
    : IRequestHandler<SendMessageQuery>
{
    public async Task Handle(SendMessageQuery request, CancellationToken token)
    {
        try
        {
            if (request.FileName == null)
            {
                await botClient.SendMessage(
                    chatId: request.ChatId,
                    parseMode: ParseMode.Html,
                    text: request.MessageText,
                    replyMarkup: request.Buttons,
                    cancellationToken: token);
            }
            else
            {
                await using var stream = File.OpenRead(request.FileName);
                await botClient.SendPhoto(
                    chatId: request.ChatId,
                    photo: new InputFileStream(stream),
                    parseMode: ParseMode.Html,
                    caption: request.MessageText,
                    replyMarkup: request.Buttons,
                    cancellationToken: token);
            }
        }
        catch (Exception e)
        {
            logger.LogError(e, "Не удалось отправить сообщение");
        }
    }
}