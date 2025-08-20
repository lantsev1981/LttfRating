namespace LttfRating;

public class ErrorMessageHandler(
    IServiceProvider serviceProvider,
    ILogger<ErrorMessageHandler> logger)
{
    public async Task HandleAsync(ITelegramBotClient botClient, Exception error, CancellationToken token)
    {
        // Зарегистрированы как Singleton, а нужен на каждый запрос новый AppContext
        using var scope = serviceProvider.CreateScope();
        var store = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();

        string errorMessage = error switch
        {
            ApiRequestException apiRequestException
                => $"Telegram API Error:\n[{apiRequestException.ErrorCode}]\n{string.Join(',', apiRequestException.GetAllMessages())}",
            _ => string.Join(',', error.GetAllMessages())
        };

        logger.LogError(error, "[Ошибка] {ErrorMessage}", errorMessage);

        var admin = await store.GameStore.GetAdminGamerId(token);
        if (admin?.UserId is null)
            return;

        await mediator.Send(new SendMessageCommand(admin.UserId.Value,
            $"""
             ‼️ <b>КРИТИЧЕСКАЯ ОШИБКА</b> ‼️
             ━━━━━━━━━━━━━━━━━━━

             ⚠️ <b>Тип ошибки:</b> <code>{error.GetType().Name}</code>

             📝 <b>Сообщение:</b>
             <pre>{errorMessage.EscapeHtml()}</pre>
             """), token);
    }
}