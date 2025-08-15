namespace LttfRating;

public class BotService(
    ITelegramBotClient botClient,
    UpdateHandler updateHandler,
    ErrorHandler errorHandler,
    ILogger<BotService> logger)
    : IBotService
{
    public async Task StartAsync(CancellationToken token)
    {
        var me = await botClient.GetMe(token);
        logger.LogTrace("{FirstName} запущен!", me.FirstName);

        var receiverOptions = new ReceiverOptions
        {
            AllowedUpdates = [UpdateType.Message, UpdateType.MessageReaction],
            DropPendingUpdates = false,
        };

        botClient.StartReceiving(
            updateHandler: updateHandler.HandleAsync,
            errorHandler: errorHandler.HandleAsync,
            receiverOptions: receiverOptions,
            cancellationToken: token);

        logger.LogTrace("Бот начал принимать обновления.");
    }
}