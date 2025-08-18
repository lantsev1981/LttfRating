namespace LttfRating;

public record RecalculateRatingCommand : IRequest;

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
        if (admin is null)
            return;

        await gamerStore.ClearRating(token);

        foreach (var match in await matchStore.GetItems(token))
        {
            logger.LogTrace("Пересчитываем матч: {Date}, {@Gamers}, партии {@Set}",
                match.Date,
                match.Gamers.Select(p => p.Login),
                match.Sets.Select(p => p.Num));
            
            match.CalculateRating();
        }

        await matchStore.Update(null!, token);

        await mediator.Send(new SendMessageCommand(admin.UserId!.Value,
            $"""
             ⚠️ <b>Рейтиг пересчитан</b>
             """), token);
    }
}