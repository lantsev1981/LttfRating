namespace LttfRating;

public interface IUnitOfWork
{
    IGamerStore GameStore { get; }
    IDomainStore<Match> MatchStore { get; }
    IDomainStore<Set> SetStore { get; }
    IDomainStore<TelegramInput> TelegramInputStore { get; }
    Task Update(CancellationToken token);
}

public class UnitOfWork(
    AppDbContext context,
    IGamerStore gameStore,
    IDomainStore<Match> matchStore,
    IDomainStore<Set> setStore,
    IDomainStore<TelegramInput> telegramInputStore) : IUnitOfWork
{
    public IGamerStore GameStore { get; } = gameStore;
    public IDomainStore<Match> MatchStore { get; } = matchStore;
    public IDomainStore<Set> SetStore { get; } = setStore;
    public IDomainStore<TelegramInput> TelegramInputStore { get; } = telegramInputStore;

    public async Task Update(CancellationToken token)
    {
        if (!context.ChangeTracker.HasChanges())
            return;

        var result = await context.SaveChangesAsync(token);

        if (result < 1)
            throw new OperationException($"Изменения не применились");
    }
}