namespace Domain;

public record Match(byte SetWonCount = 3)
{
    public Guid Id { get; init; }
    public bool IsPending { get; set; } = true;
    public required List<Gamer> Gamers { get; init; }
    public SortedSet<Set> Sets { get; init; } = [];

    public Gamer GetLastWinner() =>
        Gamers.Single(p => p.Login == Sets.Last().WinnerLogin);

    public Gamer GetLastLoser() =>
        Gamers.Single(p => p.Login != Sets.Last().WinnerLogin);

    public DateTimeOffset Date =>
        Sets.LastOrDefault()?.Date ?? DateTimeOffset.MinValue;
}