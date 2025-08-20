namespace LttfRating;

public record DeleteSetCommand(TelegramApiData Data) : IRequest;

public class DeleteSetHandler(
    IOptions<ApiConfig> config,
    IUnitOfWork store,
    ILogger<DeleteSetHandler> logger,
    IMediator mediator)
    : IRequestHandler<DeleteSetCommand>
{
    private readonly ApiConfig _config = config.Value;

    public async Task Handle(DeleteSetCommand request, CancellationToken token)
    {
        if (request.Data.Text.EndsWith("👎"))
            return;

        var set = await store.SetStore.GetByKey(
            new ChatMessage(request.Data.ChatId, request.Data.MessageId), token,
            q => q.Include(p => p.Match.Gamers));

        if (set is null)
            return;

        var sender = request.Data.User.Login;
        var isAdmin = _config.Administrators.Contains(sender);
        var isGamer = set.Match.Gamers.Any(p => p.Login == sender);
        if (isAdmin || isGamer)
        {
            // игрок может отменить игру только в течении часа
            if (!isAdmin && DateTimeOffset.UtcNow - set.Date > TimeSpan.FromHours(1))
            {
                await mediator.Send(new SendMessageCommand(request.Data.User.Id,
                    $"""
                     ⚠️ <b>Вы не можете отменить партию</b>

                     @{sender}, отменить партию можно только в течении часа.

                     Если это ошибка — обратитесь к администратору:
                     {string.Join(", ", _config.Administrators.Select(admin => $"<a href=\"tg://user?id={admin}\">@{admin}</a>"))}
                     """), token);

                return;
            }

            var winner = set.Match.LastWinner;
            var loser = set.Match.LastLoser;
            var admin = await store.GameStore.GetAdminGamerId(token);

            set.Match.IsPending = true;
            await store.SetStore.Update(set, token);
            await store.SetStore.DeleteItem(set, token);

            logger.LogTrace("{User} удалил партию {Set}", sender, $$"""{{{set.MatchId}}, {{set.Num}}}""");

            HashSet<long?> recipients = [admin?.UserId, winner.UserId, loser.UserId];

            foreach (var recipient in recipients)
            {
                if (recipient is null)
                    continue;

                await mediator.Send(new SendMessageCommand(recipient.Value,
                    $"""
                     ⚠️<b>Партия #{set.Num} • отменена</b>

                     <i>@{winner.Login} {set.WonPoint} 🆚 {set.LostPoint} @{loser.Login}</i>
                     """), token);
            }

            // TODO седелать механизм пересчёта рейтинга игроков
            // скорее всего надо делать в фоне
            // отложить обработку новых сообщений на это время
        }
    }
}