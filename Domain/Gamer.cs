namespace Domain;

public record Gamer(string Login)
{
    public float Rating { get; set; } = 1;
    
    public List<Match> Matches { get; init; } = [];
    
    public float OldRating  { get; set; }

    public long? UserId { get; set; }
}