namespace LttfRating;

public record HelpBotCommand(Message Message) : IRequest;

public class HelpBotHandler(
    ITelegramBotClient botClient)
    : IRequestHandler<HelpBotCommand>
{
    public async Task Handle(HelpBotCommand request, CancellationToken token)
    {
        await using var stream = File.OpenRead("LttfRatingBotQr.jpg");
        await botClient.SendPhoto(
            chatId: request.Message.Chat.Id,
            photo: new InputFileStream(stream),
            parseMode: ParseMode.Html,
            caption:"""
                    🤖 <b>Добро пожаловать в бот учёта рейтинга Lttf игроков в настольный теннис!</b>

                    📌 <b>Возможности бота:</b>
                    • Отправляйте результаты матча в формате:
                      <code>@игрок2 9 11</code>
                      ☝️ тегни оппонента и проставь результат партии, где 9 это твои очки, а 11 оппонента
                      ☝️ добавить результаты может только участник партии или администратор
                    • Я автоматически учту результат и обновлю рейтинг

                    🛠 <b>В разработке:</b>
                    • Просмотр рейтинга /rating

                    Если есть вопросы — пишите <a href="https://t.me/lantsev1981">создатею</a>
                    Если хочешь поучаствовать в разработке или просто позырить <a href="https://github.com/lantsev1981/LttfRating">код</a>
                    Если хочешь поблагодарить <a href="https://www.tbank.ru/cf/1k4w2TmaoyE">кликай</a> или сканируй QR-code
                    """,
            cancellationToken: token);
    }
}