namespace LttfRating;

public record AddSetCommand(TelegramApiData Data, Guid MatchId, SetValue[] SetValues) : IRequest;

public class AddSetHandler(
    IUnitOfWork store,
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

        var match = await store.MatchStore.GetByKey(request.MatchId, token, q => q
                        .Include(p => p.Gamers)
                        .Include(p => p.Sets))
                    ?? throw new NullReferenceException($"Матч {request.MatchId} не найден");

        var set = new Set(
            (byte)(match.Sets.Count + 1),
            request.SetValues[0].Points,
            request.SetValues[1].Points,
            winner,
            request.Data.ChatId,
            request.Data.MessageId);

        match.Sets.Add(set);

        var needCalculate = match.Sets.Count(p => p.WinnerLogin == winner) == match.SetWonCount;

        if (needCalculate)
        {
            logger.LogTrace("Расчитываем рейтинг: @{Winner} — @{Loser}",
                winner, loser);

            match.CalculateRating();
        }

        await store.MatchStore.Update(match, token);
    }
}