namespace LttfRating;

public record AddGamerCommand(string Login, long? UserId = null): IRequest;

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
            gamer = new Gamer(request.Login) {UserId = request.UserId};

            logger.LogTrace("Добавляем нового игрока: @{Login}", request.Login);
            await store.AddAsync(gamer, token);
        }
        else if (request.UserId is not null && gamer.UserId is null)
        {
            gamer.UserId = request.UserId;
            await store.UpdateItem(gamer, token);
        }
    }
}