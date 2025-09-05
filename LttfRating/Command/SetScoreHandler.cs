namespace LttfRating;

public record SetScoreCommand(TelegramInput Input) : IRequest;

public class SetScoreHandler(
    IMediator mediator,
    IOptions<ApiConfig> config)
    : IRequestHandler<SetScoreCommand>
{
    private readonly ApiConfig _config = config.Value;

    public async Task Handle(SetScoreCommand request, CancellationToken token)
    {
        await mediator.Send(new SendMessageQuery(request.Input.ChatId,
            "", MessageId: request.Input.MessageId, DelMessage: true), token);

        var sender = request.Input.Sender.Login;

        var parseValue = ParseMatch(request.Input.Text, sender);
        ValueValidation(parseValue.SetScore);

        var isAdmin = _config.Administrators.Contains(sender);
        var isGamer = parseValue.SetScore.Select(p => p.Login).Contains(sender);
        if (!isAdmin && !isGamer)
            throw new ValidationException("Результаты матча может отправить только участник партии (⚔️)");

        await mediator.Send(new AddGamerCommand(parseValue.SetScore[0].Login), token);
        await mediator.Send(new AddGamerCommand(parseValue.SetScore[1].Login), token);

        var matchId = await mediator.Send(new GetOrAddMatchCommand(
            parseValue.SetScore[0].Login, parseValue.SetScore[1].Login, parseValue.SetWonCount), token);
        var oldRating = await mediator.Send(new AddSetCommand(request.Input, matchId, parseValue), token);
        await mediator.Send(new SendResultQuery(request.Input, matchId, oldRating), token);
    }

    private (SetScore[] SetScore, byte SetWonCount) ParseMatch(string text, string senderLogin)
    {
        var regexMatch = UpdateExtensions.SetScoreRegex.Match(text);
        if (regexMatch.Success)
        {
            SetScore[] result =
            [
                new SetScore(
                    regexMatch.Groups["User1"].Success
                        ? regexMatch.Groups["User1"].Value.Trim('@').Trim()
                        : senderLogin,
                    byte.Parse(regexMatch.Groups["Points1"].Value)),
                new SetScore(
                    regexMatch.Groups["User2"].Value.Trim('@').Trim(),
                    byte.Parse(regexMatch.Groups["Points2"].Value))
            ];

            var length = regexMatch.Groups["Length"].Success ? byte.Parse(regexMatch.Groups["Length"].Value) : (byte)3;
            if (length is < 1 or > 5)
                throw new ValidationException("Длинна матча должна быть от 1 до 5 побед");

            return (result
                .OrderByDescending(p => p.Points)
                .ToArray(), length);
        }

        throw new ValidationException("Не удалось разобрать сообщение");
    }

    private void ValueValidation(SetScore[] setValue)
    {
        var winner = setValue[0];
        var loser = setValue[1];

        if (winner.Login == loser.Login)
            throw new ValidationException("Необходимо указать разных игроков");

        if (winner.Points < 11)
            throw new ValidationException("Результат победителя должен быть не менее 11 очков (●)");

        var pointDiff = winner.Points - loser.Points;

        if (winner.Points == 11 && pointDiff < 2)
            throw new ValidationException("Разница в очках (●) должна быть не менее 2");

        if (winner.Points > 11 && pointDiff != 2)
            throw new ValidationException("При игре на больше/меньше разница должна быть в 2 очка (●)");
    }
}