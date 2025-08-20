namespace LttfRating;

public class SetStore(AppDbContext context) : IDomainStore<Set>
{
    public async Task<Set?> GetByKey<TKey>(TKey id, CancellationToken token,
        Func<IQueryable<Set>, IQueryable<Set>>? includeQuery = null)
    {
        if (id is not ChatMessage key)
            throw new KeyNotFoundException($"Не верный тип ключа {id?.GetType().Name}");
        
        var query = context.Sets
            .AsQueryable()
            .AsSplitQuery();

        if (includeQuery != null)
            query = includeQuery(query);

        return await query
            .SingleOrDefaultAsync(p => p.ChatId == key.ChatId && p.MessageId == key.MessageId, token);
    }

    public Task<IEnumerable<Set>> GetItems(CancellationToken token,
        Func<IQueryable<Set>, IQueryable<Set>>? includeQuery = null)
    {
        throw new NotImplementedException();
    }

    public Task AddAsync(Set item, CancellationToken token)
    {
        throw new NotImplementedException();
    }

    public async Task Update(Set? item, CancellationToken token)
    {
        if (!context.ChangeTracker.HasChanges())
            return;
        
        var result = await context.SaveChangesAsync(token);

        if (result < 1)
            throw new OperationException($$"""{{nameof(Set)}}.{{{item?.MatchId}}, {{item?.Num}}}: изменения не применились""");
    }

    public async Task DeleteItem(Set item, CancellationToken token)
    {
        context.Sets.Remove(item);
        await context.SaveChangesAsync(token);
    }
}

public record ChatMessage(long ChatId, int MessageId);