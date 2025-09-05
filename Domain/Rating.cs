namespace Domain;

public record Rating
{
    public float User { get; set; } = 1;
    public float Opponent { get; set; } = 1;
}