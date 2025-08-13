namespace Common;

public class RequestHandlerLogger<TRequest, TResponse>(
    IRequestHandler<TRequest, TResponse> inner,
    ILogger<RequestHandlerLogger<TRequest, TResponse>> logger)
    : IRequestHandler<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    public async Task<TResponse> Handle(TRequest request, CancellationToken token)
    {
        var stopwatch = Stopwatch.StartNew();
        var handlerType = inner.GetType().Name;
        var typeName = typeof(TRequest).Name;

        try
        {
            logger.LogTrace("{Handler}.{Name}: начинаем обработку",
                handlerType, typeName);
            
            var result = await inner.Handle(request, token);
            stopwatch.Stop();
                
            logger.LogTrace("{Handler}.{Name}: обработано за {Elapsed} мс",
                handlerType, typeName, stopwatch.ElapsedMilliseconds);
            
            return result;
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

public class RequestHandlerLogger<TRequest>(
    IRequestHandler<TRequest> inner,
    ILogger<RequestHandlerLogger<TRequest>> logger)
    : IRequestHandler<TRequest>
    where TRequest : IRequest
{
    public async Task Handle(TRequest request, CancellationToken token)
    {
        var stopwatch = Stopwatch.StartNew();
        var handlerType = inner.GetType().Name;
        var typeName = typeof(TRequest).Name;

        try
        {
            logger.LogTrace("{Handler}.{Name}: начинаем обработку",
                handlerType, typeName);
            
            await inner.Handle(request, token);
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