namespace LttfRating;

public class MatchStore(AppDbContext context) : IDomainStore<Match>
{
    public async Task<Match?> GetById<TKey>(TKey id, CancellationToken token)
    {
        return id is Guid matchId
            ? await context.Matches
                .Include(p => p.Sets)
                .Include(p => p.Gamers)
                .AsSplitQuery()
                .SingleOrDefaultAsync(p => p.Id == matchId, token)
            : null;
    }

    public Task AddAsync(Match item, CancellationToken token)
    {
        throw new NotImplementedException();
    }

    public Task<IEnumerable<Match>> GetItems(CancellationToken token)
    {
        throw new NotImplementedException();
    }

    public async Task UpdateItem(Match item, CancellationToken token)
    {
        var result = await context.SaveChangesAsync(token);

        if (result < 1)
            throw new OperationException($"{nameof(Match)}.{item.Id}: изменения не применились");
    }
}