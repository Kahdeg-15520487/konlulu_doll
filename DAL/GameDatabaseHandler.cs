using konlulu.DAL.Entity;
using konlulu.DAL.Interfaces;
using LiteDB;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Text;

namespace konlulu.DAL
{
    class GameDatabaseHandler : BaseDatabaseHandler<GameEntity>, IGameDatabaseHandler
    {
        public GameDatabaseHandler(IConfiguration configuration) : base(configuration) { }

        public GameEntity GetInitiatingGame()
        {
            using (ILiteDatabase db = base.GetDatabase())
            {
                ILiteCollection<GameEntity> querry = db.GetCollection<GameEntity>();
                GameEntity result = querry.FindOne(g => g.GameStatus == GameStatus.Initiating);
                return result;
            }
        }
    }
}
