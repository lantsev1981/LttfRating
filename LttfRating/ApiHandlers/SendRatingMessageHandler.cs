namespace LttfRating;

public record SendRatingMessageCommand(Message Message) : IRequest;

public class SendRatingMessageHandler(
    IGamerStore gamerStore,
    IMediator mediator,
    ILogger<SendRatingMessageHandler> logger)
    : IRequestHandler<SendRatingMessageCommand>
{
    public async Task Handle(SendRatingMessageCommand request, CancellationToken token)
    {
        var allGamers = (await gamerStore.GetItems(token))
            .Where(p => p.Rating != 1) // исключаем "нейтральных"
            .OrderByDescending(p => p.Rating)
            .ToArray();

        var gamer = allGamers
            .SingleOrDefault(p => p.Login == request.Message.From!.Username);

        if (gamer == null)
        {
            await mediator.Send(new SendMessageCommand(request.Message.Chat.Id,
                "Вы пока не участвуете в рейтинге."), token);
            return;
        }

        // Место в рейтинге
        int place = Array.IndexOf(allGamers, gamer) + 1;

        // Эмодзи в зависимости от позиции
        string placeEmoji = place switch
        {
            1 => "🥇",
            2 => "🥈",
            3 => "🥉",
            <= 5 => "🚀",
            <= 10 => "🎯",
            _ => "📌"
        };

        // Эмодзи для рейтинга
        string ratingEmoji = (gamer.Rating * 100) switch
        {
            >= 200 => "👑",
            >= 180 => "🔥",
            >= 150 => "⚡",
            >= 120 => "💪",
            >= 100 => "🎮",
            _ => "🌱"
        };

        // Соседи по рейтингу
        string? above = null, below = null;

        if (place > 1)
        {
            var higher = allGamers[place - 2]; // место выше — индекс (place-2)
            int diff = (int)((higher.Rating - gamer.Rating) * 100);
            above = $"<i>Место: {place - 1} @{higher.Login} • Рейтинг: {higher.Rating * 100:F0} ({(diff >= 0 ? "+" : "")}{diff})</i>";
        }

        if (place < allGamers.Length)
        {
            var lower = allGamers[place]; // место ниже — индекс (place)
            int diff = (int)((lower.Rating - gamer.Rating) * 100);
            below = $"<i>Место: {place + 1} @{lower.Login} • Рейтинг: {lower.Rating * 100:F0} ({diff})</i>";
        }

        // Группировка матчей по противникам
        var statsByOpponent = gamer.Matches
            .Where(m => !m.IsPending)
            .GroupBy(m => m.Opponent(gamer))
            .Select(g => new
            {
                Opponent = g.Key.Login,
                Wins = g.Count(m => m.LastWinner == gamer),
                Losses = g.Count(m => m.LastWinner != gamer),
                PointsWon = g.Sum(m => m.Sets.Sum(s => s.GetPoints(gamer.Login))),
                PointsLost = g.Sum(m => m.Sets.Sum(s => s.GetPoints(g.Key.Login)))
            })
            .OrderByDescending(x => x.Wins - x.Losses)
            .ThenByDescending(x => x.PointsWon - x.PointsLost)
            .ToArray();

        // Общая статистика
        int totalWins = statsByOpponent.Sum(s => s.Wins);
        int totalLosses = statsByOpponent.Sum(s => s.Losses);
        int totalPointsWon = statsByOpponent.Sum(s => s.PointsWon);
        int totalPointsLost = statsByOpponent.Sum(s => s.PointsLost);
        int pointsDiff = totalPointsWon - totalPointsLost;

        // Самый длинный победный и проигрышный серия
        var results = gamer.Matches
            .Where(m => !m.IsPending)
            .Select(m => m.LastWinner == gamer)
            .ToArray();

        int longestWinStreak = 0, currentWinStreak = 0;
        int longestLossStreak = 0, currentLossStreak = 0;

        foreach (var win in results)
        {
            if (win)
            {
                currentWinStreak++;
                currentLossStreak = 0;
                longestWinStreak = Math.Max(longestWinStreak, currentWinStreak);
            }
            else
            {
                currentLossStreak++;
                currentWinStreak = 0;
                longestLossStreak = Math.Max(longestLossStreak, currentLossStreak);
            }
        }

        // Формирование сообщения
        var opponentsView = string.Join("\n", statsByOpponent.Select(s =>
            $"<b>@{s.Opponent}</b>: {s.Wins}-{s.Losses} <i>({(s.PointsWon - s.PointsLost >= 0 ? "+" : "")}{s.PointsWon - s.PointsLost})</i>"));

        await mediator.Send(new SendMessageCommand(request.Message.Chat.Id,
            $"""
             {above ?? ""}
             {placeEmoji} <b>Место: {place}</b> из {allGamers.Length} • Рейтинг: {gamer.Rating * 100:F0} {ratingEmoji}
             {below ?? ""}

             🏓 Всего матчей: {totalWins + totalLosses}
             📈 Побед: {totalWins} | Поражений: {totalLosses}
             🎯 Очки: {(pointsDiff >= 0 ? "+" : "")}{pointsDiff}
             🔁 Серии: побед {longestWinStreak}, проигрышей {longestLossStreak}

             📋 Статистика по соперникам:
             {opponentsView}
             """), token);
    }
}