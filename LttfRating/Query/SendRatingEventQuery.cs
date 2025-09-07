namespace LttfRating;

public record SendRatingEventQuery(TelegramInput Input, string[] Gamers, Dictionary<string, float> OldRatings) : IRequest;

public class SendRatingEventHandler(
    IUnitOfWork store,
    IMediator mediator)
    : IRequestHandler<SendRatingEventQuery>
{
    public async Task Handle(SendRatingEventQuery request, CancellationToken token)
    {
        var gamer1 = await store.GameStore.GetByKey(request.Gamers[0], token)
                     ?? throw new ValidationException($"@{request.Gamers[0]} - пока нет в рейтинге");
        var gamer2 = await store.GameStore.GetByKey(request.Gamers[1], token)
                     ?? throw new ValidationException($"@{request.Gamers[1]} - пока нет в рейтинге");

        var matches = await store.MatchStore.GetItems(token, m => m
            .Include(p => p.Gamers)
            .Include(p => p.Sets)
            .Where(p => p.Gamers.Contains(gamer1) && p.Gamers.Contains(gamer2)));

        if (matches.Length <= 2)
            return;

        var improvements = new List<string>();

        int? newRatingPosition = null;
        var oldCompare = matches[..^1].GetCompare(gamer1, gamer2);
        var newCompare = matches.GetCompare(gamer1, gamer2);
        if (request.OldRatings[gamer2.Login] >= request.OldRatings[gamer1.Login] && gamer2.Rating <= gamer1.Rating)
        {
            improvements.Add($"по рейтингу 🌟 в общем зачёте <code>({(newCompare.SubRating >= 0 ? "+" : "")}{newCompare.SubRating * 100:F0}*)</code>");
            var users = await store.GameStore.GetItems(token);
            newRatingPosition = Array.IndexOf(users, gamer1) + 1;
        }
        if (oldCompare.SubWins <= 0 && newCompare.SubWins > 0)
            improvements.Add($"по количеству побед в матчах 🏓 <code>({(newCompare.SubWins >= 0 ? "+" : "")}{newCompare.SubWins})</code>");
        if (oldCompare.SubSets <= 0 && newCompare.SubSets > 0)
            improvements.Add($"по количеству выигранных партий ⚔️ <code>({(newCompare.SubSets >= 0 ? "+" : "")}{newCompare.SubSets})</code>");
        if (oldCompare.SubPoints <= 0 && newCompare.SubPoints > 0)
            improvements.Add($"по общему количеству очков ⬤ <code>({(newCompare.SubPoints >= 0 ? "+" : "")}{newCompare.SubPoints}⬤)</code>");

        var oldCompareForChar = matches[..^1].GetCompareForChar(gamer1, gamer2);
        var newCompareForChar = matches.GetCompareForChar(gamer1, gamer2);
        if (oldCompareForChar.SubRating <= 0 && newCompareForChar.SubRating > 0)
            improvements.Add($"по рейтингу 🌟 в личном зачёте <code>({(newCompareForChar.SubRating >= 0 ? "+" : "")}{newCompareForChar.SubRating * 100:F0}*)</code>");
        if (oldCompareForChar.Ma <= 0 && newCompareForChar.Ma > 0)
            improvements.Add($"по средней динамике роста очков ⬤");

        if (!improvements.Any())
            return;

        var compareText = string.Join('\n', improvements.Select((p, i) => $"  {i + 1}. {p}"));
        var message = $"""
                       🎉 @{gamer1.Login} превзошёл @{gamer2.Login}
                       {compareText}
                       """;

        InlineKeyboardButton[] inlineKeyboard =
        [
            InlineKeyboardButton.WithCallbackData($"{gamer1.Login} 📊 {gamer2.Login}",
                $"/rating @{gamer1.Login} @{gamer2.Login}")
        ];

        await mediator.Send(new SendMessageQuery(request.Input.ChatId,
            message, Buttons: new InlineKeyboardMarkup(inlineKeyboard)), token);
        
        if (newRatingPosition <= 3)
        {
            await mediator.Send(new SendMessageQuery(request.Input.ChatId,
                $"""
                 {newRatingPosition.Value.ToEmojiPosition()} @{gamer1.Login} общем зачёте!
                 """, Buttons: new InlineKeyboardMarkup(
                    InlineKeyboardButton.WithCallbackData($"🌟 {gamer1.Login}",
                        $"/rating @{gamer1.Login}"))), token);
        }
    }
}