namespace LttfRating;

public class UpdateMessageHandler(
    ErrorMessageHandler errorHandler,
    IMediator mediator,
    IOptions<ApiConfig> config,
    ILogger<UpdateMessageHandler> logger,
    IGamerStore store)
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
                var admin = await store.GetAdminGamerId(token);

                if (admin?.UserId is not null)
                {
                    await mediator.Send(new SendMessageCommand(admin.UserId.Value,
                        $"""
                         🆕 <b>НОВЫЙ ПОЛЬЗОВАТЕЛЬ</b>
                         ━━━━━━━━━━━━━━━━━━━

                         ├ ID: <code>{user.Id}</code>
                         ├ Логин: @{user.Username ?? "-"}
                         ├ Имя: {user.FirstName}
                         ├ Фамилия: {user.LastName ?? "-"}
                         └ Язык: {user.LanguageCode ?? "-"}
                         """), token);
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
                        case "/start@LttfRatingBot":
                        case "/start":

                            await mediator.Send(new SendMessageCommand(user.Id,
                                """
                                🤖 <b>Добро пожаловать в бот учёта рейтинга Lttf игроков в настольный теннис!</b>

                                📌 <b>Возможности бота:</b>
                                • Нажми /start что бы разрешить боту отправлять сообщения в личку
                                • Отправляйте результаты матча в формате:
                                  <code>@соперник 9 11</code>
                                  ☝️ тегни соперника и проставь результат партии, где 9 это твои очки, а 11 соперника
                                • Я автоматически учту результат и обновлю рейтинг
                                • Поставь 👎 на сообщении типа "<code>@соперник 9 11</code>" если не согласен с результами или они введены ошибочно
                                  ☝️ удалить результаты можно только в течении часа после их публикации
                                • Отправь /rating что бы получить совой рейтинг, место и статистику в личку 

                                Если есть вопросы — пишите <a href="https://t.me/lantsev1981">создатею</a>
                                Если хочешь поучаствовать в разработке или просто позырить <a href="https://github.com/lantsev1981/LttfRating">код</a>
                                Если хочешь поблагодарить <a href="https://www.tbank.ru/cf/1k4w2TmaoyE">кликай</a> или сканируй QR-code
                                """, "LttfRatingBotQr.jpg"), token);

                            break;
                        
                        case "/recalculate@LttfRatingBot":
                        case "/recalculate":
                            await mediator.Send(new RecalculateRatingMessageCommand(update.Message.From!.Username!), token);
                            break;
                        case "/rating@LttfRatingBot":
                        case "/rating":
                            var viewLogin = commandAndArg.Length == 2 ? commandAndArg[1].TrimStart('@') : null;
                            await mediator.Send(new SendRatingMessageCommand(update.Message, viewLogin), token);
                            break;
                        default:
                            await mediator.Send(new SetValueMessageCommand(update.Message), token);
                            break;
                    }

                    break;
                }
                case UpdateType.CallbackQuery:
                {
                    if (update.CallbackQuery!.Data is null || update.CallbackQuery.Message is null)
                    {
                        logger.LogTrace("Сообщение не содержит текст");
                        break;
                    }

                    logger.LogTrace("Пришло сообщение: {Text} от @{Username}",
                        update.CallbackQuery!.Data, user.Username);
                    
                    var commandAndArg = update.CallbackQuery.Data.Split(' ');
                    switch (commandAndArg[0])
                    {
                        case "/rating@LttfRatingBot":
                        case "/rating":
                            await mediator.Send(new SendRatingMessageCommand(update.CallbackQuery.Message!, commandAndArg.Length == 2 ? commandAndArg[1] : null), token);
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