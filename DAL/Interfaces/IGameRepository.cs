using konlulu.DAL.Entity;

namespace konlulu.DAL.Interfaces
{
    public interface IGameRepository : IBaseRepository<GameEntity>
    {
        GameEntity GetInitiatingGame();
    }
}