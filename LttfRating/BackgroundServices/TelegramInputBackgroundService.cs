namespace LttfRating;

public class TelegramInputBackgroundService(
    ILogger<BaseBackgroundService> baseLogger,
    ILogger<TelegramInputBackgroundService> logger,
    IServiceProvider serviceProvider,
    IOptions<ApiConfig> config)
    : BaseBackgroundService(baseLogger)
{
    private readonly ApiConfig _config = config.Value;

    protected override async Task<TimeSpan> Do(CancellationToken token)
    {
        using var scope = serviceProvider.CreateScope();
        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();
        var store = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

        var inputs = await store.TelegramInputStore.GetItems(token);
        foreach (var input in inputs)
        {
            try
            {
                await DoItem(mediator, store, input, token);
            }
            catch (ValidationException e)
            {
                logger.LogError(
                    e,
                    """
                    Сообщение {@Input}: ошибка обработки ({Message})
                    """,
                    input,
                    e.GetAllMessages());
                
                await SendValidationError(mediator, input.ChatId, input.Sender.Login, e.Message, token);
            }
            catch (Exception e)
            {
                logger.LogError(
                    e,
                    """
                    Сообщение {@Input}: ошибка обработки ({Message})
                    """,
                    input,
                    e.GetAllMessages());
            }
                
            await store.TelegramInputStore.DeleteItem(input, token);
        }

        return _config.TelegramInputInterval;
    }

    private async Task DoItem(
        IMediator mediator,
        IUnitOfWork store,
        TelegramInput input,
        CancellationToken token)
    {
        logger.LogTrace("Обрабатываем сообщение: {Text} от @{Username}",
            input.Text, input.Sender.Login);
        
        // добавляем пользователя
        if (await mediator.Send(new AddGamerCommand(input.Sender.Login, input.Sender.Id), token))
        {
            var admin = await store.GameStore.GetAdminGamerId(token);

            if (admin?.UserId is not null)
            {
                await mediator.Send(new SendMessageQuery(admin.UserId.Value,
                    $"""
                     🆕 <b>НОВЫЙ ПОЛЬЗОВАТЕЛЬ</b>
                     ━━━━━━━━━━━━━━━━━━━

                     ├ ID: <code>{input.Sender.Id}</code>
                     ├ Логин: @{input.Sender.Login}
                     """), token);

                // ├ Имя: {data.User.BaseUser.FirstName}
                // ├ Фамилия: {data.User.BaseUser.LastName ?? "-"}
                // └ Язык: {data.User.BaseUser.LanguageCode ?? "-"}
            }
        }

        var commandType = input.GetCommandType();
        switch (commandType)
        {
            case CommandType.Start:
                await mediator.Send(new SendMessageQuery(input.Sender.Id,
                    """
                    🤖 <b>Добро пожаловать в бот учёта рейтинга Lttf игроков в настольный теннис!</b>

                    📌 <b>Возможности бота:</b>
                    • Нажми /start что бы разрешить боту отправлять сообщения в личку
                    • Отправляйте результаты матча в формате:
                      <code>@соперник 9 11</code>
                      ☝️ тегни соперника и проставь результат партии, где 9 это твои очки, а 11 соперника
                      ☝️ в конце запроса можно добавить требуемое количество побед в матче (по умолчанию до 3 побед)
                    • Я автоматически учту результат и обновлю рейтинг
                    • Поставь 👎 на сообщении типа <code>@соперник 9 11</code> если не согласен с результатами или они введены ошибочно
                      ☝️ удалить результаты можно только в течении часа после их публикации
                    • Отправь /rating что бы получить совой рейтинг, место и статистику
                    • Отправь <code>/rating @игрок</code> что бы получить рейтинг, место и статистику другого игрока
                    • Отправь <code>/rating @игрок1 @игрок2</code> что бы получить статистику между игроками

                    Если есть вопросы — пишите <a href="https://t.me/lantsev1981">создатею</a>
                    Если хочешь поучаствовать в разработке или просто позырить <a href="https://github.com/lantsev1981/LttfRating">код</a>
                    Если хочешь поблагодарить <a href="https://www.tbank.ru/cf/1k4w2TmaoyE">кликай</a> или сканируй QR-code
                    """, FileName: "LttfRatingBotQr.jpg"), token);
                break;
            case CommandType.GetRating:
                await mediator.Send(new SendRatingQuery(input), token);
                break;
            case CommandType.CompareRating:
                await mediator.Send(new SendCompareQuery(input), token);   
                break;
            case CommandType.RecalculateRating:
                await mediator.Send(new RecalculateRatingCommand(input), token);
                break;
            case CommandType.SetScore:
                await mediator.Send(new SetScoreCommand(input), token);
                break;
            case CommandType.DeleteSet:
                await mediator.Send(new DeleteSetCommand(input), token);
                break;
        }
    }
        
    async Task SendValidationError(IMediator mediator, long chatId, string username, string errorMessage, CancellationToken token)
    {
        var adminLinks = string.Join(", ", _config.Administrators.Select(admin =>
            $"<a href=\"tg://user?id={admin}\">@{admin}</a>"));

        await mediator.Send(new SendMessageQuery(chatId,
            $"""
             ⚠️ @{username}, {errorMessage}.

             Если это ошибка — обратитесь к администратору:
             {adminLinks}
             """), token);
    }
}