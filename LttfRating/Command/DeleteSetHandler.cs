namespace LttfRating;

public record DeleteSetCommand(MessageReactionUpdated MessageReaction) : IRequest;

public class DeleteSetHandler(
    IOptions<ApiConfig> config,
    IDomainStore<Set> store,
    ILogger<DeleteSetHandler> logger,
    IMediator mediator)
    : IRequestHandler<DeleteSetCommand>
{
    private readonly ApiConfig _config = config.Value;

    public async Task Handle(DeleteSetCommand request, CancellationToken token)
    {
        if (request.MessageReaction.NewReaction.LastOrDefault(p => p.Type == ReactionTypeKind.Emoji)
            is not ReactionTypeEmoji { Emoji: "👎" })
            return;

        var set = await store.GetByKey(new ChatMessage(
                request.MessageReaction.Chat.Id,
                request.MessageReaction.MessageId),
            token);

        if (set is null)
            return;

        var sender = request.MessageReaction.User!.Username!;
        var isAdmin = _config.Administrators.Contains(sender);
        var isGamer = set.Match.Gamers.Any(p => p.Login == sender);
        if (isAdmin || isGamer)
        {
            // игрок может отменить игру только в течении часа
            if (!isAdmin && DateTimeOffset.UtcNow - set.Date > TimeSpan.FromHours(1))
            {
                await mediator.Send(new SendMessageCommand(request.MessageReaction.User.Id,
                    $"""
                     ⚠️ <b>Вы не можете отменить партию</b>

                     @{sender}, отменить партию можно только в течении часа.

                     Если это ошибка — обратитесь к администратору:
                     {string.Join(", ", _config.Administrators.Select(admin => $"<a href=\"tg://user?id={admin}\">@{admin}</a>"))}
                     """), token);

                return;
            }

            var winner = set.Match.GetLastWinner();
            var loser = set.Match.GetLastLoser();

            set.Match.IsPending = true;
            await store.UpdateItem(set, token);
            await store.DeleteItem(set, token);

            logger.LogTrace("{User} удалил партию {Set}", sender, $$"""{{{set.MatchId}}, {{set.Num}}}""");

            await mediator.Send(new SendMessageCommand(request.MessageReaction.User.Id,
                $"""
                 ⚠️<b>Партия #{set.Num} • отменена</b>

                 <i>@{winner.Login} {set.WonPoint} 🆚 {set.LostPoint} @{loser.Login}</i>
                 """), token);

            // TODO седелать механизм пересчёта рейтинга игроков
            // скорее всего надо делать в фоне
            // отложить обработку новых сообщений на это время
        }
    }
}