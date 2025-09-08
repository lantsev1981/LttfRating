namespace LttfRating;

public record GetOrAddMatchCommand(string Winner, string Loser, byte SetWonCount) : IRequest<Guid>;

public class GetOrAddMatchHandler(
    IUnitOfWork store,
    ILogger<GetOrAddMatchHandler> logger)
    : IRequestHandler<GetOrAddMatchCommand, Guid>
{
    public async Task<Guid> Handle(GetOrAddMatchCommand request, CancellationToken token)
    {
        var winner = await store.GamerStore.GetByKey(request.Winner, token)
                     ?? throw new NullReferenceException($"Игрок {request.Winner} не найден");
        
        var loser = await store.GamerStore.GetByKey(request.Loser, token)
                    ?? throw new NullReferenceException($"Игрок {request.Loser} не найден");

        // Из-за удаления партий, может быть несколько открытых матчей
        var matches = await store.MatchStore.GetItems(token, m => m
            .Where(p => !p.Date.HasValue)
            .Include(p => p.Gamers)
            .Where(p => p.Gamers.Contains(winner) && p.Gamers.Contains(loser)));
        
        var match = matches.SingleOrDefault();

        if (match is null)
        {
            logger.LogTrace("Добавляем новый матч: @{Winner} \ud83c\udd9a @{Loser}",
                winner.Login,
                loser.Login);

            match = new Match(request.SetWonCount)
            {
                Gamers = [winner, loser]
            };

            winner.Matches.Add(match);

            await store.Update(token);
        }

        return match.Id;
    }
}