namespace LttfRating;

public record SetUpdateMessageCommand(Message Message) : IRequest;

public class SetUpdateMessageHandler(
    ILogger<SetUpdateMessageHandler> logger,
    IMediator mediator,
    IOptions<ApiConfig> config,
    ITelegramBotClient botClient)
    : IRequestHandler<SetUpdateMessageCommand>
{
    private readonly ApiConfig _config = config.Value;

    public async Task Handle(SetUpdateMessageCommand request, CancellationToken token)
    {
        var setValues = ParseMatch(request.Message.Text ?? "");
        if (setValues == null)
        {
            logger.LogTrace("Сообщение не соответствует паттерну {Pattern}", nameof(ParseMatch));
            return;
        }

        var sender = request.Message.From?.Username ?? "";
        var isAdmin = _config.Administrators.Contains(sender);
        var isGamer = setValues.Select(p => p.Login).Contains(sender);
        if (!isAdmin && !isGamer)
        {
            await botClient.SendMessage(
                chatId: request.Message.Chat.Id,
                text: $"""
                       ⚠️ <b>Ошибка отправки результата</b>

                       @{sender}, результаты матча может отправить только участник партии.

                       Если это ошибка — обратитесь к администратору:
                       {string.Join(", ", _config.Administrators.Select(admin => $"<a href=\"tg://user?id={admin}\">@{admin}</a>"))}
                       """,
                parseMode: ParseMode.Html,
                cancellationToken: token);
            
            return;
        }

        await mediator.Send(new AddGamerCommand(setValues[0].Login), token);
        await mediator.Send(new AddGamerCommand(setValues[1].Login), token);

        var matchId = await mediator.Send(new GetOrAddMatchCommand(
            setValues[0].Login, setValues[1].Login), token);
        await mediator.Send(new AddSetCommand(matchId, setValues), token);
        await mediator.Send(new SendResultMessageCommand(request.Message.Chat.Id, matchId), token);
    }

    private static SetValue[]? ParseMatch(string text)
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

        result = result
            .OrderByDescending(p => p.Points)
            .ToArray();
        
        return ValueValidation(result) ? result : null;
    }

    private static bool ValueValidation(SetValue[] setValue)
    {
        if (setValue[0].Login == setValue[1].Login)
            return false;
        
        if (setValue[0].Points < 11)
            return false;
        
        if (setValue[0].Points - setValue[1].Points < 2)
            return false;

        return true;
    }
}