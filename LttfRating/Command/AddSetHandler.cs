namespace LttfRating;

public record AddSetCommand(Guid MatchId, SetValue[] SetValues, long ChatId, int MessageId) : IRequest;

public class AddSetHandler(
    IDomainStore<Match> store,
    ILogger<AddSetHandler> logger)
    : IRequestHandler<AddSetCommand>
{
    public async Task Handle(AddSetCommand request, CancellationToken token)
    {
        var winner = request.SetValues[0].Login;
        var loser = request.SetValues[1].Login;

        logger.LogTrace("Добавляем партию: @{Winner} {WonPoint} — {LostPoint} @{Loser}",
            winner,
            request.SetValues[0].Points,
            request.SetValues[1].Points,
            loser);

        var match = await store.GetByKey(request.MatchId, token)
                    ?? throw new NullReferenceException($"Матч {request.MatchId} не найден");

        var set = new Set(
            (byte)(match.Sets.Count + 1),
            request.SetValues[0].Points,
            request.SetValues[1].Points,
            winner,
            request.ChatId,
            request.MessageId);

        match.Sets.Add(set);

        var needCalculate = match.Sets.Count(p => p.WinnerLogin == winner) == match.SetWonCount;

        if (needCalculate)
        {
            logger.LogTrace("Расчитываем рейтинг: @{Winner} — @{Loser}",
                winner, loser);

            match.CalculateRating();
        }

        await store.Update(match, token);
    }
}