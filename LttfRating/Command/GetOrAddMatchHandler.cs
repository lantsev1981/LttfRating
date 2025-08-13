namespace LttfRating;

public record GetOrAddMatchCommand(string Winner, string Loser) : IRequest<Guid>;

public class GetOrAddMatchHandler(
    IDomainStore<Gamer> store,
    ILogger<GetOrAddMatchHandler> logger)
    : IRequestHandler<GetOrAddMatchCommand, Guid>
{
    public async Task<Guid> Handle(GetOrAddMatchCommand request, CancellationToken token)
    {
        var winner = await store.GetById(request.Winner, token)
                     ?? throw new NullReferenceException($"Игрок {request.Winner} не найден");
        var loser = await store.GetById(request.Loser, token)
                    ?? throw new NullReferenceException($"Игрок {request.Loser} не найден");

        var match = winner.Matches
            .SingleOrDefault(p =>
                p.IsPending
                && p.Gamers.Contains(loser));

        if (match is null)
        {
            logger.LogTrace("Добавляем новый матч: @{Winner} vs @{Loser}",
                winner.Login,
                loser.Login);

            match = new Match
            {
                Gamers = [winner, loser]
            };

            winner.Matches.Add(match);

            await store.UpdateItem(winner, token);
        }

        return match.Id;
    }
}