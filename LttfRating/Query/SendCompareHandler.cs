namespace LttfRating;

public record SendCompareQuery(TelegramInput Input) : IRequest;

public class SendCompareHandler(
    IUnitOfWork store,
    IMediator mediator)
    : IRequestHandler<SendCompareQuery>
{
    public async Task Handle(SendCompareQuery request, CancellationToken token)
    {
        var match = UpdateExtensions.CompareRatingRegex.Match(request.Input.Text);
        if (!match.Success)
            throw new ValidationException("Неудалось разобрать сообщение");
        
        var gamerLogin1 = match.Groups["User1"].Value.Trim('@').Trim();
        var gamerLogin2 = match.Groups["User2"].Value.Trim('@').Trim();
        
        if (gamerLogin1 == gamerLogin2)
            throw new ValidationException("Необходимо указать разных игроков");

        var gamer1 = await store.GameStore.GetByKey(gamerLogin1, token, q => q
                         .Include(p => p.Matches)
                         .ThenInclude(p => p.Gamers)
                         .Include(p => p.Matches)
                         .ThenInclude(p => p.Sets))
                     ?? throw new ValidationException($"@{gamerLogin1} - пока нет в рейтинге");
        var gamer2 = await store.GameStore.GetByKey(gamerLogin2, token, q => q
                         .Include(p => p.Matches)
                         .ThenInclude(p => p.Gamers)
                         .Include(p => p.Matches)
                         .ThenInclude(p => p.Sets))
                     ?? throw new ValidationException($"@{gamerLogin2} - пока нет в рейтинге");

        var commonMatches = GetCommonMatches(gamer1, gamer2);

        if (commonMatches.Length == 0)
        {
            await mediator.Send(new SendMessageQuery(request.Input.ChatId,
                $"""
                 <b>{gamer1.Login} 🆚 {gamer2.Login}</b>
                 <i>Нет завершённых матчей</i>
                 """), token);

            return;
        }

        await mediator.Send(new SendMessageQuery(request.Input.ChatId,
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
            .OrderByDate()
            .ToArray();
    }
}