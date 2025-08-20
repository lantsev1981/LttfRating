namespace LttfRating;

public record GetOrAddMatchCommand(string Winner, string Loser) : IRequest<Guid>;

public class GetOrAddMatchHandler(
    IUnitOfWork store,
    ILogger<GetOrAddMatchHandler> logger)
    : IRequestHandler<GetOrAddMatchCommand, Guid>
{
    public async Task<Guid> Handle(GetOrAddMatchCommand request, CancellationToken token)
    {
        var winner = await store.GameStore.GetByKey(request.Winner, token, q => q
                         .Include(p => p.Matches)
                         .ThenInclude(p => p.Gamers))
                     ?? throw new NullReferenceException($"Игрок {request.Winner} не найден");
        
        var loser = await store.GameStore.GetByKey(request.Loser, token)
                    ?? throw new NullReferenceException($"Игрок {request.Loser} не найден");

        // Из-за удаления партий, может быть несколько открытых матчей
        var match = winner.Matches
            .FirstOrDefault(p =>
                p.IsPending
                && p.Gamers.Contains(loser));

        if (match is null)
        {
            logger.LogTrace("Добавляем новый матч: @{Winner} \ud83c\udd9a @{Loser}",
                winner.Login,
                loser.Login);

            match = new Match
            {
                Gamers = [winner, loser]
            };

            winner.Matches.Add(match);

            await store.GameStore.Update(winner, token);
        }

        return match.Id;
    }
}