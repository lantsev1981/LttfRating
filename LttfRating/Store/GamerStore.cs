namespace LttfRating;

public class GamerStore(AppDbContext context) : IDomainStore<Gamer>
{
    public async Task<Gamer?> GetById<TKey>(TKey id, CancellationToken token)
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

    public Task<IEnumerable<Gamer>> GetItems(CancellationToken token)
    {
        throw new NotImplementedException();
    }

    public async Task UpdateItem(Gamer item, CancellationToken token)
    {
        var result = await context.SaveChangesAsync(token);

        if (result < 1)
            throw new OperationException($"{nameof(Gamer)}.{item.Login}: изменения не применились");
    }
}