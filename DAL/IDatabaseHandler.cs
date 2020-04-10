using konlulu.DAL.Entity;
using LiteDB;

namespace konlulu.DAL
{
    public interface IDatabaseHandler
    {
        void Delete<T>(T document) where T : BaseEntity;
        T Get<T>(object id) where T : BaseEntity;
        ILiteCollection<T> GetCollection<T>() where T : BaseEntity;
        void Save<T>(T document) where T : BaseEntity;
    }
}