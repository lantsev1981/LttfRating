namespace LttfRating;

public record SendMessageQuery(
    long ChatId,
    string MessageText,
    int? MessageId = null,
    string? FileName = null,
    bool DelMessage = false,
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
            if (request.DelMessage)
            {
                await botClient.DeleteMessage(request.ChatId, request.MessageId!.Value, token);
                return;
            }
            
            if (request.FileName != null)
            {
                await using var stream = File.OpenRead(request.FileName);
                await botClient.SendPhoto(
                    chatId: request.ChatId,
                    photo: new InputFileStream(stream),
                    parseMode: ParseMode.Html,
                    caption: request.MessageText,
                    replyMarkup: request.Buttons,
                    cancellationToken: token);

                return;
            }

            if (request.MessageId.HasValue && request.MessageText.IsEmoji())
            {
                await botClient.SetMessageReaction(
                    chatId: request.ChatId,
                    messageId: request.MessageId!.Value,
                    reaction: new[] { new ReactionTypeEmoji { Emoji = request.MessageText } },
                    cancellationToken: token);

                return;
            }

            await botClient.SendMessage(
                chatId: request.ChatId,
                parseMode: ParseMode.Html,
                text: request.MessageText,
                replyMarkup: request.Buttons,
                cancellationToken: token);
        }
        catch (Exception e)
        {
            logger.LogError(e, "Не удалось отправить сообщение: {ChatId}, {Text}",
                request.ChatId, request.MessageText);
        }
    }
}