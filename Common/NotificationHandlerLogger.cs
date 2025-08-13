namespace Common;

public class NotificationHandlerLogger<TNotification>(
    INotificationHandler<TNotification> inner,
    ILogger<NotificationHandlerLogger<TNotification>> logger)
    : INotificationHandler<TNotification>
    where TNotification : INotification
{
    public async Task Handle(TNotification notification, CancellationToken token)
    {
        var stopwatch = Stopwatch.StartNew();
        var handlerType = inner.GetType().Name;
        var typeName = typeof(TNotification).Name;

        try
        {
            logger.LogTrace("{Handler}.{Name}: начинаем обработку",
                handlerType, typeName);
            
            await inner.Handle(notification, token);
            stopwatch.Stop();
                
            logger.LogTrace("{Handler}.{Name}: обработано за {Elapsed} мс",
                handlerType, typeName, stopwatch.ElapsedMilliseconds);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "{Handler}.{Name}: ошибка {@Message}",
                handlerType, typeName, ex.GetAllMessages());
                
            stopwatch.Stop();
                
            throw;
        }
    }
}