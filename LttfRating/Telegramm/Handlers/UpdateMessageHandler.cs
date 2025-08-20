namespace LttfRating;

public class UpdateMessageHandler(
    IServiceProvider serviceProvider,
    IOptions<ApiConfig> config,
    ILogger<UpdateMessageHandler> logger)
{
    private readonly ApiConfig _config = config.Value;

    public async Task HandleAsync(ITelegramBotClient botClient, Update update, CancellationToken token)
    {
        // Зарегистрированы как Singleton, а нужен на каждый запрос новый AppContext
        using var scope = serviceProvider.CreateScope();
        var store = scope.ServiceProvider.GetRequiredService<IGamerStore>();
        var errorHandler = scope.ServiceProvider.GetRequiredService<ErrorMessageHandler>();
        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();
        
        try
        {
            var data = update.GetData();

            if (data.User.Login is "")
            {
                logger.LogTrace("Отсутствует логин {UserId}", data.User.Id);

                await mediator.Send(new SendMessageCommand(data.User.Id,
                    $"""
                     ⚠️ <b>Привет! Для работы с ботом необходимо указать в настройках профиля логин</b>

                     Логин - это ключ, по которому я веду учёт партий, без него ни как 😉

                     Если это ошибка — обратитесь к администратору:
                     {string.Join(", ", _config.Administrators.Select(admin => $"<a href=\"tg://user?id={admin}\">@{admin}</a>"))}
                     """, "LoginSettings.jpg"), token);

                return;
            }

            if (await mediator.Send(new AddGamerCommand(data.User.Login, data.User.Id), token))
            {
                var admin = await store.GetAdminGamerId(token);

                if (admin?.UserId is not null)
                {
                    await mediator.Send(new SendMessageCommand(admin.UserId.Value,
                        $"""
                         🆕 <b>НОВЫЙ ПОЛЬЗОВАТЕЛЬ</b>
                         ━━━━━━━━━━━━━━━━━━━

                         ├ ID: <code>{data.User.BaseUser.Id}</code>
                         ├ Логин: @{data.User.BaseUser.Username ?? "-"}
                         ├ Имя: {data.User.BaseUser.FirstName}
                         ├ Фамилия: {data.User.BaseUser.LastName ?? "-"}
                         └ Язык: {data.User.BaseUser.LanguageCode ?? "-"}
                         """), token);
                }
            }

            switch (update.Type)
            {
                case UpdateType.MessageReaction:
                {
                    await mediator.Send(new DeleteSetCommand(data), token);
                    break;
                }
                case UpdateType.CallbackQuery:
                case UpdateType.Message:
                {
                    if (string.IsNullOrWhiteSpace(data.Text))
                    {
                        logger.LogTrace("Сообщение не содержит текст");
                        break;
                    }

                    logger.LogTrace("Пришло сообщение: {Text} от @{Username}",
                        data.Text, data.User.Login);

                    var commandAndArg = data.Text.Split(' ');
                    switch (commandAndArg[0])
                    {
                        case "/start@LttfRatingBot":
                        case "/start":
                        {
                            await mediator.Send(new SendMessageCommand(data.User.Id,
                                """
                                🤖 <b>Добро пожаловать в бот учёта рейтинга Lttf игроков в настольный теннис!</b>

                                📌 <b>Возможности бота:</b>
                                • Нажми /start что бы разрешить боту отправлять сообщения в личку
                                • Отправляйте результаты матча в формате:
                                  <code>@соперник 9 11</code>
                                  ☝️ тегни соперника и проставь результат партии, где 9 это твои очки, а 11 соперника
                                • Я автоматически учту результат и обновлю рейтинг
                                • Поставь 👎 на сообщении типа <code>@соперник 9 11</code> если не согласен с результатами или они введены ошибочно
                                  ☝️ удалить результаты можно только в течении часа после их публикации
                                • Отправь /rating что бы получить совой рейтинг, место и статистику
                                • Отправь <code>/rating @игрок</code> что бы получить рейтинг, место и статистику другого игрока
                                • Отправь <code>/rating @игрок1 @игрок2</code> что бы получить статистику между игроками

                                Если есть вопросы — пишите <a href="https://t.me/lantsev1981">создатею</a>
                                Если хочешь поучаствовать в разработке или просто позырить <a href="https://github.com/lantsev1981/LttfRating">код</a>
                                Если хочешь поблагодарить <a href="https://www.tbank.ru/cf/1k4w2TmaoyE">кликай</a> или сканируй QR-code
                                """, "LttfRatingBotQr.jpg"), token);

                            break;
                        }
                        case "/recalculate@LttfRatingBot":
                        case "/recalculate":
                        {
                            await mediator.Send(new RecalculateRatingMessageCommand(data), token);
                            break;
                        }
                        case "/rating@LttfRatingBot":
                        case "/rating":
                        {
                            switch (commandAndArg.Length)
                            {
                                case 3:
                                    await mediator.Send(new SendCompareMessageCommand(data), token);
                                    break;
                                default:
                                    await mediator.Send(new SendRatingMessageCommand(data), token);
                                    break;
                            }

                            break;
                        }
                        default:
                        {
                            await mediator.Send(new SetValueMessageCommand(data), token);
                            break;
                        }
                    }

                    break;
                }

                // Другие типы можно добавить позже
                default:
                {
                    logger.LogTrace("Необработанный тип сообщения: {Type}", update.Type);
                    break;
                }
            }
        }
        catch (Exception ex)
        {
            await errorHandler.HandleAsync(botClient, ex, token);
        }
    }
}