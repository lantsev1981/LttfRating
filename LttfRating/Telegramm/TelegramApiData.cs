namespace LttfRating;

public record TelegramApiData(long ChatId, int MessageId, TelegramApiUser User, string Text);