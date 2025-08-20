namespace LttfRating;

public static class TelegramApiExtensions
{
    public static TelegramApiData GetData(this Update update)
        => update.Type switch
        {
            UpdateType.Message => new TelegramApiData(
                update.Message!.Chat.Id,
                update.Message.MessageId,
                GetUser(update),
                update.Message.Text ?? string.Empty),

            UpdateType.MessageReaction => new TelegramApiData(
                update.MessageReaction!.Chat.Id,
                update.MessageReaction.MessageId,
                GetUser(update),
                update.MessageReaction!.NewReaction.GetEmoji()),

            UpdateType.CallbackQuery => new TelegramApiData(
                update.CallbackQuery!.Message!.Chat.Id,
                update.CallbackQuery.Message.MessageId,
                GetUser(update),
                update.CallbackQuery.Data!),

            UpdateType.EditedMessage => new TelegramApiData(
                update.EditedMessage!.Chat.Id,
                update.EditedMessage.MessageId,
                GetUser(update),
                update.EditedMessage.Text ?? string.Empty),

            UpdateType.InlineQuery => new TelegramApiData(
                -1, // нет ChatId для InlineQuery
                -1, // нет MessageId
                GetUser(update),
                update.InlineQuery!.Query),

            UpdateType.ChosenInlineResult => new TelegramApiData(
                -1, // нет ChatId
                -1, // нет MessageId
                GetUser(update),
                update.ChosenInlineResult!.ResultId),

            UpdateType.ShippingQuery => new TelegramApiData(
                -1, // нет ChatId
                -1, // нет MessageId
                GetUser(update),
                update.ShippingQuery!.InvoicePayload),

            UpdateType.PreCheckoutQuery => new TelegramApiData(
                -1, // нет ChatId
                -1, // нет MessageId
                GetUser(update),
                update.PreCheckoutQuery!.InvoicePayload),

            UpdateType.PollAnswer => new TelegramApiData(
                -1, // нет ChatId
                -1, // нет MessageId
                GetUser(update),
                string.Join(",", update.PollAnswer!.OptionIds)),

            UpdateType.BusinessMessage => new TelegramApiData(
                update.BusinessMessage!.Chat.Id,
                update.BusinessMessage.MessageId,
                GetUser(update),
                update.BusinessMessage.Text ?? string.Empty),

            UpdateType.EditedBusinessMessage => new TelegramApiData(
                update.EditedBusinessMessage!.Chat.Id,
                update.EditedBusinessMessage.MessageId,
                GetUser(update),
                update.EditedBusinessMessage.Text ?? string.Empty),

            _ => throw new ArgumentOutOfRangeException(nameof(update.Type))
        };

    private static TelegramApiUser GetUser(this Update update)
    {
        return update.Type switch
        {
            UpdateType.Message =>
                new TelegramApiUser(update.Message!.From!),

            UpdateType.MessageReaction =>
                new TelegramApiUser(update.MessageReaction!.User!),

            UpdateType.CallbackQuery =>
                new TelegramApiUser(update.CallbackQuery!.From!),

            UpdateType.EditedMessage =>
                new TelegramApiUser(update.Message!.From!),

            UpdateType.InlineQuery =>
                new TelegramApiUser(update.InlineQuery!.From),

            UpdateType.ChosenInlineResult =>
                new TelegramApiUser(update.ChosenInlineResult!.From),

            UpdateType.ShippingQuery =>
                new TelegramApiUser(update.ShippingQuery!.From),

            UpdateType.PreCheckoutQuery =>
                new TelegramApiUser(update.PreCheckoutQuery!.From),

            UpdateType.PollAnswer =>
                new TelegramApiUser(update.PollAnswer!.User!),

            UpdateType.BusinessMessage =>
                new TelegramApiUser(update.BusinessMessage!.From!),

            UpdateType.EditedBusinessMessage =>
                new TelegramApiUser(update.EditedBusinessMessage!.From!),

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
}