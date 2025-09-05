namespace Domain;

public static class MatchExtensions
{
    public static IEnumerable<Match> OrderByDate(this IEnumerable<Match> items)
    {
        return items.OrderBy(p => p.Date);
    }

    public static OldRating CalculateRating(this Match match)
    {
        var winner = match.LastWinner;
        var loser = match.LastLoser;

        var oldRating = new OldRating(winner.Rating, loser.Rating);

        float points = match.Sets.Sum(p => p.Points);
        float winnerPoints = match.Sets.Sum(p => p.GetPoints(winner.Login));
        float loserPoints = points - winnerPoints;

        // Формула изменения рейтинга (замкнутая система)
        var opponentPrize = winnerPoints / points * (oldRating.Loser * 0.5f); // вознагаждение за силу противника
        var losePointPenalty = loserPoints / points * (oldRating.Winner * 0.5f); // штраф за пропущенные очки
        var change = opponentPrize - losePointPenalty;

        winner.Rating += change;
        loser.Rating -= change;

        match.IsPending = false;

        return oldRating;
    }

    public static MatchesCompare GetCompare(this Match[] matches, Gamer gamer1, Gamer gamer2)
    {
        var gamer1Wins = matches.Count(m => m.LastWinner == gamer1);
        var gamer2Wins = matches.Count(m => m.LastWinner == gamer2);

        var sets1 = matches.Sum(m => m.Sets.Count(s => s.WinnerLogin == gamer1.Login));
        var sets2 = matches.Sum(m => m.Sets.Count(s => s.WinnerLogin == gamer2.Login));

        var points1 = matches.Sum(m => m.Sets.Sum(p => p.GetPoints(gamer1.Login)));
        var points2 = matches.Sum(m => m.Sets.Sum(p => p.GetPoints(gamer2.Login)));

        return new MatchesCompare(
            Ratings: [ gamer1.Rating, gamer2.Rating],
            Wins: [ gamer1Wins, gamer2Wins],
            Sets: [ sets1, sets2],
            Points: [ points1, points2]
        );
    }

    public static MatchesCharCompare GetCompareForChar(this Match[] matches, Gamer gamer1, Gamer gamer2)
    {
        gamer1.Rating = 1;
        gamer2.Rating = 1;

        var subPoints = matches
            .SelectMany(m =>
            {
                var result = m.Sets.Select(p =>
                    p.GetPoints(gamer1.Login) - p.GetPoints(gamer2.Login));

                // пересчитываем рейтинг в личном зачёте
                m.CalculateRating();
                
                return result;
            })
            .ToArray();
        
        var movingAvgStep = (int)Math.Floor(subPoints.Length / 4f);
        var movingAvg = CalculateMovingAvg(subPoints, movingAvgStep);

        return new MatchesCharCompare(gamer1.Rating - gamer2.Rating, movingAvg.Last(), subPoints);
    }
    
    public static float[] CalculateMovingAvg(int[] data, int step)
    {
        float[] result = new float[data.Length - step + 1];

        for (var i = 0; i < result.Length; i++)
        {
            var sum = 0;
            for (var j = 0; j < step; j++)
            {
                sum += data[i + j];
            }

            result[i] = sum / (float)step;
        }

        return result;
    }
}

public record MatchesCompare(
    float[] Ratings,
    int[] Wins,
    int[] Sets,
    int[] Points)
{
    public float SubRating => Ratings[0] - Ratings[1];
    public int SubWins => Wins[0] - Wins[1];
    public int SubSets => Sets[0] - Sets[1];
    public int SubPoints => Points[0] - Points[1];
    public float SubPointsPerSet => Math.Abs(SubPoints / (float)Sets.Sum());
}

public record MatchesCharCompare(
    float Ratings,
    float Ma,
    int[] SubPoints);

public record MatchResult(int Point);