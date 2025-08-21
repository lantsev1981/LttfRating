namespace LttfRating;

public class TelegramInputStore(AppDbContext context) : IDomainStore<TelegramInput>
{
    public Task<TelegramInput?> GetByKey<TKey>(TKey id, CancellationToken token,
        Func<IQueryable<TelegramInput>, IQueryable<TelegramInput>>? includeQuery = null)
    {
        throw new NotImplementedException();
    }

    public async Task<IEnumerable<TelegramInput>> GetItems(CancellationToken token,
        Func<IQueryable<TelegramInput>, IQueryable<TelegramInput>>? includeQuery = null)
    {
        var query = context.TelegramInputs
            .AsQueryable()
            .AsSplitQuery();

        if (includeQuery != null)
            query = includeQuery(query);

        return await query
            .ToArrayAsync(token);
    }

    public async Task AddAsync(TelegramInput item, CancellationToken token)
    {
        await context.TelegramInputs
            .AddAsync(item, token);

        await context.SaveChangesAsync(token);
    }

    public Task Update(TelegramInput? item, CancellationToken token)
    {
        throw new NotImplementedException();
    }

    public async Task DeleteItem(TelegramInput item, CancellationToken token)
    {
        context.TelegramInputs.Remove(item);
        await context.SaveChangesAsync(token);
    }
}