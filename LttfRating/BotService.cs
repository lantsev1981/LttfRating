namespace LttfRating;

public class BotService(
    ITelegramBotClient botClient,
    UpdateMessageHandler updateHandler,
    ErrorMessageHandler errorHandler,
    ILogger<BotService> logger)
    : IBotService
{
    public async Task StartAsync(CancellationToken token)
    {
        var me = await botClient.GetMe(token);
        logger.LogTrace("{FirstName} запущен!", me.FirstName);
        
        await botClient.SetMyCommands(
            new BotCommand[]
            {
                new() { Command = "start", Description = "Запуск бота, список команд и помощь" },
                new() { Command = "rating", Description = "Посмотреть свой рейтинг" }
            },
            new BotCommandScopeDefault(),
            cancellationToken: token
        );

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