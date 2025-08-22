namespace LttfRating;

public static class UpdateExtensions
{
    public static TelegramInput GetInput(this Update update)
        => update.Type switch
        {
            UpdateType.Message => new TelegramInput(
                update.Type.ToString(),
                update.Message!.Chat.Id,
                update.Message.MessageId,
                update.Message.Text ?? string.Empty)
            {
                Sender = GetSender(update)
            },

            UpdateType.MessageReaction => new TelegramInput(
                update.Type.ToString(),
                update.MessageReaction!.Chat.Id,
                update.MessageReaction.MessageId,
                update.MessageReaction!.NewReaction.GetEmoji())
            {
                Sender = GetSender(update)
            },

            UpdateType.CallbackQuery => new TelegramInput(
                update.Type.ToString(),
                update.CallbackQuery!.Message!.Chat.Id,
                update.CallbackQuery.Message.MessageId,
                update.CallbackQuery.Data!)
            {
                Sender = GetSender(update)
            },

            UpdateType.EditedMessage => new TelegramInput(
                update.Type.ToString(),
                update.EditedMessage!.Chat.Id,
                update.EditedMessage.MessageId,
                update.EditedMessage.Text ?? string.Empty)
            {
                Sender = GetSender(update)
            },

            UpdateType.InlineQuery => new TelegramInput(
                update.Type.ToString(),
                -1, // нет ChatId для InlineQuery
                -1, // нет MessageId
                update.InlineQuery!.Query)
            {
                Sender = GetSender(update)
            },

            UpdateType.ChosenInlineResult => new TelegramInput(
                update.Type.ToString(),
                -1, // нет ChatId
                -1, // нет MessageId
                update.ChosenInlineResult!.ResultId)
            {
                Sender = GetSender(update)
            },

            UpdateType.ShippingQuery => new TelegramInput(
                update.Type.ToString(),
                -1, // нет ChatId
                -1, // нет MessageId
                update.ShippingQuery!.InvoicePayload)
            {
                Sender = GetSender(update)
            },

            UpdateType.PreCheckoutQuery => new TelegramInput(
                update.Type.ToString(),
                -1, // нет ChatId
                -1, // нет MessageId
                update.PreCheckoutQuery!.InvoicePayload)
            {
                Sender = GetSender(update)
            },

            UpdateType.PollAnswer => new TelegramInput(
                update.Type.ToString(),
                -1, // нет ChatId
                -1, // нет MessageId
                string.Join(",", update.PollAnswer!.OptionIds))
            {
                Sender = GetSender(update)
            },

            UpdateType.BusinessMessage => new TelegramInput(
                update.Type.ToString(),
                update.BusinessMessage!.Chat.Id,
                update.BusinessMessage.MessageId,
                update.BusinessMessage.Text ?? string.Empty)
            {
                Sender = GetSender(update)
            },

            UpdateType.EditedBusinessMessage => new TelegramInput(
                update.Type.ToString(),
                update.EditedBusinessMessage!.Chat.Id,
                update.EditedBusinessMessage.MessageId,
                update.EditedBusinessMessage.Text ?? string.Empty)
            {
                Sender = GetSender(update)
            },

            _ => throw new ArgumentOutOfRangeException(nameof(update.Type))
        };

    private static TelegramInputSender GetSender(this Update update)
    {
        return update.Type switch
        {
            UpdateType.Message =>
                new TelegramInputSender(update.Message!.From!.Id, update.Message!.From!.Username ?? ""),

            UpdateType.MessageReaction =>
                new TelegramInputSender(update.MessageReaction!.User!.Id, update.MessageReaction!.User!.Username ?? ""),

            UpdateType.CallbackQuery =>
                new TelegramInputSender(update.CallbackQuery!.From.Id, update.CallbackQuery!.From.Username ?? ""),

            UpdateType.EditedMessage =>
                new TelegramInputSender(update.Message!.From!.Id, update.Message!.From!.Username ?? ""),

            UpdateType.InlineQuery =>
                new TelegramInputSender(update.InlineQuery!.From.Id, update.InlineQuery!.From.Username ?? ""),

            UpdateType.ChosenInlineResult =>
                new TelegramInputSender(update.ChosenInlineResult!.From.Id,
                    update.ChosenInlineResult!.From.Username ?? ""),

            UpdateType.ShippingQuery =>
                new TelegramInputSender(update.ShippingQuery!.From.Id, update.ShippingQuery!.From.Username ?? ""),

            UpdateType.PreCheckoutQuery =>
                new TelegramInputSender(update.PreCheckoutQuery!.From.Id, update.PreCheckoutQuery!.From.Username ?? ""),

            UpdateType.PollAnswer =>
                new TelegramInputSender(update.PollAnswer!.User!.Id, update.PollAnswer!.User!.Username ?? ""),

            UpdateType.BusinessMessage =>
                new TelegramInputSender(update.BusinessMessage!.From!.Id, update.BusinessMessage!.From!.Username ?? ""),

            UpdateType.EditedBusinessMessage =>
                new TelegramInputSender(update.EditedBusinessMessage!.From!.Id,
                    update.EditedBusinessMessage!.From!.Username ?? ""),

            _ => throw new ArgumentOutOfRangeException(nameof(update.Type))
        };
    }

    private static string GetEmoji(this ReactionType[] reactionTypes)
    {
        return string.Join("", reactionTypes
            .Where(p => p.Type == ReactionTypeKind.Emoji)
            .Cast<ReactionTypeEmoji>()
            .Select(p => p.Emoji));
    }

    public static readonly Regex StartRegex = new(
        @"^/start(?:@LttfRatingBot)?\s*$",
        RegexOptions.Compiled);

    // /rating с 0 или 1 пользователем
    public static readonly Regex GetRatingRegex = new(
        @"^/rating(?:@LttfRatingBot)?(?:\s+(?<User>@[a-zA-Z0-9_]{1,32}))?\s*$",
        RegexOptions.Compiled);

    // /rating с 2 пользователями
    public static readonly Regex CompareRatingRegex = new(
        @"^/rating(?:@LttfRatingBot)?\s+(?<User1>@[a-zA-Z0-9_]{1,32})\s+(?<User2>@[a-zA-Z0-9_]{1,32})\s*$",
        RegexOptions.Compiled);

    public static readonly Regex RecalculateRatingRegex = new(
        @"^/recalculate(?:@LttfRatingBot)?\s*$",
        RegexOptions.Compiled);

    public static readonly Regex SetScoreRegex = new(
        @"^(?<User1>@[a-zA-Z0-9_]{1,32}\s+)?(?<User2>@[a-zA-Z0-9_]{1,32})\s+" +
        @"(?<Points1>25[0-5]|2[0-4][0-9]|1[0-9]{2}|[1-9]?[0-9])\s+" +
        @"(?<Points2>25[0-5]|2[0-4][0-9]|1[0-9]{2}|[1-9]?[0-9])" +
        @"(\s+(?<Length>25[0-5]|2[0-4][0-9]|1[0-9]{2}|[1-9]?[0-9]))?\s*$",
        RegexOptions.Compiled);

    public static CommandType GetCommandType(this TelegramInput value)
    {
        var input = value.Text.Trim(); // Убираем лишние пробелы

        if (string.IsNullOrWhiteSpace(input))
            return CommandType.Unknown;

        if (StartRegex.IsMatch(input))
            return CommandType.Start;

        if (GetRatingRegex.IsMatch(input))
            return CommandType.GetRating;

        if (CompareRatingRegex.IsMatch(input))
            return CommandType.CompareRating;

        if (RecalculateRatingRegex.IsMatch(input))
            return CommandType.RecalculateRating;

        if (SetScoreRegex.IsMatch(input))
            return CommandType.SetScore;

        if (value.UpdateType == UpdateType.MessageReaction.ToString() && input.TrimEnd().EndsWith("👎"))
            return CommandType.DeleteSet;

        return CommandType.Unknown;
    }
}