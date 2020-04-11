using konlulu.DAL.Entity;
using konlulu.DAL.Interfaces;
using LiteDB;

namespace konlulu.DAL
{
    class PlayerDatabaseHandler : BaseDatabaseHandler<PlayerEntity>, IPlayerDatabaseHandler
    {
        public PlayerDatabaseHandler(ILiteDatabase db) : base(db) { }

        public PlayerEntity GetPlayer(ulong id)
        {
            ILiteCollection<PlayerEntity> querry = db.GetCollection<PlayerEntity>();
            PlayerEntity result = querry.FindOne(pe => pe.UserId == id);
            return result;
        }
    }
}
