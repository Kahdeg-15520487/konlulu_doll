using konlulu.DAL.Entity;
using LiteDB;
using System;
using System.Collections.Generic;
using System.Linq.Expressions;

namespace konlulu.DAL.Interfaces
{
    public interface IFlavorTextRepository : IBaseRepository<FlavorTextEntity>
    {
        FlavorTextEntity GetByName(string name);
    }
}