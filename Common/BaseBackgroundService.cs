namespace Common;

public abstract class BaseBackgroundService(
    ILogger<BaseBackgroundService> logger)
    : BackgroundService
{
    private static bool _isRun;
    private static readonly Random Random = new();

    protected override async Task ExecuteAsync(CancellationToken token)
    {
        logger.LogTrace("{FirstName} запущен!", GetType().Name);
        
        while (!token.IsCancellationRequested)
        {
            // время ожидание в случае одновременного срабатывания или ошибки 1-2 секунды
            var delay = TimeSpan.FromMilliseconds(Random.Next(1000) + 1000);

            if (_isRun)
            {
                logger.LogWarning(
                    "{Type}: другой процесс уже работает, ожидаем {Milliseconds}мс",
                    GetType().Name,
                    delay.TotalMilliseconds);

                await Task.Delay(delay, token);

                continue;
            }

            try
            {
                _isRun = true;
                delay = await Do(token);
            }
            catch (OperationCanceledException)
            {
                logger.LogWarning("{Type}: операция была отменена", GetType().Name);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "{Type}: ошибка фонового процесса", GetType().Name);
                delay = TimeSpan.FromMinutes(1);
            }

            _isRun = false;
            await Task.Delay(delay, token);
        }
    }

    protected abstract Task<TimeSpan> Do(CancellationToken token);
}