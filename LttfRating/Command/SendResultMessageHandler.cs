namespace LttfRating;

public record SendResultMessageCommand(long ChatId, Guid MatchId) : IRequest;

public class SendResultMessageHandler(
    IDomainStore<Match> store,
    IMediator mediator)
    : IRequestHandler<SendResultMessageCommand>
{
    public async Task Handle(SendResultMessageCommand request, CancellationToken token)
    {
        var match = await store.GetByKey(request.MatchId, token)
                    ?? throw new NullReferenceException($"Матч {request.MatchId} не найден");

        var winner = match.LastWinner;
        var loser = match.LastLoser;
        var lastSet = match.Sets.Last();

        await mediator.Send(new SendMessageCommand(request.ChatId,
            $"""
             <i>Партия #{lastSet.Num} • Матч до {match.SetWonCount} побед</i>

             <b>@{winner.Login} 🆚 @{loser.Login}</b>
             <code>┌────────────────┐
             {match.WinnerSetCount} {ToEmojiDigits(lastSet.WonPoint, "00")} — {ToEmojiDigits(lastSet.LostPoint, "00")} {match.LoserSetCount}
             └────────────────┘</code>
             """), token);

        if (!match.IsPending)
        {
            var points = match.Sets.Sum(p => p.Points);
            var winnerPoints = match.Sets.Sum(p => p.GetPoints(winner.Login));
            var loserPoints = points - winnerPoints;
            var winnerSubRating = winner.Rating - winner.OldRating;
            var loserSubRating = loser.Rating - loser.OldRating;
            await mediator.Send(new SendMessageCommand(request.ChatId,
                $"""
                 <i>Матч завершён</i>

                 <b>🏆 @{winner.Login} 🆚 @{loser.Login}</b>
                 <code> ┌───────────────┐
                 {winnerPoints:00}   {ToEmojiDigits(match.WinnerSetCount, "0")} — {ToEmojiDigits(match.LoserSetCount, "0")}   {loserPoints:00}
                  └───────────────┘</code>

                 📊 Изменение рейтинга:
                 {winner.Rating * 100:F0} <code>({(winnerSubRating >= 0 ? "+" : "")}{winnerSubRating * 100:F0})</code> — {loser.Rating * 100:F0} <code>({(loserSubRating >= 0 ? "+" : "")}{loserSubRating * 100:F0})</code>
                 """), token);
        }
    }

    static string ToEmojiDigits(int number, string format)
    {
        var digits = number.ToString(format).ToCharArray();
        var emojiDigits = digits.Select(d => d switch
        {
            '0' => "0️⃣",
            '1' => "1️⃣",
            '2' => "2️⃣",
            '3' => "3️⃣",
            '4' => "4️⃣",
            '5' => "5️⃣",
            '6' => "6️⃣",
            '7' => "7️⃣",
            '8' => "8️⃣",
            '9' => "9️⃣",
            _ => d.ToString()
        });
        return string.Join("", emojiDigits);
    }
}