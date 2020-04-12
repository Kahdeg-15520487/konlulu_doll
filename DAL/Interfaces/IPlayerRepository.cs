using konlulu.DAL.Entity;
using System;
using System.Collections.Generic;
using System.Text;

namespace konlulu.DAL.Interfaces
{
    public interface IPlayerRepository : IBaseRepository<PlayerEntity>
    {
        PlayerEntity GetPlayer(ulong id);
    }
}
