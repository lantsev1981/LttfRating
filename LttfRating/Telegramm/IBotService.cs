namespace LttfRating;

internal interface IBotService
{
    Task StartAsync(CancellationToken token);
}