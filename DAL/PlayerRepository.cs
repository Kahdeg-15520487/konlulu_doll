using konlulu.DAL.Entity;
using konlulu.DAL.Interfaces;
using LiteDB;

namespace konlulu.DAL
{
    class PlayerRepository : BaseRepository<PlayerEntity>, IPlayerRepository
    {
        public PlayerRepository(ILiteDatabase db) : base(db) { }

        public PlayerEntity GetPlayer(ulong id)
        {
            ILiteCollection<PlayerEntity> querry = db.GetCollection<PlayerEntity>();
            PlayerEntity result = querry.FindOne(pe => pe.UserId == id);
            return result;
        }
    }
}
