namespace Domain;

public record Gamer(string Login)
{
    public float Rating { get; set; } = 1;
    
    public List<Match> Matches { get; init; } = [];

    public long? UserId { get; set; }

    public DateTimeOffset? LastSendStatistics { get; set; }
}