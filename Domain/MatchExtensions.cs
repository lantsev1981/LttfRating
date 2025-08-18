namespace Domain;

public static class MatchExtensions
{
    public static IEnumerable<Match> OrderByDate(this IEnumerable<Match> items)
    {
        return items.OrderBy(p => p.Date);
    }

    public static void CalculateRating(this Match match)
    {
        var winner = match.LastWinner;
        var loser = match.LastLoser;

        winner.OldRating = winner.Rating;
        loser.OldRating = loser.Rating;
        float points = match.Sets.Sum(p => p.Points);
        float winnerPoints = match.Sets.Sum(p => p.GetPoints(winner.Login));
        float loserPoints = points - winnerPoints;

        // Формула изменения рейтинга (замкнутая система)
        var opponentPrize = winnerPoints / points * (loser.OldRating * 0.5f); // вознагаждение за силу противника
        var losePointPenalty = loserPoints / points * (winner.OldRating * 0.5f); // штраф за пропущенные очки
        var change = opponentPrize - losePointPenalty;
        
        winner.Rating += change;
        loser.Rating -= change;

        match.IsPending = false;
    }
}