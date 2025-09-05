namespace LttfRating;

public record AddSetCommand(TelegramInput Input, Guid MatchId, (SetScore[] SetScore, byte SetWonCount) ParseValue)
    : IRequest<Rating>;

public class AddSetHandler(
    IUnitOfWork store,
    ILogger<AddSetHandler> logger)
    : IRequestHandler<AddSetCommand, Rating>
{
    public async Task<Rating> Handle(AddSetCommand request, CancellationToken token)
    {
        var winner = request.ParseValue.SetScore[0].Login;
        var loser = request.ParseValue.SetScore[1].Login;

        logger.LogTrace("Добавляем партию: @{Winner} {WonPoint} — {LostPoint} @{Loser}",
            winner,
            request.ParseValue.SetScore[0].Points,
            request.ParseValue.SetScore[1].Points,
            loser);

        var match = await store.MatchStore.GetByKey(request.MatchId, token, q => q
                        .Include(p => p.Gamers)
                        .Include(p => p.Sets))
                    ?? throw new NullReferenceException($"Матч {request.MatchId} не найден");

        var set = new Set(
            (byte)(match.Sets.Count + 1),
            request.ParseValue.SetScore[0].Points,
            request.ParseValue.SetScore[1].Points,
            winner,
            request.Input.ChatId,
            request.Input.MessageId);

        match.Sets.Add(set);

        var needCalculate = match.Sets.Count(p => p.WinnerLogin == winner) == match.SetWonCount;
        var oldRating = new Rating { User = match.LastWinner.Rating, Opponent = match.LastLoser.Rating };
        
        if (needCalculate)
        {
            logger.LogTrace("Рассчитываем рейтинг: @{Winner} — @{Loser}",
                winner, loser);

            var startRating = match.ReCalculateRating(oldRating with { });
            match.LastWinner.Rating = startRating.User;
            match.LastLoser.Rating = startRating.Opponent;
            match.IsPending = false;
        }

        await store.Update(token);

        return oldRating;
    }
}