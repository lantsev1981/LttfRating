namespace LttfRating;

public class ErrorHandler(
    ILogger<ErrorHandler> logger)
{
    public Task HandleAsync(ITelegramBotClient botClient, Exception error, CancellationToken token)
    {
        var messageText = error switch
        {
            ApiRequestException exp =>
                $"""
                 🤬 Telegram API Error [{exp.HttpStatusCode}]
                 
                   <code>{string.Join(", ", error.GetAllMessages())}</code>
                 """,
            _ =>
                $"""
                 🤬 Ошибка
                 
                   <code>{string.Join(", ", error.GetAllMessages())}</code>
                 """,
        };

        logger.LogError(error, "{ErrorMessage}", messageText);

        return Task.CompletedTask;
    }
}