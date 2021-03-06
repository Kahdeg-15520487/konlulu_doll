﻿using konlulu.DAL.Entity;
using konlulu.DAL.Interfaces;
using LiteDB;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;

namespace konlulu.DAL
{
    public class GamePlayerRepository : BaseRepository<GamePlayerEntity>, IGamePlayerRepository
    {
        private readonly ILogger<GamePlayerRepository> logger;

        public GamePlayerRepository(ILiteDatabase db, ILogger<GamePlayerRepository> logger) : base(db)
        {
            this.logger = logger;

        }

        public override IEnumerable<GamePlayerEntity> Querry(Expression<Func<GamePlayerEntity, bool>> predicate)
        {
            IEnumerable<GamePlayerEntity> querry = db.GetCollection<GamePlayerEntity>()
                                                     .Include(gep => gep.Player)
                                                     .Include(gep => gep.Game)
                                                     .Find(predicate);
            return querry.ToList();
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
            IEnumerable<GamePlayerEntity> querry = db.GetCollection<GamePlayerEntity>()
                                                     .Include(gep => gep.Player)
                                                     .Include(gep => gep.Game)
                                                     .Find(gep => gep.Game.Id.Equals(gameId))
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
