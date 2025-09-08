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
        var twoHoursAgo = now.AddMinutes(-2);

        var matches = await store.MatchStore.GetItems(token, m => m
            .Where(p => p.Date.HasValue && p.Date!.Value.Date == now.Date && p.Date <= twoHoursAgo)
            .Include(p => p.Gamers)
            .Include(p => p.Sets));

        // Находим геймеров для уведомления
        var gamersToNotify = matches
            .SelectMany(match => match.Gamers
                .Where(g => g.UserId != null)
                .Select(gamer => (Gamer: gamer, MatchDate: match.Date)))
            .GroupBy(x => x.Gamer.Login)
            .ToDictionary(
                g => g.Key,
                g => g.MaxBy(x => x.MatchDate)
            );

        // Фильтруем геймеров, которым нужно отправить уведомление
        var gamersToSend = gamersToNotify.Values
            .Where(x => x.Gamer.LastSendStatistics == null 
                        || x.Gamer.LastSendStatistics < x.MatchDate)
            .Select(x => x.Gamer)
            .ToList();

        // Отправляем уведомления
        foreach (var gamer in gamersToSend)
        {
            await mediator.Send(new SendRatingQuery(
                new TelegramInput(gamer.UserId!.Value, 0, $"/rating @{gamer.Login}") 
                { 
                    Sender = null! 
                }, true), token);
        
            gamer.LastSendStatistics = now;
        }

        if (gamersToSend.Count > 0)
            await store.Update(token);

        return TimeSpan.FromMinutes(15);
    }
}