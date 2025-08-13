namespace LttfRating;

public record SendResultMessageCommand(long ChatId, Guid MatchId) : IRequest;

public class SendResultMessageHandler(
    IDomainStore<Match> store,
    ITelegramBotClient botClient)
    : IRequestHandler<SendResultMessageCommand>
{
    public async Task Handle(SendResultMessageCommand request, CancellationToken token)
    {
        var match = await store.GetById(request.MatchId, token)
                    ?? throw new NullReferenceException($"Матч {request.MatchId} не найден");
        
        var winner = match.GetLastWinner();
        var loser = match.GetLastLoser();
        var lastSet = match.Sets.Last();

        await botClient.SendMessage(
            chatId: request.ChatId,
            text: $"""
                   Матч до {match.SetWonCount} побед, партия {lastSet.Num}
                   @{winner.Login} {lastSet.WonPoint} — {lastSet.LostPoint} @{loser.Login}
                   """,
            cancellationToken: token);

        if (!match.IsPending)
        {
            var points = match.Sets.Sum(p => p.Points);
            var winnerPoints = match.Sets.Sum(p => p.GetPoints(winner.Login));
            var loserPoints = points - winnerPoints;
            
            // группируем партии по победителю
            var setGroup = match.Sets
                .GroupBy(p => p.WinnerLogin)
                .OrderByDescending(p => p.Count())
                .ToDictionary(p => p.Key, p => p.ToArray());
            
            setGroup.TryGetValue(loser.Login, out var losSets);
            
            await botClient.SendMessage(
                chatId: request.ChatId,
                text: $"""
                       🏆@{winner.Login} ({winnerPoints}) {setGroup[winner.Login].Length} — {losSets?.Length ?? 0} ({loserPoints}) @{loser.Login}
                       {(winner.Rating > winner.OldRating ? "📈" : "📉")} {winner.OldRating * 100:F0} -> {winner.Rating * 100:F0} — {loser.OldRating * 100:F0} -> {loser.Rating * 100:F0} {(loser.Rating > loser.OldRating ? "📈" : "📉")}
                       """,
                cancellationToken: token);
        }
    }
}