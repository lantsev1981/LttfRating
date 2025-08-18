namespace LttfRating;

public interface IGamerStore : IDomainStore<Gamer>
{
    Task<Gamer?> GetAdminGamerId(CancellationToken token);

    Task ClearRating(CancellationToken token);
}

public class GamerStore(AppDbContext context, IOptions<ApiConfig> config) : IGamerStore
{
    private readonly ApiConfig _config = config.Value;

    public async Task<Gamer?> GetByKey<TKey>(TKey id, CancellationToken token)
    {
        return id is string login
            ? await context.Gamers
                .Include(p => p.Matches)
                .ThenInclude(p => p.Gamers)
                .Include(p => p.Matches)
                .ThenInclude(p => p.Sets)
                .AsSplitQuery()
                .SingleOrDefaultAsync(p => p.Login == login, token)
            : null;
    }

    public async Task AddAsync(Gamer item, CancellationToken token)
    {
        await context.Gamers
            .AddAsync(item, token);

        await context.SaveChangesAsync(token);
    }

    public async Task<IEnumerable<Gamer>> GetItems(CancellationToken token)
    {
        return await context.Gamers
            .Include(p => p.Matches)
            .ThenInclude(p => p.Gamers)
            .Include(p => p.Matches)
            .ThenInclude(p => p.Sets)
            .OrderByDescending(p => p.Rating)
            .AsSplitQuery()
            .ToArrayAsync(token);
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

    public async Task ClearRating(CancellationToken token)
    {
        await context.Gamers.ForEachAsync(p => p.Rating = 1, token);
    }
}