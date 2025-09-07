namespace LttfRating;

public class UpdateHandler(
    IServiceProvider serviceProvider,
    ILogger<UpdateHandler> logger,
    ErrorHandler errorHandler)
{

    public async Task HandleAsync(ITelegramBotClient botClient, Update update, CancellationToken token)
    {
        // Зарегистрированы как Singleton, а нужен на каждый запрос новый AppContext
        using var scope = serviceProvider.CreateScope();
        var store = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();
        
        try
        {
            var input = update.GetInput();

            if (input.Sender.Login is "")
            {
                logger.LogTrace("Отсутствует логин {UserId}", input.Sender.Id);

                await mediator.Send(new SendMessageQuery(input.Sender.Id,
                    $"""
                     ⚠️ <b>Привет! Для работы с ботом необходимо указать в настройках профиля логин</b>

                     Логин - это ключ, по которому я веду учёт партий (⚔️), без него ни как 😉
                     """, FileName: "LoginSettings.jpg"), token);

                return;
            }
            
            // Сохраняем запрос если он соответствует одному из паттернов
            // обработка запросов будет осуществляться в TelegramInputBackgroundService
            if (input.GetCommandType() != CommandType.Unknown)
            {
                logger.LogTrace("Пришло сообщение: {Text} от @{Username}",
                    input.Text, input.Sender.Login);
                
                await store.TelegramInputStore.AddAsync(input, token);
                
                await mediator.Send(new SendMessageQuery(input.ChatId,
                    "🫡", MessageId: input.MessageId), token);
            }
        }
        catch (Exception ex)
        {
            await errorHandler.HandleAsync(botClient, ex, token);
        }
    }
}