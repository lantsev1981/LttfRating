namespace LttfRating;

public class ApiConfig
{
    public string[] Administrators { get; init; } = [];
    public TimeSpan TelegramInputInterval { get; init; } = TimeSpan.FromSeconds(2);
}