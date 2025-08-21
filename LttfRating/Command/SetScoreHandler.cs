namespace LttfRating;

public record SetScoreCommand(TelegramInput Input) : IRequest;

public class SetScoreHandler(
    ILogger<SetScoreHandler> logger,
    IMediator mediator,
    IOptions<ApiConfig> config)
    : IRequestHandler<SetScoreCommand>
{
    private readonly ApiConfig _config = config.Value;

    public async Task Handle(SetScoreCommand request, CancellationToken token)
    {
        var sender = request.Input.Sender.Login;

        var parseValue = ParseMatch(request.Input.Text, sender);
        ValueValidation(parseValue.SetScore);

        var isAdmin = _config.Administrators.Contains(sender);
        var isGamer = parseValue.SetScore.Select(p => p.Login).Contains(sender);
        if (!isAdmin && !isGamer)
            throw new ValidationException("Результаты матча может отправить только участник партии");

        await mediator.Send(new AddGamerCommand(parseValue.SetScore[0].Login), token);
        await mediator.Send(new AddGamerCommand(parseValue.SetScore[1].Login), token);

        var matchId = await mediator.Send(new GetOrAddMatchCommand(
            parseValue.SetScore[0].Login, parseValue.SetScore[1].Login, parseValue.SetWonCount), token);
        await mediator.Send(new AddSetCommand(request.Input, matchId, parseValue), token);
        await mediator.Send(new SendResultQuery(request.Input.ChatId, matchId), token);
    }

    private (SetScore[] SetScore, byte SetWonCount) ParseMatch(string text, string senderLogin)
    {
        var match = UpdateExtensions.SetScoreRegex.Match(text);
        if (match.Success)
        {
            SetScore[] result =
            [
                new SetScore(
                    match.Groups["User1"].Success ? match.Groups["User1"].Value.Trim('@').Trim() : senderLogin,
                    byte.Parse(match.Groups["Points1"].Value)),
                new SetScore(
                    match.Groups["User2"].Value.Trim('@').Trim(),
                    byte.Parse(match.Groups["Points2"].Value))
            ];
            
            return (result
                .OrderByDescending(p => p.Points)
                .ToArray(),match.Groups["Length"].Success ? byte.Parse(match.Groups["Length"].Value) : (byte)3);
        }
        
        throw new ValidationException("Неудалось разобрать сообщение");
    }

    private void ValueValidation(SetScore[] setValue)
    {
        var winner = setValue[0];
        var loser = setValue[1];

        if (winner.Login == loser.Login)
            throw new ValidationException("Указан одинаковый логин для обоих игроков");

        if (winner.Points < 11)
            throw new ValidationException("Результат победителя должен быть не менее 11 очков");

        var pointDiff = winner.Points - loser.Points;

        if (winner.Points == 11 && pointDiff < 2)
            throw new ValidationException("Разница в очках должна быть не менее 2");

        if (winner.Points > 11 && pointDiff != 2)
            throw new ValidationException("При игре на больше/меньше разница должна быть в 2 очка");
    }
}