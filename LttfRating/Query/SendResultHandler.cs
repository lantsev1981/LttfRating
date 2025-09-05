namespace LttfRating;

public record SendResultQuery(TelegramInput Input, Guid MatchId, OldRating OldRating) : IRequest;

public class SendResultHandler(
    IUnitOfWork store,
    IMediator mediator)
    : IRequestHandler<SendResultQuery>
{
    public async Task Handle(SendResultQuery request, CancellationToken token)
    {
        var match = await store.MatchStore.GetByKey(request.MatchId, token, q => q
                        .Include(p => p.Gamers)
                        .Include(p => p.Sets))
                    ?? throw new NullReferenceException($"Матч {request.MatchId} не найден");

        var winner = match.LastWinner;
        var loser = match.LastLoser;
        var lastSet = match.Sets.Last();

        InlineKeyboardButton[][] inlineKeyboard =
        [
            [
                InlineKeyboardButton.WithCallbackData($"❌ Удалить",
                    $"/deleteset {request.Input.ChatId} {request.Input.MessageId}")
            ]
        ];
        await mediator.Send(new SendMessageQuery(request.Input.ChatId,
            $"""
             <i>Партия (⚔️) #{lastSet.Num} • Матч до {match.SetWonCount} побед</i>

             <b>@{winner.Login} {lastSet.WonPoint:00} 🆚 {lastSet.LostPoint:00} @{loser.Login}</b>
             ⚔️ По партиям:  {match.WinnerSetCount} — {match.LoserSetCount}
             """, Buttons: new InlineKeyboardMarkup(inlineKeyboard)), token);

        if (!match.IsPending)
        {
            inlineKeyboard =
            [
                [
                    InlineKeyboardButton.WithCallbackData($"{winner.Login} 📊 {loser.Login}",
                        $"/rating @{winner.Login} @{loser.Login}")
                ],
                [
                    InlineKeyboardButton.WithCallbackData($"{loser.Login} 📊 {winner.Login}",
                        $"/rating @{loser.Login} @{winner.Login}")
                ],
                [
                    InlineKeyboardButton.WithCallbackData($"🌟 {winner.Login}",
                        $"/rating @{winner.Login}"),
                    InlineKeyboardButton.WithCallbackData($"🌟 {loser.Login}",
                        $"/rating @{loser.Login}")
                ]
            ];
            var points = match.Sets.Sum(p => p.Points);
            var winnerPoints = match.Sets.Sum(p => p.GetPoints(winner.Login));
            var loserPoints = points - winnerPoints;
            var subPoints = winnerPoints - loserPoints;
            var winnerSubRating = winner.Rating - request.OldRating.Winner;
            var loserSubRating = loser.Rating - request.OldRating.Loser;
            await mediator.Send(new SendMessageQuery(request.Input.ChatId,
                $"""
                 <i>Матч завершён</i>

                 <b>@{winner.Login} {match.WinnerSetCount} 🆚 {match.LoserSetCount} @{loser.Login}</b>
                  ⬤  По очкам: {winnerPoints} — {loserPoints} <code>({(subPoints >= 0 ? "+" : "")}{subPoints}●)</code>
                 🌟 Рейтинг: {winner.Rating * 100:F0} <code>({(winnerSubRating >= 0 ? "+" : "")}{winnerSubRating * 100:F0}*)</code> — {loser.Rating * 100:F0} <code>({(loserSubRating >= 0 ? "+" : "")}{loserSubRating * 100:F0}*)</code>
                 """, Buttons: new InlineKeyboardMarkup(inlineKeyboard)), token);
            
            // Проверяем события
            await mediator.Send(new SendRatingEventQuery(request.Input, [winner.Login, loser.Login]), token);
            await mediator.Send(new SendRatingEventQuery(request.Input, [loser.Login, winner.Login]), token);
        }
    }
}