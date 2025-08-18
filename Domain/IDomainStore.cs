namespace Domain;

public interface IDomainStore<TEntry>
{
    Task<TEntry?> GetByKey<TKey>(TKey id, CancellationToken token);
    Task AddAsync(TEntry item, CancellationToken token);
    Task<IEnumerable<TEntry>> GetItems(CancellationToken token);
    Task Update(TEntry? item, CancellationToken token);
    Task DeleteItem(TEntry item, CancellationToken token);
}