namespace Domain;

public record Gamer(string Login)
{
    public float Rating { get; set; }
    public List<Match> Matches { get; init; } = [];
    
    public float OldRating  { get; set; }
}