namespace LttfRating;

public interface IUnitOfWork
{
    IGamerStore GameStore { get; }
    IDomainStore<Match> MatchStore { get; }
    IDomainStore<Set> SetStore { get; }
    IDomainStore<TelegramInput> TelegramInputStore { get; }
}

public class UnitOfWork(IServiceProvider serviceProvider) : IUnitOfWork, IDisposable
{
    private readonly IServiceScope _scope = serviceProvider.CreateScope();
    private Lazy<IGamerStore>? _lazyGameStore;
    private Lazy<IDomainStore<Match>>? _lazyMatchStore;
    private Lazy<IDomainStore<Set>>? _lazySetStore;
    private Lazy<IDomainStore<TelegramInput>>? _lazyTelegramInputStore;

    public IGamerStore GameStore
        => (_lazyGameStore ??= GetService<IGamerStore, Gamer>()).Value;

    public IDomainStore<Match> MatchStore
        => (_lazyMatchStore ??= GetService<IDomainStore<Match>, Match>()).Value;

    public IDomainStore<Set> SetStore
        => (_lazySetStore ??= GetService<IDomainStore<Set>, Set>()).Value;

    public IDomainStore<TelegramInput> TelegramInputStore
        => (_lazyTelegramInputStore ??= GetService<IDomainStore<TelegramInput>, TelegramInput>()).Value;

    private Lazy<T> GetService<T, TT>() where T : IDomainStore<TT>
        => new(() => _scope.ServiceProvider.GetRequiredService<T>());

    new
        public void Dispose() => _scope.Dispose();
}