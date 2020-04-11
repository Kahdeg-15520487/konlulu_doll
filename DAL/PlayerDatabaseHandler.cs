using konlulu.DAL.Entity;
using konlulu.DAL.Interfaces;
using LiteDB;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Text;

namespace konlulu.DAL
{
    class PlayerDatabaseHandler : BaseDatabaseHandler<PlayerEntity>, IPlayerDatabaseHandler
    {
        public PlayerDatabaseHandler(IConfiguration configuration) : base(configuration) { }

        public PlayerEntity GetPlayer(ulong id)
        {
            using (ILiteDatabase db = base.GetDatabase())
            {
                ILiteCollection<PlayerEntity> querry = db.GetCollection<PlayerEntity>();
                PlayerEntity result = querry.FindOne(pe => pe.UserId == id);
                return result;
            }
        }
    }
}
