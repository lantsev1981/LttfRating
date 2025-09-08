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

        var items = await store.TelegramInputStore.GetItems(token);

        foreach (var item in items)
        {
            try
            {
                await DoItem(mediator, store, item, token);
            }
            catch (Exception e)
            {
                logger.LogError(e,
                    "Сообщение {@Input}: ошибка обработки {@Message}",
                    item, e.GetAllMessages());

                var messageText = e switch
                {
                    ValidationException =>
                        $"""
                         🤬 Ошибка обработки запроса
                         
                           {e.Message}
                         """,
                    ApiRequestException exp =>
                        $"""
                         🤬 Telegram API Error [{exp.HttpStatusCode}]
                         
                           {e.Message}
                         """,
                    _ => null
                };

                if (!string.IsNullOrWhiteSpace(messageText))
                {
                    await mediator.Send(new SendMessageQuery(item.ChatId,
                        messageText, MessageId: item.MessageId, DisableNotification: false), token);
                }

                await mediator.Send(new SendMessageQuery(item.ChatId,
                    "🤬", MessageId: item.MessageId, DisableNotification: false), token);
            }

            await store.TelegramInputStore.DeleteItem(item, token);
        }

        return _config.TelegramInputInterval;
    }

    private async Task DoItem(
        IMediator mediator,
        IUnitOfWork store,
        TelegramInput item,
        CancellationToken token)
    {
        logger.LogTrace("Обрабатываем сообщение: {Text} от @{Username}",
            item.Text, item.Sender.Login);
        
        await mediator.Send(new SendMessageQuery(item.ChatId,
            "👨‍💻", MessageId: item.MessageId), token);

        // добавляем пользователя
        if (await mediator.Send(new AddGamerCommand(item.Sender.Login, item.Sender.Id), token))
        {
            var admin = await store.GamerStore.GetAdminGamerId(token);

            if (admin?.UserId is not null)
            {
                await mediator.Send(new SendMessageQuery(admin.UserId.Value,
                    $"""
                     🆕 <b>НОВЫЙ ПОЛЬЗОВАТЕЛЬ</b>
                     ━━━━━━━━━━━━━━━━━━━

                     ├ ID: <code>{item.Sender.Id}</code>
                     └ Логин: @{item.Sender.Login}
                     """, DisableNotification: false), token);

                // ├ Имя: {data.User.BaseUser.FirstName}
                // ├ Фамилия: {data.User.BaseUser.LastName ?? "-"}
                // └ Язык: {data.User.BaseUser.LanguageCode ?? "-"}
            }
        }

        var commandType = item.GetCommandType();
        switch (commandType)
        {
            case CommandType.Start:
                await mediator.Send(new SendMessageQuery(item.Sender.Id,
                    """
                    🤖 <b>Добро пожаловать в бот учёта рейтинга Lttf игроков в настольный теннис!</b>

                    📌 <b>Возможности бота:</b>
                    • Нажми /start что бы разрешить боту отправлять сообщения в личку
                    • Отправляйте результаты матча в формате:
                      <code>@соперник 9 11</code>
                      ☝️ тегни соперника и проставь результат партии (⚔️), где 9 это твои очки (●), а 11 соперника
                      ☝️ в конце запроса можно добавить требуемое количество побед в матче (по умолчанию до 3 побед)
                    • Я автоматически учту результат и обновлю рейтинг

                    Если есть вопросы — пишите <a href="https://t.me/lantsev1981">создатею</a>
                    Если хочешь поучаствовать в разработке или просто позырить <a href="https://github.com/lantsev1981/LttfRating">код</a>
                    Если хочешь поблагодарить <a href="https://www.tbank.ru/cf/1k4w2TmaoyE">кликай</a> или сканируй QR-code
                    """, FileName: "LttfRatingBotQr.jpg", DisableNotification: false), token);
                break;
            case CommandType.GetRating:
                await mediator.Send(new SendRatingQuery(item, false), token);
                break;
            case CommandType.CompareRating:
                await mediator.Send(new SendCompareQuery(item), token);
                break;
            case CommandType.RecalculateRating:
                await mediator.Send(new RecalculateRatingCommand(item), token);
                break;
            case CommandType.SetScore:
                await mediator.Send(new SetScoreCommand(item), token);
                break;
            case CommandType.DeleteSet:
                await mediator.Send(new DeleteSetCommand(item), token);
                break;
        }

        await mediator.Send(new SendMessageQuery(item.ChatId,
            "👍", MessageId: item.MessageId), token);
    }
}