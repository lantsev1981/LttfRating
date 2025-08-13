namespace Domain;

public interface IDomainStore<TEntry>
{
    Task<TEntry?> GetById<TKey>(TKey id, CancellationToken token);
    Task AddAsync(TEntry item, CancellationToken token);
    Task<IEnumerable<TEntry>> GetItems(CancellationToken token);
    Task UpdateItem(TEntry item, CancellationToken token);
}