namespace Domain;

public static class MatchExtensions
{
    public static IEnumerable<Match> OrderByDate(this IEnumerable<Match> items)
    {
        return items.OrderBy(p => p.Date);
    }
    
    public static void CalculateRating(this Match match)
    {
        var winner = match.GetLastWinner();
        var loser = match.GetLastLoser();

        winner.OldRating = winner.Rating;
        loser.OldRating = loser.Rating;
        var points = match.Sets.Sum(p => p.Points);
        var winnerPoints = match.Sets.Sum(p => p.GetPoints(winner.Login));
        var loserPoints = points - winnerPoints;

        // рейтинг победителя
        var pointPrize = winnerPoints / (float)(2 * points); // вознагаждение за победные очки (на двоих)
        var opponentPrize = winnerPoints * (loser.OldRating / points); // вознагаждение за силу противника
        var losePointPenalty = loserPoints * (winner.OldRating / points); // штраф за пропущенные очки
        winner.Rating += pointPrize + opponentPrize - losePointPenalty;

        // рейтинг проигравшего
        pointPrize = loserPoints / (float)(2 * points); // вознагаждение за победные очки (на двоих)
        opponentPrize = loserPoints * (winner.OldRating / points); // вознагаждение за силу противника
        losePointPenalty = winnerPoints * (loser.OldRating / points); // штраф за пропущенные очки
        loser.Rating += pointPrize + opponentPrize - losePointPenalty;

        match.IsPending = false;
    }
}