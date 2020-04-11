using konlulu.DAL.Entity;
using konlulu.DAL.Interfaces;
using LiteDB;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;

namespace konlulu.DAL
{
    public class GamePlayerDatabaseHandler : BaseDatabaseHandler<GamePlayerEntity>, IGamePlayerDatabaseHandler
    {
        public GamePlayerDatabaseHandler(IConfiguration configuration) : base(configuration) { }

        public override IEnumerable<GamePlayerEntity> Querry(Expression<Func<GamePlayerEntity, bool>> predicate)
        {
            using (ILiteDatabase db = base.GetDatabase())
            {
                IEnumerable<GamePlayerEntity> querry = db.GetCollection<GamePlayerEntity>()
                                                         .Include(gep => gep.Player)
                                                         .Include(gep => gep.Game)
                                                         .Find(predicate);
                return querry.ToList();
            }
        }

        public IEnumerable<GamePlayerEntity> GetGameByPlayer(ObjectId playerId)
        {
            return Querry(gep => gep.Player.Id.Equals(playerId));
        }

        public IEnumerable<GamePlayerEntity> GetPlayerInGame(ObjectId gameId)
        {
            return Querry(gep => gep.Game.Id.Equals(gameId));
        }

        public int GetJoinOrder(ObjectId gameId)
        {
            using (ILiteDatabase db = base.GetDatabase())
            {
                IEnumerable<GamePlayerEntity> querry = db.GetCollection<GamePlayerEntity>()
                                                         .Include(gep => gep.Player)
                                                         .Include(gep => gep.Game)
                                                         .Find(gep => gep.Player.Id.Equals(gameId))
                                                         .OrderBy(gep => gep.JoinOrder);
                GamePlayerEntity last = querry.LastOrDefault();
                if (last == null)
                {
                    return 0;
                }
                else
                {
                    return last.JoinOrder + 1;
                }
            }
        }
    }
}
