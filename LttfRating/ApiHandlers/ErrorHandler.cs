namespace LttfRating;

public class ErrorHandler(
    ILogger<ErrorHandler> logger,
    IOptions<ApiConfig> config,
    IMediator mediator,
    IGamerStore store)
{
    private readonly ApiConfig _config = config.Value;

    public async Task HandleAsync(ITelegramBotClient botClient, Exception error, CancellationToken token)
    {
        var errorMessage = error switch
        {
            ApiRequestException apiRequestException
                => $"Telegram API Error:\n[{apiRequestException.ErrorCode}]\n{apiRequestException.Message}",
            _ => error.Message
        };

        logger.LogError(error, "[Ошибка] {ErrorMessage}", errorMessage);
        
        var admin = await store.GetAdminGamerId(token);
        if (admin?.UserId is null)
            return;

        await mediator.Send(new SendMessageCommand(admin.UserId.Value,
            $"""
             ‼️ <b>КРИТИЧЕСКАЯ ОШИБКА</b> ‼️
             ━━━━━━━━━━━━━━━━━━━

             ⚠️ <b>Тип ошибки:</b> <code>{error.GetType().Name}</code>

             📝 <b>Сообщение:</b>
             <pre>{error.Message}</pre>
             """), token);
    }
}