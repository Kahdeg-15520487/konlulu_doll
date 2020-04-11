using konlulu.DAL.Entity;
using System;
using System.Collections.Generic;
using System.Text;

namespace konlulu.DAL.Interfaces
{
    public interface IPlayerDatabaseHandler : IBaseDatabaseHandler<PlayerEntity>
    {
        PlayerEntity GetPlayer(ulong id);
    }
}
