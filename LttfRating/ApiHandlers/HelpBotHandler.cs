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
                      <code>@игрок1 @игрок2 8 11</code>
                      ☝️ тегни игроков и проставь результат партии в порядке указания игроков
                      ☝️ добавить результаты может только проигравший игрок или администратор
                    • Я автоматически учту результат и обновлю рейтинг.

                    🛠 <b>В разработке:</b>
                    • Просмотр рейтинга /rating

                    Если есть вопросы — пишите @lantsev1981
                    Если хочешь поучавствовать в разработке https://github.com/lantsev1981/LttfRating
                    Если хочешь поблагодарить кликай https://www.tbank.ru/cf/1k4w2TmaoyE или сканируй QR-code
                    """,
            cancellationToken: token);
    }
}