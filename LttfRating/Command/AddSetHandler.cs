namespace LttfRating;

public record AddSetCommand(Guid MatchId, SetValue[] SetValues, long ChatId, int MessageId) : IRequest;

public class AddSetHandler(
    IDomainStore<Match> store,
    ILogger<AddSetHandler> logger)
    : IRequestHandler<AddSetCommand>
{
    public async Task Handle(AddSetCommand request, CancellationToken token)
    {
        logger.LogTrace("Добавляем партию: @{Winner} {WonPoint} — {LostPoint} @{Loser}",
            request.SetValues[0].Login,
            request.SetValues[0].Points,
            request.SetValues[1].Points,
            request.SetValues[1].Login);

        var match = await store.GetByKey(request.MatchId, token)
                    ?? throw new NullReferenceException($"Матч {request.MatchId} не найден");

        var set = new Set(
            (byte)(match.Sets.Count + 1),
            request.SetValues[0].Points,
            request.SetValues[1].Points,
            request.SetValues[0].Login,
            request.ChatId,
            request.MessageId);

        match.Sets.Add(set);

        var winner = match.GetLastWinner();
        var loser = match.GetLastLoser();

        match.IsPending = match.Sets.Count(p => p.WinnerLogin == winner.Login) != match.SetWonCount;

        winner.OldRating = winner.Rating;
        loser.OldRating = loser.Rating;
        var points = match.Sets.Sum(p => p.Points);
        var winnerPoints = match.Sets.Sum(p => p.GetPoints(winner.Login));
        var loserPoints = points - winnerPoints;

        if (!match.IsPending)
        {
            logger.LogTrace("Расчитываем рейтинг: @{Winner} {WonPoint} — {LostPoint} @{Loser}",
                winner.Login,
                winnerPoints,
                loserPoints,
                loser.Login
            );

            // рейтинг победителя
            var pointPrize = winnerPoints / (float)(2 * points); // вознагаждение за победные очки (на двоих)
            var opponentPrize = winnerPoints * (loser.OldRating / points); // вознагаждение за силу противника
            var losePointPenalty = loserPoints * (winner.OldRating / points); // штраф за пропущенные очки
            winner.Rating += pointPrize + opponentPrize - losePointPenalty;

            // рейтинг проигравшего
            pointPrize = loserPoints / (float)(2 * points); // вознагаждение за победные очки (на двоих)
            opponentPrize = loserPoints * (winner.OldRating / points); // вознагаждение за силу противника
            losePointPenalty = winnerPoints * (loser.OldRating / points); // штраф за пропущенные очки
            loser.Rating += pointPrize + opponentPrize - losePointPenalty;

            match.IsPending = false;
        }

        await store.UpdateItem(match, token);
    }
}