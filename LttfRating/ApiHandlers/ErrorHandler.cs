namespace LttfRating;

public class ErrorHandler(
    ILogger<ErrorHandler> logger)
{
    public Task HandleAsync(ITelegramBotClient botClient, Exception error, CancellationToken token)
    {
        var errorMessage = error switch
        {
            ApiRequestException apiRequestException
                => $"Telegram API Error:\n[{apiRequestException.ErrorCode}]\n{apiRequestException.Message}",
            _ => error.Message
        };

        logger.LogError(error, "[Ошибка] {ErrorMessage}", errorMessage);

        // Можно отправить сообщение админу через бота (опционально)
        // await botClient.SendTextMessageAsync(ADMIN_ID, errorMessage, cancellationToken: token);

        return Task.CompletedTask;
    }
}