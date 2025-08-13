namespace Domain;

public record GamerMatch
{
    public required string Login { get; init; }
    public required Guid MatchId { get; init; }
    public required Gamer Gamer { get; init; }
    public required Match Match { get; init; }
}