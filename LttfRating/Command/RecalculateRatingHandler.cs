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
        var admin = await store.GameStore.GetAdminGamerId(token);
        if (admin?.Login != request.Input.Sender.Login)
            return;

        var gamers =await store.GameStore.GetItems(token);
        foreach (var gamer in gamers)
            gamer.Rating = 1;

        var matches = await store.MatchStore.GetItems(token, q => q
            .Include(p => p.Gamers)
            .Include(p => p.Sets));
        
        foreach (var match in matches)
        {
            match.CalculateRating();

            var winner = match.LastWinner;
            var loser = match.LastLoser;
            
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
                    $"{winner.Rating * 100:F0} ({(winner.Rating - winner.OldRating) * 100:F0})",
                    $"{loser.Rating * 100:F0} ({(loser.Rating - loser.OldRating) * 100:F0})"
                ]);
        }

        await store.MatchStore.Update(null!, token);

        await mediator.Send(new SendMessageQuery(admin.UserId!.Value,
            $"""
             ⚠️ <b>Рейтиг пересчитан</b>
             """), token);
    }
}