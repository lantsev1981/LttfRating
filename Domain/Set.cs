namespace Domain;

public record Set(byte Num, byte WonPoint, byte LostPoint, string WinnerLogin, long ChatId, int MessageId)
    : IComparable<Set>
{
    public Guid MatchId { get; init; }

    public DateTimeOffset Date { get; init; } = DateTimeOffset.UtcNow;

    public Match Match { get; init; } = null!;

    public byte Points => (byte)(WonPoint + LostPoint);

    public byte GetPoints(string login) => WinnerLogin == login ? WonPoint : LostPoint;

    public int CompareTo(Set? other)
    {
        return other is null ? 1 : Date.CompareTo(other.Date);
    }
}