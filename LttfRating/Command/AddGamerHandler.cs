namespace LttfRating;

public record AddGamerCommand(string Login, long? UserId = null) : IRequest<bool>;

public class AddGamerHandler(
    IUnitOfWork store,
    ILogger<AddGamerHandler> logger)
    : IRequestHandler<AddGamerCommand, bool>
{
    public async Task<bool> Handle(AddGamerCommand request, CancellationToken token)
    {
        var gamer = await store.GameStore.GetByKey(request.Login, token);
        if (gamer is null)
        {
            gamer = new Gamer(request.Login) { UserId = request.UserId };

            logger.LogTrace("Добавляем нового игрока: @{Login}", request.Login);
            await store.GameStore.AddAsync(gamer, token);

            return request.UserId is not null;
        }

        if (request.UserId is not null && gamer.UserId is null)
        {
            gamer.UserId = request.UserId;
            await store.GameStore.Update(gamer, token);

            return true;
        }

        return false;
    }
}