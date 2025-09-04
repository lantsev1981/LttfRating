namespace LttfRating;

public record DeleteSetCommand(TelegramInput Input) : IRequest;

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
        var regexMatch = UpdateExtensions.DeleteSetRegex.Match(request.Input.Text);
        if (!regexMatch.Success)
            throw new ValidationException("Не удалось разобрать сообщение");
        
        var chatId = long.Parse(regexMatch.Groups["ChatId"].Value.Trim());
        var messageId = int.Parse(regexMatch.Groups["MessageId"].Value.Trim());
        
        var set = await store.SetStore.GetByKey(
            new ChatMessage(chatId, messageId), token,
            q => q.Include(p => p.Match.Gamers))
            ?? throw new ValidationException("Партия не найдена");

        var sender = request.Input.Sender.Login;
        var isAdmin = _config.Administrators.Contains(sender);
        var isGamer = set.Match.Gamers.Any(p => p.Login == sender);
        if (isAdmin || isGamer)
        {
            // игрок может отменить игру только в течение часа
            if (!isAdmin && DateTimeOffset.UtcNow - set.Date > TimeSpan.FromHours(1))
                throw new ValidationException("Отменить партию можно только в течении часа");

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

                await mediator.Send(new SendMessageQuery(recipient.Value,
                    $"""
                     ⚠️<b>Партия (⚔️) #{set.Num} • отменена</b>

                     <i>@{winner.Login} {set.WonPoint} 🆚 {set.LostPoint} @{loser.Login}</i>
                     """), token);
            }

            // TODO седелать механизм пересчёта рейтинга игроков если отменили последнюю партию в матче
        }
    }
}