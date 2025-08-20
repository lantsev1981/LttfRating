namespace LttfRating;

public interface IGamerStore : IDomainStore<Gamer>
{
    Task<Gamer?> GetAdminGamerId(CancellationToken token);
}

public class GamerStore(AppDbContext context, IOptions<ApiConfig> config) : IGamerStore
{
    private readonly ApiConfig _config = config.Value;

    public async Task<Gamer?> GetByKey<TKey>(TKey id, CancellationToken token,
        Func<IQueryable<Gamer>, IQueryable<Gamer>>? includeQuery = null)
    {
        if (id is not string key)
            throw new KeyNotFoundException($"Не верный тип ключа {id?.GetType().Name}");
        
        var query = context.Gamers
            .AsQueryable()
            .AsSplitQuery();

        if (includeQuery != null)
            query = includeQuery(query);

        return await query
            .SingleOrDefaultAsync(p => p.Login == key, token);
    }

    public async Task<IEnumerable<Gamer>> GetItems(CancellationToken token,
        Func<IQueryable<Gamer>, IQueryable<Gamer>>? includeQuery = null)
    {
        var query = context.Gamers
            .AsQueryable()
            .AsSplitQuery();

        if (includeQuery != null)
            query = includeQuery(query);

        return await query
            .OrderByDescending(p => p.Rating)
            .ToArrayAsync(token);
    }

    public async Task AddAsync(Gamer item, CancellationToken token)
    {
        await context.Gamers
            .AddAsync(item, token);

        await context.SaveChangesAsync(token);
    }

    public async Task Update(Gamer? item, CancellationToken token)
    {
        if (!context.ChangeTracker.HasChanges())
            return;

        var result = await context.SaveChangesAsync(token);

        if (result < 1)
            throw new OperationException($"{nameof(Gamer)}.{item?.Login}: изменения не применились");
    }

    public Task DeleteItem(Gamer item, CancellationToken token)
    {
        throw new NotImplementedException();
    }

    public async Task<Gamer?> GetAdminGamerId(CancellationToken token)
    {
        var adminLogin = _config.Administrators.FirstOrDefault();
        if (adminLogin is null)
            return null;

        return await GetByKey(adminLogin, token);
    }
}