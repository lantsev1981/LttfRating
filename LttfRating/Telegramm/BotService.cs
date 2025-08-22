namespace LttfRating;

public class BotService(
    ITelegramBotClient botClient,
    UpdateHandler updateHandler,
    ErrorHandler errorHandler,
    ILogger<BotService> logger)
    : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            var me = await botClient.GetMe(stoppingToken);

            // Устанавливаем команды
            await botClient.SetMyCommands(
                new[]
                {
                    new BotCommand { Command = "start", Description = "Запуск бота, список команд и помощь" },
                    new BotCommand { Command = "rating", Description = "Посмотреть свой рейтинг" }
                },
                new BotCommandScopeDefault(),
                cancellationToken: stoppingToken);

            var receiverOptions = new ReceiverOptions
            {
                AllowedUpdates = [UpdateType.Message, UpdateType.MessageReaction, UpdateType.CallbackQuery],
                DropPendingUpdates = false,
            };

            // Запускаем приём обновлений
            botClient.StartReceiving(
                updateHandler: updateHandler.HandleAsync,
                errorHandler: errorHandler.HandleAsync,
                receiverOptions: receiverOptions,
                cancellationToken: stoppingToken);

            logger.LogTrace("{FirstName} запущен!", me.FirstName);

            while (!stoppingToken.IsCancellationRequested)
                await Task.Delay(1000, stoppingToken);
        }
        catch (OperationCanceledException)
        {
            // Корректная остановка
        }
        catch (Exception ex)
        {
            logger.LogCritical(ex, "Критическая ошибка при запуске бота.");
            throw;
        }
    }
}