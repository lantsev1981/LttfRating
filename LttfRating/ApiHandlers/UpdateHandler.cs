namespace LttfRating;

public class UpdateHandler(
    ErrorHandler errorHandler,
    IMediator mediator,
    IOptions<ApiConfig> config,
    ILogger<UpdateHandler> logger,
    IDomainStore<Gamer> store)
{
    private readonly ApiConfig _config = config.Value;

    public async Task HandleAsync(ITelegramBotClient botClient, Update update, CancellationToken token)
    {
        try
        {
            var user = GetUser(update);

            if (user.Username is null)
            {
                logger.LogTrace("Отсутствует логин {UserId}", user.Id);

                await mediator.Send(new SendMessageCommand(user.Id,
                    $"""
                     ⚠️ <b>Привет! Для работы с ботом необходимо указать в настройках профиля логин</b>

                     Логин - это ключ, по которому я веду учёт партий, без него ни как 😉

                     Если это ошибка — обратитесь к администратору:
                     {string.Join(", ", _config.Administrators.Select(admin => $"<a href=\"tg://user?id={admin}\">@{admin}</a>"))}
                     """, "LoginSettings.jpg"), token);

                return;
            }

            if (await mediator.Send(new AddGamerCommand(user.Username, user.Id), token))
            {
                var adminLogin = _config.Administrators.FirstOrDefault();
                if (adminLogin is not null)
                {
                    var admin = await store.GetByKey(adminLogin, token);
                    if (admin?.UserId is not null)
                    {
                        await mediator.Send(new SendMessageCommand(admin.UserId.Value,
                            $"""
                             🆕 <b>НОВЫЙ ПОЛЬЗОВАТЕЛЬ</b>
                             ━━━━━━━━━━━━━━━━━━━

                             👤 <b>Основная информация:</b>
                             ├ ID: <code>{user.Id}</code>
                             ├ Логин: @{user.Username ?? "-"}
                             ├ Имя: {user.FirstName}
                             ├ Фамилия: {user.LastName ?? "-"}
                             └ Язык: {user.LanguageCode ?? "-"}
                             """), token);
                    }
                }
            }

            switch (update.Type)
            {
                case UpdateType.MessageReaction:
                {
                    await mediator.Send(new DeleteSetCommand(update.MessageReaction!), token);

                    break;
                }
                case UpdateType.Message:
                {
                    if (update.Message!.Text is null)
                    {
                        logger.LogTrace("Сообщение не содержит текст");
                        break;
                    }

                    logger.LogTrace("Пришло сообщение: {Text} от @{Username}",
                        update.Message.Text, user.Username);

                    var commandAndArg = update.Message.Text.Split(' ');
                    switch (commandAndArg[0])
                    {
                        case "/help":
                            
                            await mediator.Send(new SendMessageCommand(update.Message.From!.Id,
                                """
                                🤖 <b>Добро пожаловать в бот учёта рейтинга Lttf игроков в настольный теннис!</b>

                                📌 <b>Возможности бота:</b>
                                • Отправляйте результаты матча в формате:
                                  <code>@соперник 9 11</code>
                                  ☝️ тегни соперника и проставь результат партии, где 9 это твои очки, а 11 соперника
                                  ☝️ добавить результаты может только участник партии или администратор
                                • Я автоматически учту результат и обновлю рейтинг

                                Если есть вопросы — пишите <a href="https://t.me/lantsev1981">создатею</a>
                                Если хочешь поучаствовать в разработке или просто позырить <a href="https://github.com/lantsev1981/LttfRating">код</a>
                                Если хочешь поблагодарить <a href="https://www.tbank.ru/cf/1k4w2TmaoyE">кликай</a> или сканируй QR-code
                                """, "LttfRatingBotQr.jpg"), token);
                            
                            break;
                        default:
                            await mediator.Send(new SetUpdateMessageCommand(update.Message), token);
                            break;
                    }

                    break;
                }

                // Другие типы можно добавить позже
                default:
                    logger.LogTrace("Необработанный тип сообщения: {Type}", update.Type);
                    break;
            }
        }
        catch (Exception ex)
        {
            await errorHandler.HandleAsync(botClient, ex, token);
        }
    }

    private User GetUser(Update update)
    {
        User? user = update.Type switch
        {
            UpdateType.Message => update.Message?.From,
            UpdateType.EditedMessage => update.EditedMessage?.From,
            UpdateType.MessageReaction => update.MessageReaction?.User,
            UpdateType.CallbackQuery => update.CallbackQuery?.From,
            UpdateType.InlineQuery => update.InlineQuery?.From,
            UpdateType.ChosenInlineResult => update.ChosenInlineResult?.From,
            UpdateType.ShippingQuery => update.ShippingQuery?.From,
            UpdateType.PreCheckoutQuery => update.PreCheckoutQuery?.From,
            UpdateType.PollAnswer => update.PollAnswer?.User,
            UpdateType.BusinessMessage => update.BusinessMessage?.From,
            UpdateType.EditedBusinessMessage => update.EditedBusinessMessage?.From,

            _ => throw new ArgumentOutOfRangeException(nameof(update.Type))
        };

        return user ?? throw new ArgumentNullException(
            paramName: nameof(user),
            message: $"User not found in {update.Type} update");
    }
}