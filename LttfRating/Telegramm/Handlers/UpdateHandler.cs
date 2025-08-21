namespace LttfRating;

public class UpdateHandler(
    IServiceProvider serviceProvider,
    IOptions<ApiConfig> config,
    ILogger<UpdateHandler> logger)
{
    private readonly ApiConfig _config = config.Value;

    public async Task HandleAsync(ITelegramBotClient botClient, Update update, CancellationToken token)
    {
        // Зарегистрированы как Singleton, а нужен на каждый запрос новый AppContext
        using var scope = serviceProvider.CreateScope();
        var store = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
        var errorHandler = scope.ServiceProvider.GetRequiredService<ErrorHandler>();
        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();
        
        try
        {
            var input = update.GetInput();

            if (input.Sender.Login is "")
            {
                logger.LogTrace("Отсутствует логин {UserId}", input.Sender.Id);
                
                var adminLinks = string.Join(", ", _config.Administrators.Select(admin =>
                    $"<a href=\"tg://user?id={admin}\">@{admin}</a>"));

                await mediator.Send(new SendMessageQuery(input.Sender.Id,
                    $"""
                     ⚠️ <b>Привет! Для работы с ботом необходимо указать в настройках профиля логин</b>

                     Логин - это ключ, по которому я веду учёт партий, без него ни как 😉

                     Если это ошибка — обратитесь к администратору:
                     {adminLinks}
                     """, "LoginSettings.jpg"), token);

                return;
            }
            
            // Сохраняем запрос если он соответствует одному из паттернов
            // обработка запросов будет осуществляться в TelegramInputBackgroundService
            if (input.Text.GetCommandType() != CommandType.Unknown)
            {
                logger.LogTrace("Пришло сообщение: {Text} от @{Username}",
                    input.Text, input.Sender.Login);
                await store.TelegramInputStore.AddAsync(input, token);
            }
        }
        catch (Exception ex)
        {
            await errorHandler.HandleAsync(botClient, ex, token);
        }
    }
}