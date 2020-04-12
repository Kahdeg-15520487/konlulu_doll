using konlulu.DAL.Entity;
using LiteDB;
using System;
using System.Collections.Generic;
using System.Linq.Expressions;

namespace konlulu.DAL.Interfaces
{
    public interface IBaseRepository<T> where T : BaseEntity
    {
        IEnumerable<T> GetAll();
        T Get(ObjectId id);
        IEnumerable<T> Querry(Expression<Func<T, bool>> predicate);
        ObjectId Save(T document);
        void Delete(T document);
    }
}