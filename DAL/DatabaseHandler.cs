using konlulu.DAL.Entity;
using LiteDB;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text;

namespace konlulu.DAL
{
    public class DatabaseHandler : IDatabaseHandler
    {
        private readonly ConcurrentDictionary<object, BaseEntity> cache;
        private readonly string connectionString;

        public DatabaseHandler(IConfiguration configuration)
        {
            cache = new ConcurrentDictionary<object, BaseEntity>();
            connectionString = configuration["_CONNSTR"];
        }

        public T Get<T>(object id) where T : BaseEntity
        {
            if (cache.TryGetValue(id, out BaseEntity cached))
            {
                return (T)cached;
            }

            ILiteCollection<T> collection = GetCollection<T>();
            T get = collection.FindOne(x => x.Id == id);

            if (!(get is null))
            {
                cache.TryAdd(get.Id, get);
            }

            return get;
        }

        public void Save<T>(T document) where T : BaseEntity
        {
            ILiteCollection<T> collection = this.GetCollection<T>();

            if (!collection.Exists(x => x.Id == document.Id))
            {
                collection.Insert(document);
                cache.TryAdd(document.Id, document);
            }
            else
            {
                collection.Update(document);
                cache.TryUpdate(document.Id, document, null);
            }
        }

        public void Delete<T>(T document) where T : BaseEntity
        {
            ILiteCollection<T> collection = this.GetCollection<T>();
            collection.DeleteMany(x => x.Id == document.Id);
            cache.TryRemove(document.Id, out _);
        }

        public ILiteCollection<T> GetCollection<T>() where T : BaseEntity
        {
            using (LiteDatabase database = new LiteDatabase(connectionString))
            {
                return database.GetCollection<T>(typeof(T).Name);
            }
        }
    }
}
