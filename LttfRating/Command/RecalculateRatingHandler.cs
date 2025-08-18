namespace LttfRating;

public record RecalculateRatingCommand(string Login) : IRequest;

public class RecalculateRatingHandler(
    IDomainStore<Match> matchStore,
    IGamerStore gamerStore,
    IMediator mediator,
    ILogger<RecalculateRatingHandler> logger)
    : IRequestHandler<RecalculateRatingCommand>
{
    public async Task Handle(RecalculateRatingCommand request, CancellationToken token)
    {
        var admin = await gamerStore.GetAdminGamerId(token);
        if (admin?.Login != request.Login)
            return;

        await gamerStore.ClearRating(token);

        foreach (var match in await matchStore.GetItems(token))
        {
            match.CalculateRating();

            var winner = match.LastWinner;
            var loser = match.LastLoser;
            
            logger.LogTrace(
                """
                Пересчитываем матч: {Date}, партии {@Set}
                 {Winner} vs {Loser}
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

        await matchStore.Update(null!, token);

        await mediator.Send(new SendMessageCommand(admin.UserId!.Value,
            $"""
             ⚠️ <b>Рейтиг пересчитан</b>
             """), token);
    }
}