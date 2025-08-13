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
                    if (update.Message is null)
                        break;

                    if (!await HandleAsync(update.Message, token))
                        logger.LogTrace("Не соответствует паттерну {Pattern}", nameof(ParseMatch));
                    
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

    private async Task<bool> HandleAsync(Message message, CancellationToken token)
    {
        if (message.Text is null)
            return false;

        logger.LogTrace("Пришло сообщение: {Text} от @{Username}", message.Text, message.From?.Username);

        var setValues = ParseMatch(message.Text);
        if (setValues == null)
            return false;

        await mediator.Send(new AddGamerCommand(setValues[0].Login), token);
        await mediator.Send(new AddGamerCommand(setValues[1].Login), token);

        var matchId = await mediator.Send(new GetOrAddMatchCommand(
            setValues[0].Login, setValues[1].Login), token);
        await mediator.Send(new AddSetCommand(matchId, setValues), token);
        await mediator.Send(new SendMessageCommand(message.Chat.Id, matchId), token);

        return true;
    }

    static SetValue[]? ParseMatch(string text)
    {
        var match = Regex.Match(text, @"@(\w+)\s+@(\w+)\s+(\d+)\s+(\d+)");

        if (!match.Success)
            return null;

        if (!byte.TryParse(match.Groups[3].Value, out var points1))
            return null;

        if (!byte.TryParse(match.Groups[4].Value, out var points2))
            return null;

        SetValue[] result =
        [
            new SetValue(match.Groups[1].Value, points1),
            new SetValue(match.Groups[2].Value, points2)
        ];

        return result
            .OrderByDescending(p => p.Points)
            .ToArray();
    }
}