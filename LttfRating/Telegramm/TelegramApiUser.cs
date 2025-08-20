namespace LttfRating;

public record TelegramApiUser(User BaseUser)
{
    public long Id { get; init; } = BaseUser.Id;
    public string Login { get; init; } = BaseUser.Username ?? "";
}