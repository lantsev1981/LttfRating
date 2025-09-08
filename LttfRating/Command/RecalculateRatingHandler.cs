namespace LttfRating;

public record RecalculateRatingCommand(TelegramInput Input) : IRequest;

public class RecalculateRatingHandler(
    IUnitOfWork store,
    IMediator mediator,
    ILogger<RecalculateRatingHandler> logger)
    : IRequestHandler<RecalculateRatingCommand>
{
    public async Task Handle(RecalculateRatingCommand request, CancellationToken token)
    {
        var admin = await store.GamerStore.GetAdminGamerId(token);
        if (admin?.Login != request.Input.Sender.Login)
            throw new Exception($"[@{request.Input.Sender.Login}] пересчёт рейтинга может вызвать только администратор");

        var gamers = await store.GamerStore.GetItems(token);
        foreach (var gamer in gamers)
            gamer.Rating = 1;

        var matches = await store.MatchStore.GetItems(token, m => m
            .Where(p => p.Date.HasValue)
            .Include(p => p.Gamers)
            .Include(p => p.Sets));

        foreach (var match in matches)
        {
            var winner = match.LastWinner;
            var loser = match.LastLoser;
            
            var oldRatings = new Dictionary<string, float>
            {
                { winner.Login, match.LastWinner.Rating },
                { loser.Login, match.LastLoser.Rating }
            };
            
            var ratings = new Dictionary<string, float>(oldRatings);
            match.ReCalculateRating(ratings);
            winner.Rating = ratings[winner.Login];
            loser.Rating = ratings[loser.Login];

            logger.LogTrace(
                """
                Пересчитываем матч: {Date}, партии {@Set}
                 {Winner} 🆚 {Loser}
                 {@Rating}
                """,
                match.Date,
                match.Sets.Select(p => p.Num),
                $"{winner.Login} {match.Sets.Sum(p => p.GetPoints(winner.Login))}",
                $"{match.Sets.Sum(p => p.GetPoints(loser.Login))} {loser.Login}",
                (string[])
                [
                    $"{winner.Rating * 100:F0} ({(winner.Rating - oldRatings[winner.Login]) * 100:F0})",
                    $"{loser.Rating * 100:F0} ({(loser.Rating - oldRatings[loser.Login]) * 100:F0})"
                ]);
        }

        await store.Update(token);

        await mediator.Send(new SendMessageQuery(request.Input.ChatId,
            $"""
             ⚠️ <b>Рейтинг пересчитан</b>
             """, DisableNotification: false), token);
    }
}