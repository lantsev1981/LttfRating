namespace LttfRating;

public record SendCompareQuery(TelegramInput Input) : IRequest;

public class SendCompareHandler(
    IUnitOfWork store,
    IMediator mediator)
    : IRequestHandler<SendCompareQuery>
{
    private record MatchResult(int Point, float Rating);
    private const string FileName = "compare.png";

    public async Task Handle(SendCompareQuery request, CancellationToken token)
    {
        var regexMatch = UpdateExtensions.CompareRatingRegex.Match(request.Input.Text);
        if (!regexMatch.Success)
            throw new ValidationException("Неудалось разобрать сообщение");

        var gamerLogin1 = regexMatch.Groups["User1"].Value.Trim('@').Trim();
        var gamerLogin2 = regexMatch.Groups["User2"].Value.Trim('@').Trim();

        if (gamerLogin1 == gamerLogin2)
            throw new ValidationException("Необходимо указать разных игроков");

        var gamer1 = await store.GameStore.GetByKey(gamerLogin1, token)
                     ?? throw new ValidationException($"@{gamerLogin1} - пока нет в рейтинге");
        var gamer2 = await store.GameStore.GetByKey(gamerLogin2, token)
                     ?? throw new ValidationException($"@{gamerLogin2} - пока нет в рейтинге");

        var matches = await store.MatchStore.GetItems(token, m => m
            .Include(p => p.Gamers)
            .Include(p => p.Sets)
            .Where(p => p.Gamers.Contains(gamer1) && p.Gamers.Contains(gamer2)));

        if (matches.Length == 0)
        {
            await mediator.Send(new SendMessageQuery(request.Input.ChatId,
                $"""
                 <b>{gamer1.Login} 🆚 {gamer2.Login}</b>
                 <i>Нет завершённых матчей</i>
                 """), token);

            return;
        }

        InlineKeyboardButton[][] inlineKeyboard =
        [
            [
                InlineKeyboardButton.WithCallbackData($"{gamerLogin2} 📊 {gamerLogin1}",
                    $"/rating @{gamerLogin2} @{gamerLogin1}")
            ],
            [
                InlineKeyboardButton.WithCallbackData($"🌟 {gamerLogin1}",
                    $"/rating @{gamerLogin1}"),
                InlineKeyboardButton.WithCallbackData($"🌟 {gamerLogin2}",
                    $"/rating @{gamerLogin2}")
            ]
        ];

        await mediator.Send(new SendMessageQuery(request.Input.ChatId,
            $"""
             {GetHeadToHeadStats(gamer1, gamer2, matches)}
             {GetAllMatchesStats(gamer1, gamer2, matches)}
             """, FileName: FileName, Buttons: new InlineKeyboardMarkup(inlineKeyboard)), token);
    }

    private string GetHeadToHeadStats(Gamer gamer1, Gamer gamer2, Match[] matches)
    {
        var gamer1Wins = matches.Count(m => m.LastWinner == gamer1);
        var gamer2Wins = matches.Count(m => m.LastWinner == gamer2);

        var totalSetsGamer1 = matches.Sum(m => m.Sets.Count(s => s.WinnerLogin == gamer1.Login));
        var totalSetsGamer2 = matches.Sum(m => m.Sets.Count(s => s.WinnerLogin == gamer2.Login));

        var totalPointsGamer1 = matches.Sum(m => m.Sets.Sum(p => p.GetPoints(gamer1.Login)));
        var totalPointsGamer2 = matches.Sum(m => m.Sets.Sum(p => p.GetPoints(gamer2.Login)));

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

    private string GetAllMatchesStats(Gamer gamer1, Gamer gamer2, Match[] matches)
    {
        gamer1.Rating = 1;
        gamer2.Rating = 1;

        var matchResults = matches
            .Select(m =>
            {
                var pointsGamer1 = m.Sets.Sum(p => p.GetPoints(gamer1.Login));
                var pointsGamer2 = m.Sets.Sum(p => p.GetPoints(gamer2.Login));

                var subPoint = pointsGamer1 - pointsGamer2;
                m.CalculateRating();
                var subRating = (gamer1.Rating - gamer2.Rating) * 100;

                return new MatchResult(subPoint, subRating);
//$"""
//<i>#{matches.Length - index}</i> • <b>{setsGamer1} — {setsGamer2}</b> <code>({(subPoints >= 0 ? "+" : "")}{subPoints}●)</code> • <i>{m.Date:dd.MM.yyyy HH:mm}</i>
//🌟 Рейтинг: {gamer1.Rating * 100:F0} <code>({(winnerSubRating >= 0 ? "+" : "")}{winnerSubRating * 100:F0}*)</code> — {gamer2.Rating * 100:F0} <code>({(loserSubRating >= 0 ? "+" : "")}{loserSubRating * 100:F0}*)</code>
//"""
            })
            .ToArray();

        GenerateCharDataImage(matchResults);

        var subRating = gamer1.Rating - gamer2.Rating;

        return $"""
                🌟 Рейтинг (в личном зачёте): {gamer1.Rating * 100:F0} — {gamer2.Rating * 100:F0} <code>({(subRating >= 0 ? "+" : "")}{subRating * 100:F0}*)</code>
                """;
    }

    private static void GenerateCharDataImage(MatchResult[] matchResults)
    {
        var plt = new Plot();

        var matches = Enumerable.Range(1, matchResults.Length).ToArray();
        var points = matchResults.Select(x => x.Point).ToArray();
        var ratings = matchResults.Select(x => x.Rating).ToArray();

        // Основной график
        var scatter = plt.Add.Scatter(matches, points);
        scatter.LineWidth = 3;
        scatter.MarkerSize = 8;
        scatter.Color = Colors.SteelBlue;
        scatter.LegendText = "Разница в очках (●)";

        // Скользящее среднее
        var movingAvgStep = matchResults.Length switch
        {
            <= 5 => matchResults.Length,
            <= 10 => 5,
            _ => 10
        };
        var movingAvg = CalculateMovingAvg(points, movingAvgStep);
        var maScatter = plt.Add.Scatter(matches[(movingAvgStep - 1)..], movingAvg);
        maScatter.LineWidth = 1.5f;
        maScatter.MarkerSize = 4;
        maScatter.Color = Colors.Red;
        maScatter.LegendText = $"MA{movingAvgStep} (●)";

        // График рейтинга на правой оси
        var rightAxis = plt.Axes.AddRightAxis();
        var ratingScatter = plt.Add.Scatter(matches, ratings);
        ratingScatter.Axes.YAxis = rightAxis;
        ratingScatter.LineWidth = 0.75f;
        ratingScatter.MarkerSize = 2;
        ratingScatter.Color = Colors.Green;
        ratingScatter.LegendText = "Разница в рейтинге";
        ratingScatter.LinePattern = LinePattern.Dashed;

        // Настройка интервалов тиков для гарантированного отображения
        plt.Axes.Bottom.TickGenerator = new ScottPlot.TickGenerators.NumericFixedInterval(
            GetTickSpacing(matches.Length, 0));
        plt.Axes.Left.TickGenerator = new ScottPlot.TickGenerators.NumericFixedInterval(
            GetTickSpacing(points.Min(), points.Max()));
        rightAxis.TickGenerator = new ScottPlot.TickGenerators.NumericFixedInterval(
            GetTickSpacing(ratings.Min(), ratings.Max()));

        // Настройка сетки для лучшей видимости делений
        plt.Grid.LineWidth = 1;
        plt.Grid.LineColor = Colors.LightGray;

        plt.Title("Динамика игрока");
        plt.XLabel("Матчи");
        plt.Axes.Left.Label.Text = "Очки (●)";
        rightAxis.LabelText = "Рейтинг";

        plt.ShowLegend();
        plt.SavePng(FileName, 1200, 600);
    }

    private static float[] CalculateMovingAvg(int[] data, int step)
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

    private static int GetTickSpacing(float min, float max)
    {
        return (int)Math.Ceiling(Math.Abs(max - min) / 10);
    }
}