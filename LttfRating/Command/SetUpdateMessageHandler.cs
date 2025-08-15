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
        var sender = request.Message.From!.Username ?? "";

        var setValues = ParseMatch(request.Message.Text!, sender);
        if (setValues == null || !ValueValidation(setValues))
        {
            logger.LogTrace("Сообщение не соответствует паттерну {Pattern}", nameof(ParseMatch));
            return;
        }

        var isAdmin = _config.Administrators.Contains(sender);
        var isGamer = setValues.Select(p => p.Login).Contains(sender);
        if (!isAdmin && !isGamer)
        {
            await mediator.Send(new SendMessageCommand(request.Message.From.Id,
                $"""
                 ⚠️ <b>Ошибка отправки результата</b>

                 @{sender}, результаты матча может отправить только участник партии.

                 Если это ошибка — обратитесь к администратору:
                 {string.Join(", ", _config.Administrators.Select(admin => $"<a href=\"tg://user?id={admin}\">@{admin}</a>"))}
                 """), token);

            return;
        }

        await mediator.Send(new AddGamerCommand(setValues[0].Login), token);
        await mediator.Send(new AddGamerCommand(setValues[1].Login), token);

        var matchId = await mediator.Send(new GetOrAddMatchCommand(
            setValues[0].Login, setValues[1].Login), token);
        await mediator.Send(new AddSetCommand(matchId, setValues, request.Message.Chat.Id, request.Message.MessageId), token);
        await mediator.Send(new SendResultMessageCommand(request.Message.Chat.Id, matchId), token);
    }

    private static SetValue[]? ParseMatch(string text, string senderLogin)
    {
        SetValue[]? result = null;

        // Формат 1: @игрок1 @игрок2 11 3
        var fullMatch = Regex.Match(text, @"^@(\w+)\s+@(\w+)\s+(\d+)\s+(\d+)$");
        if (fullMatch.Success)
        {
            if (!byte.TryParse(fullMatch.Groups[3].Value, out byte points1) ||
                !byte.TryParse(fullMatch.Groups[4].Value, out byte points2))
                return null;

            result =
            [
                new SetValue(fullMatch.Groups[1].Value, points1),
                new SetValue(fullMatch.Groups[2].Value, points2)
            ];
        }

        // Формат 2: @игрок2 11 3
        // где игрок1 = senderLogin
        var shortMatch = Regex.Match(text, @"^@(\w+)\s+(\d+)\s+(\d+)$");
        if (shortMatch.Success)
        {
            if (!byte.TryParse(shortMatch.Groups[2].Value, out byte points1) ||
                !byte.TryParse(shortMatch.Groups[3].Value, out byte points2))
                return null;

            result =
            [
                new SetValue(senderLogin, points1), // отправитель
                new SetValue(shortMatch.Groups[1].Value, points2)
            ];
        }

        result = result?
            .OrderByDescending(p => p.Points)
            .ToArray();

        return result;
    }

    private static bool ValueValidation(SetValue[] setValue)
    {
        // TODO как проверить существует ли пользователь в телеге?

        if (setValue.Any(p => string.IsNullOrWhiteSpace(p.Login)))
            return false;

        if (setValue[0].Login == setValue[1].Login)
            return false;

        if (setValue[0].Points < 11)
            return false;

        if (setValue[0].Points - setValue[1].Points < 2)
            return false;

        return true;
    }
}