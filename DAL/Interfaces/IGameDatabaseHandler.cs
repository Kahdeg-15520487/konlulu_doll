using konlulu.DAL.Entity;

namespace konlulu.DAL.Interfaces
{
    public interface IGameDatabaseHandler : IBaseDatabaseHandler<GameEntity>
    {
        GameEntity GetInitiatingGame();
    }
}