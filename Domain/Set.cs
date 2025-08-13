namespace Domain;

public record Set(byte Num, byte WonPoint, byte LostPoint, string WinnerLogin)
{
    public Guid MatchId { get; init; }
    public Match Match { get; init; } = null!;
    public byte Points => (byte)(WonPoint + LostPoint);
    public DateTimeOffset Date { get; init; } = DateTimeOffset.UtcNow;
    public byte GetPoints(string login) => WinnerLogin == login ? WonPoint : LostPoint;
}