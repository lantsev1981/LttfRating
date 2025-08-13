namespace LttfRating;

public record AddGamerCommand(string Login): IRequest;

public class AddGamerHandler(
    IDomainStore<Gamer> store,
    ILogger<AddGamerHandler> logger)
    : IRequestHandler<AddGamerCommand>
{
    public async Task Handle(AddGamerCommand request, CancellationToken token)
    {
        var gamer = await store.GetById(request.Login, token);
        if (gamer is null)
        {
            gamer = new Gamer(request.Login);

            logger.LogTrace("Добавляем нового игрока: @{Login}", request.Login);
            await store.AddAsync(gamer, token);
        }
    }
}