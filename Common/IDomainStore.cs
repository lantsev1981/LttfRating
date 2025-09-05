namespace Common;

public interface IDomainStore<TEntry>
{
    Task<TEntry?> GetByKey<TKey>(TKey id, CancellationToken token,
        Func<IQueryable<TEntry>, IQueryable<TEntry>>? includeQuery = null);

    Task<TEntry[]> GetItems(CancellationToken token,
        Func<IQueryable<TEntry>, IQueryable<TEntry>>? includeQuery = null);

    Task AddAsync(TEntry item, CancellationToken token);

    Task DeleteItem(TEntry item, CancellationToken token);
}