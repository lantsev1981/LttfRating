namespace LttfRating;

public interface IUnitOfWork
{
    IGamerStore GameStore { get; }
    IDomainStore<Match> MatchStore { get; }
    IDomainStore<Set> SetStore { get; }
}

public class UnitOfWork(IServiceProvider serviceProvider) : IUnitOfWork, IDisposable
{
    private readonly IServiceScope _scope = serviceProvider.CreateScope();
    private Lazy<IGamerStore>? _lazyGameStore;
    private Lazy<IDomainStore<Match>>? _lazyMatchStore;
    private Lazy<IDomainStore<Set>>? _lazySetStore;

    public IGamerStore GameStore => (_lazyGameStore ??= new Lazy<IGamerStore>(
        () => _scope.ServiceProvider.GetRequiredService<IGamerStore>())).Value;

    public IDomainStore<Match> MatchStore => (_lazyMatchStore ??= new Lazy<IDomainStore<Match>>(
        () => _scope.ServiceProvider.GetRequiredService<IDomainStore<Match>>())).Value;

    public IDomainStore<Set> SetStore => (_lazySetStore ??= new Lazy<IDomainStore<Set>>(
        () => _scope.ServiceProvider.GetRequiredService<IDomainStore<Set>>())).Value;

    public void Dispose() => _scope.Dispose();
}