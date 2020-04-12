using konlulu.DAL.Entity;
using konlulu.DAL.Interfaces;
using LiteDB;

namespace konlulu.DAL
{
    class GameRepository : BaseRepository<GameEntity>, IGameRepository
    {
        public GameRepository(ILiteDatabase db) : base(db) { }

        public GameEntity GetInitiatingGame()
        {
            ILiteCollection<GameEntity> querry = db.GetCollection<GameEntity>();
            GameEntity result = querry.FindOne(g => g.GameStatus == GameStatus.Initiating);
            return result;
        }
    }
}
