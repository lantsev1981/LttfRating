namespace Domain;

public static class MatchExtensions
{
    public static IEnumerable<Match> OrderByDate(this IEnumerable<Match> items)
    {
        return items.OrderBy(p => p.Date);
    }

    public static Rating ReCalculateRating(this Match match, Rating rating)
    {
        float points = match.Sets.Sum(p => p.Points);
        float winnerPoints = match.Sets.Sum(p => p.GetPoints(match.LastWinner.Login));
        float loserPoints = points - winnerPoints;

        // Формула изменения рейтинга (замкнутая система)
        var opponentPrize = winnerPoints / points * (rating.Opponent * 0.5f); // вознаграждение за силу противника
        var losePointPenalty = loserPoints / points * (rating.User * 0.5f); // штраф за пропущенные очки
        var change = opponentPrize - losePointPenalty;

        rating.User += change;
        rating.Opponent -= change;

        return rating;
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
            Ratings: [gamer1.Rating, gamer2.Rating],
            Wins: [gamer1Wins, gamer2Wins],
            Sets: [sets1, sets2],
            Points: [points1, points2]
        );
    }

    public static MatchesCharCompare GetCompareForChar(this Match[] matches, Gamer gamer1, Gamer gamer2)
    {
        var rating = new Rating { User = 1, Opponent = 1 };

        var subPoints = matches
            .SelectMany(m =>
            {
                var result = m.Sets.Select(p =>
                    p.GetPoints(gamer1.Login) - p.GetPoints(gamer2.Login));

                // пересчитываем рейтинг в личном зачёте
                m.ReCalculateRating(rating);

                return result;
            })
            .ToArray();

        var movingAvgStep = (int)Math.Floor(subPoints.Length / 4f);
        var movingAvg = CalculateMovingAvg(subPoints, movingAvgStep);

        return new MatchesCharCompare([rating.User, rating.Opponent], movingAvg.Last(), subPoints);
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
    float[] Ratings,
    float Ma,
    int[] SubPoints)
{
    public float SubRating => Ratings[0] - Ratings[1];
}