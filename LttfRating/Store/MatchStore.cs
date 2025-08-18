namespace LttfRating;

public class MatchStore(AppDbContext context) : IDomainStore<Match>
{
    public async Task<Match?> GetByKey<TKey>(TKey id, CancellationToken token)
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

    public async Task<IEnumerable<Match>> GetItems(CancellationToken token)
    {
        var matches = await context.Matches
            .AsSplitQuery()
            .Include(p => p.Gamers)
            .Include(p => p.Sets)
            .Where(p => !p.IsPending)
            .ToArrayAsync(token);
        
        return matches.OrderByDate();
    }

    public async Task Update(Match? item, CancellationToken token)
    {
        if (!context.ChangeTracker.HasChanges())
            return;

        var result = await context.SaveChangesAsync(token);

        if (result < 1)
            throw new OperationException($"{nameof(Match)}.{item?.Id}: изменения не применились");
    }

    public Task DeleteItem(Match item, CancellationToken token)
    {
        throw new NotImplementedException();
    }
}