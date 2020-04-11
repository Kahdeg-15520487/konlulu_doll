using konlulu.DAL.Entity;
using LiteDB;
using System;
using System.Collections.Generic;
using System.Text;

namespace konlulu.DAL.Interfaces
{
    public interface IGamePlayerDatabaseHandler : IBaseDatabaseHandler<GamePlayerEntity>
    {
        IEnumerable<GamePlayerEntity> GetPlayerInGame(ObjectId gameId);
        IEnumerable<GamePlayerEntity> GetGameByPlayer(ObjectId playerId);
        int GetJoinOrder(ObjectId gameId);
    }
}
