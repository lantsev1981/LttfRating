namespace LttfRating;

public record SendCompareMessageCommand(Message Message, string GamerLogin1, string GamerLogin2) : IRequest;

public class SendCompareMessageHandler(
    IGamerStore gamerStore,
    IMediator mediator,
    ILogger<SendCompareMessageHandler> logger)
    : IRequestHandler<SendCompareMessageCommand>
{
    public async Task Handle(SendCompareMessageCommand request, CancellationToken token)
    {
        var gamer1 = await gamerStore.GetByKey(request.GamerLogin1, token);
        var gamer2 = await gamerStore.GetByKey(request.GamerLogin2, token);
        if (gamer1 is null || gamer2 is null)
            return;

        var commonMatches = GetCommonMatches(gamer1, gamer2);

        if (commonMatches.Length == 0)
        {
            await mediator.Send(new SendMessageCommand(request.Message.Chat.Id,
                $"""
                 <b>{gamer1.Login} 🆚 {gamer2.Login}</b>
                 <i>Нет завершённых матчей</i>
                 """), token);

            return;
        }

        await mediator.Send(new SendMessageCommand(request.Message.Chat.Id,
            $"""
             {GetHeadToHeadStats(gamer1, gamer2, commonMatches)}

             {GetAllMatchesStats(gamer1, gamer2, commonMatches)}
             """), token);
    }

    private string GetHeadToHeadStats(Gamer gamer1, Gamer gamer2, Match[] commonMatches)
    {
        var gamer1Wins = commonMatches.Count(m => m.LastWinner == gamer1);
        var gamer2Wins = commonMatches.Count(m => m.LastWinner == gamer2);

        var totalSetsGamer1 = commonMatches.Sum(m => m.Sets.Count(s => s.WinnerLogin == gamer1.Login));
        var totalSetsGamer2 = commonMatches.Sum(m => m.Sets.Count(s => s.WinnerLogin == gamer2.Login));

        var totalPointsGamer1 = commonMatches.Sum(m => m.Sets.Sum(p => p.GetPoints(gamer1.Login)));
        var totalPointsGamer2 = commonMatches.Sum(m => m.Sets.Sum(p => p.GetPoints(gamer2.Login)));

        var subRating = gamer1.Rating - gamer2.Rating;
        var subWins = gamer1Wins - gamer2Wins;
        var subSets = totalSetsGamer1 - totalSetsGamer2;
        var subPoints = totalPointsGamer1 - totalPointsGamer2;

        return $"""
                <b>@{gamer1.Login} 🆚 @{gamer2.Login}</b>
                🌟 Рейтинг (в общем зачёте): {gamer1.Rating * 100:F0} — {gamer2.Rating * 100:F0} <code>({(subRating >= 0 ? "+" : "")}{subRating * 100:F0}*)</code>
                🏓 По матчам: {gamer1Wins} — {gamer2Wins} <code>({(subWins >= 0 ? "+" : "")}{subWins})</code>
                📋 По партиям: {totalSetsGamer1} — {totalSetsGamer2} <code>({(subSets >= 0 ? "+" : "")}{subSets})</code>
                 ⬤  По очкам: {totalPointsGamer1} — {totalPointsGamer2} <code>({(subPoints >= 0 ? "+" : "")}{subPoints}●)</code>
                """;
    }

    private string GetAllMatchesStats(Gamer gamer1, Gamer gamer2, Match[] commonMatches)
    {
        gamer1.Rating = 1;
        gamer2.Rating = 1;

        var matchesText = commonMatches
            .Select((m, index) =>
            {
                var setsGamer1 = m.Sets.Count(s => s.WinnerLogin == gamer1.Login);
                var setsGamer2 = m.Sets.Count(s => s.WinnerLogin == gamer2.Login);

                var pointsGamer1 = m.Sets.Sum(p => p.GetPoints(gamer1.Login));
                var pointsGamer2 = m.Sets.Sum(p => p.GetPoints(gamer2.Login));

                var subPoints = pointsGamer1 - pointsGamer2;

                m.CalculateRating();
                var winnerSubRating = gamer1.Rating - gamer1.OldRating;
                var loserSubRating = gamer2.Rating - gamer2.OldRating;

                return $"""
                        <i>#{commonMatches.Length - index}</i> • <b>{setsGamer1} — {setsGamer2}</b> <code>({(subPoints >= 0 ? "+" : "")}{subPoints}●)</code> • <i>{m.Date:dd.MM.yyyy HH:mm}</i>
                          🌟 Рейтинг: {gamer1.Rating * 100:F0} <code>({(winnerSubRating >= 0 ? "+" : "")}{winnerSubRating * 100:F0}*)</code> — {gamer2.Rating * 100:F0} <code>({(loserSubRating >= 0 ? "+" : "")}{loserSubRating * 100:F0}*)</code>
                        """;
            })
            .Reverse();

        var byMatchString = string.Join("\n", matchesText);

        var subRating = gamer1.Rating - gamer2.Rating;

        return $"""
                🌟 Рейтинг (в личном зачёте): {gamer1.Rating * 100:F0} — {gamer2.Rating * 100:F0} <code>({(subRating >= 0 ? "+" : "")}{subRating * 100:F0}*)</code>
                <b>Все матчи:</b>
                {byMatchString}
                """;
    }

    private Match[] GetCommonMatches(Gamer gamer1, Gamer gamer2)
    {
        return gamer1.Matches
            .Where(m => !m.IsPending && m.Gamers.Contains(gamer2))
            .OrderBy(m => m.Date)
            .ToArray();
    }
}