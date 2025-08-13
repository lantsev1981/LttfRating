namespace LttfRating;

public class UpdateHandler(
    ErrorHandler errorHandler,
    IMediator mediator,
    ILogger<UpdateHandler> logger)
{
    public async Task HandleAsync(ITelegramBotClient botClient, Update update, CancellationToken token)
    {
        try
        {
            switch (update.Type)
            {
                case UpdateType.Message:
                {
                    if (update.Message?.Text is null)
                    {
                        logger.LogTrace("Сообщение не содержит текст");
                        break;
                    }

                    if (update.Message.From?.Username is null)
                    {
                        logger.LogTrace("Сообщение от неизвестного пользователя");
                        break;
                    }
                    
                    logger.LogTrace("Пришло сообщение: {Text} от @{Username}",
                        update.Message.Text, update.Message.From?.Username);

                    var commandAndArg = update.Message.Text.Split(' ');
                    switch (commandAndArg[0])
                    {
                        case "/help": 
                            await mediator.Send(new HelpBotCommand(update.Message), token);
                            break;
                        default:
                            await mediator.Send(new SetUpdateMessageCommand(update.Message), token);
                            break;
                    }
                    
                    break;
                }

                // Другие типы можно добавить позже
                default:
                    logger.LogTrace("Необработанный тип сообщения: {Type}", update.Type);
                    break;
            }
        }
        catch (Exception ex)
        {
            await errorHandler.HandleAsync(botClient, ex, token);
        }
    }
}