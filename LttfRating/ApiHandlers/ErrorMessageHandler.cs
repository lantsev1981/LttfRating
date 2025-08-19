namespace LttfRating;

public class ErrorMessageHandler(
    IServiceProvider serviceProvider,
    ILogger<ErrorMessageHandler> logger)
{
    public async Task HandleAsync(ITelegramBotClient botClient, Exception error, CancellationToken token)
    {
        // Зарегистрированы как Singleton, а нужен на каждый запрос новый AppContext
        using var scope = serviceProvider.CreateScope();
        var store = scope.ServiceProvider.GetRequiredService<IGamerStore>();
        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();
        
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
             <pre>{EscapeHtml(errorMessage)}</pre>
             """), token);
    }

    private static string EscapeHtml(string text)
    {
        if (string.IsNullOrEmpty(text))
            return text;

        return text
            .Replace("&", "&amp;")
            .Replace("<", "&lt;")
            .Replace(">", "&gt;")
            .Replace("\"", "&quot;")
            .Replace("'", "&apos;");
    }
}