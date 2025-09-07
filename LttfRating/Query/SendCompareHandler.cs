namespace LttfRating;

public record SendCompareQuery(TelegramInput Input) : IRequest;

public class SendCompareHandler(
    IUnitOfWork store,
    IMediator mediator)
    : IRequestHandler<SendCompareQuery>
{
    private const string FileName = "compare.png";

    public async Task Handle(SendCompareQuery request, CancellationToken token)
    {
        var regexMatch = UpdateExtensions.CompareRatingRegex.Match(request.Input.Text);
        if (!regexMatch.Success)
            throw new ValidationException("Не удалось разобрать сообщение");

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

        if (matches.Length <= 1)
        {
            await mediator.Send(new SendMessageQuery(request.Input.ChatId,
                $"""
                 <b>{gamer1.Login} 🆚 {gamer2.Login}</b>
                 <i>Пока нечего анализировать</i>
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
        var compare = matches.GetCompare(gamer1, gamer2);
        
        return $"""
                <b>@{gamer1.Login} 🆚 @{gamer2.Login}</b>
                🌟 Рейтинг (в общем зачёте): {gamer1.Rating * 100:F0} — {gamer2.Rating * 100:F0} <code>({(compare.SubRating >= 0 ? "+" : "")}{compare.SubRating * 100:F0}*)</code>
                🏓 По матчам: {compare.Wins[0]} — {compare.Wins[1]} <code>({(compare.SubWins >= 0 ? "+" : "")}{compare.SubWins})</code>
                ⚔️ По партиям: {compare.Sets[0]} — {compare.Sets[1]} <code>({(compare.SubSets >= 0 ? "+" : "")}{compare.SubSets})</code>
                 ⬤  По очкам: {compare.Points[0]} — {compare.Points[1]} <code>({(compare.SubPoints >= 0 ? "+" : "")}{compare.SubPoints}●)</code>
                 ⬤ / ⚔️: <code>({(compare.SubPoints >= 0 ? "+" : "-")}{compare.SubPointsPerSet:F2}●)</code>
                """;
    }

    private string GetAllMatchesStats(Gamer gamer1, Gamer gamer2, Match[] matches)
    {
        
        var compare = matches.GetCompareForChar(gamer1, gamer2);

        GenerateCharDataImage(compare.SubPoints);

        return $"""
                🌟 Рейтинг (в личном зачёте): {compare.Ratings[gamer1.Login] * 100:F0} — {compare.Ratings[gamer2.Login] * 100:F0} <code>({(compare.SubRating >= 0 ? "+" : "")}{compare.SubRating * 100:F0}*)</code>
                """;
    }

    private static void GenerateCharDataImage(int[] subPoints)
    {
        var plt = new Plot();

        var matches = Enumerable.Range(1, subPoints.Length).ToArray();
        
        var rightAxis = plt.Axes.AddRightAxis();
        
        var zeroLine = plt.Add.HorizontalLine(0);
        zeroLine.Axes.YAxis = rightAxis;
        zeroLine.Color = Colors.Green;
        zeroLine.LineWidth = 1;
        
        var scatter = plt.Add.Scatter(matches, subPoints);
        scatter.Axes.YAxis = rightAxis;
        scatter.LineWidth = 0;
        scatter.MarkerSize = 4;
        scatter.Color = Colors.SteelBlue;
        scatter.LegendText = "Разница в очках (●)";
        //scatter.Smooth = true;
        scatter.LinePattern = LinePattern.Dotted;

        var movingAvgStep = (int)Math.Floor(subPoints.Length / 4f);
        var movingAvg = MatchExtensions.CalculateMovingAvg(subPoints, movingAvgStep);
        var maScatter = plt.Add.Scatter(matches[(movingAvgStep - 1)..], movingAvg);
        maScatter.Axes.YAxis = rightAxis;
        maScatter.LineWidth = 3;
        maScatter.MarkerSize = 0;
        maScatter.Color = Colors.Red;
        maScatter.LegendText = $"MA{movingAvgStep} (●)";
        maScatter.Smooth = true;

        // Настройка интервалов тиков для гарантированного отображения
        plt.Axes.Bottom.TickGenerator = new ScottPlot.TickGenerators.NumericFixedInterval(
            GetTickSpacing(matches.Length, 0));
        plt.Axes.Right.TickGenerator = new ScottPlot.TickGenerators.NumericFixedInterval(
            GetTickSpacing(subPoints.Min(), subPoints.Max()));

        // Настройка сетки для лучшей видимости делений
        plt.Grid.LineWidth = 1;
        plt.Grid.LineColor = Colors.LightGray;

        plt.Title("Динамика игрока");
        plt.XLabel("Партии");
        plt.Axes.Left.Label.Text = "Очки (●)";
        plt.Axes.Right.Label.Text = "Очки (●)";

        plt.ShowLegend();
        plt.SavePng(FileName, 1200, 600);
    }

    private static int GetTickSpacing(float min, float max)
    {
        return (int)Math.Ceiling(Math.Abs(max - min) / 10);
    }
}