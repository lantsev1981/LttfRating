namespace LttfRating;

public class SendDayStatisticsBackgroundService(
    ILogger<BaseBackgroundService> logger,
    IServiceProvider serviceProvider) : BaseBackgroundService(logger)
{
    protected override async Task<TimeSpan> Do(CancellationToken token)
    {
        using var scope = serviceProvider.CreateScope();
        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();
        var store = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

        var now = DateTimeOffset.UtcNow;
        var delay = now.AddHours(-1);

        // проверяем игроков которым не отправлялась сводка болешь delay
        var gamers = await store.GamerStore.GetItems(token, g => g
            .Where(p => !p.LastSendStatistics.HasValue || p.LastSendStatistics.Value <= delay)
            .Where(p => p.UserId.HasValue));

        foreach (var gamer in gamers)
        {
            // все матчи игрока за сегодня
            var matches = await store.MatchStore.GetItems(token, m => m
                .Include(p => p.Gamers)
                .Include(p => p.Sets)
                .Where(p => p.Gamers.Contains(gamer))
                .Where(p => p.Sets.Any(pp => pp.Date.Date == now.Date)));

            // есть партия младше delay
            var notDelay = matches
                .SelectMany(p => p.Sets)
                .Any(p => p.Date > delay);

            // Есть новые матчи
            var hasNewMatch = matches
                .Any(p => p.Date.HasValue &&  p.Date > gamer.LastSendStatistics);

            if (notDelay || !hasNewMatch)
                continue;

            await mediator.Send(new SendRatingQuery(
                new TelegramInput(gamer.UserId!.Value, 0, $"/rating @{gamer.Login}")
                {
                    Sender = null!
                }, true), token);

            gamer.LastSendStatistics = now;
        }
        
        await store.Update(token);

        return TimeSpan.FromMinutes(15);
    }
}