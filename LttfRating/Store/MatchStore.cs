namespace LttfRating;

public class MatchStore(AppDbContext context) : IDomainStore<Match>
{
    public async Task<Match?> GetByKey<TKey>(TKey id, CancellationToken token,
        Func<IQueryable<Match>, IQueryable<Match>>? includeQuery = null)
    {
        if (id is not Guid key)
            throw new KeyNotFoundException($"Не верный тип ключа {id?.GetType().Name}");
        
        var query = context.Matches
            .AsQueryable()
            .AsSplitQuery();

        if (includeQuery != null)
            query = includeQuery(query);

        return await query
            .SingleOrDefaultAsync(p => p.Id == key, token);
    }

    public async Task<Match[]> GetItems(CancellationToken token,
        Func<IQueryable<Match>, IQueryable<Match>>? includeQuery = null)
    {
        var query = context.Matches
            .AsQueryable()
            .AsSplitQuery();

        if (includeQuery != null)
            query = includeQuery(query);

        var matches =  await query
            .Where(p => !p.IsPending)
            .ToArrayAsync(token);
        
        return matches.OrderByDate().ToArray();
    }

    public Task AddAsync(Match item, CancellationToken token)
    {
        throw new NotImplementedException();
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