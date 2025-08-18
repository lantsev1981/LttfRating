namespace LttfRating;

public class SetStore(AppDbContext context) : IDomainStore<Set>
{
    public async Task<Set?> GetByKey<TKey>(TKey id, CancellationToken token)
    {
        return id is ChatMessage key
            ? await context.Sets
                .Include(p => p.Match)
                .ThenInclude(p => p.Gamers)
                .AsSplitQuery()
                .SingleOrDefaultAsync(p => p.ChatId == key.ChatId && p.MessageId == key.MessageId, token)
            : null;
    }

    public Task AddAsync(Set item, CancellationToken token)
    {
        throw new NotImplementedException();
    }

    public Task<IEnumerable<Set>> GetItems(CancellationToken token)
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