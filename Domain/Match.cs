namespace Domain;

public record Match(byte SetWonCount = 3)
{
    public Guid Id { get; init; }
    public required List<Gamer> Gamers { get; init; }
    public SortedSet<Set> Sets { get; init; } = [];

    public Gamer Opponent(Gamer sender) =>
        Gamers.Single(p => p != sender);

    public Gamer LastWinner =>
        Gamers.Single(p => p.Login == Sets.Last().WinnerLogin);

    public Gamer LastLoser =>
        Gamers.Single(p => p.Login != Sets.Last().WinnerLogin);

    public int WinnerSetCount =>
        Sets.Count(p => p.WinnerLogin == LastWinner.Login);
    public int LoserSetCount =>
        Sets.Count(p => p.WinnerLogin == LastLoser.Login);

    public DateTimeOffset? Date { get; set; }
}