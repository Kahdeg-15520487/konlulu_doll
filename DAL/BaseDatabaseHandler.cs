using konlulu.DAL.Entity;
using konlulu.DAL.Interfaces;
using LiteDB;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;

namespace konlulu.DAL
{
    public class BaseDatabaseHandler<T> : IBaseDatabaseHandler<T> where T : BaseEntity
    {
        private readonly string connectionString;

        public BaseDatabaseHandler(IConfiguration configuration)
        {
            connectionString = configuration["_CONNSTR"];
        }

        public virtual IEnumerable<T> GetAll()
        {
            using (ILiteDatabase database = GetDatabase())
            {
                ILiteCollection<T> collection = database.GetCollection<T>();
                return collection.FindAll().ToList();
            }
        }

        public virtual T Get(ObjectId id)
        {
            using (ILiteDatabase database = GetDatabase())
            {
                ILiteCollection<T> collection = database.GetCollection<T>();
                T get = collection.FindOne(x => x.Id == id);
                return get;
            }
        }

        public virtual IEnumerable<T> Querry(Expression<Func<T, bool>> predicate)
        {
            using (ILiteDatabase database = GetDatabase())
            {
                return database.GetCollection<T>().Find(predicate);
            }
        }

        public virtual ObjectId Save(T document)
        {
            using (ILiteDatabase database = GetDatabase())
            {
                ILiteCollection<T> collection = database.GetCollection<T>();
                ObjectId result = null;

                if (!collection.Exists(x => x.Id == document.Id))
                {
                    result = collection.Insert(document);
                }
                else
                {
                    collection.Update(document);
                    result = document.Id;
                }
                return result;
            }
        }

        public virtual void Delete(T document)
        {
            using (ILiteDatabase database = GetDatabase())
            {
                ILiteCollection<T> collection = database.GetCollection<T>();
                collection.DeleteMany(x => x.Id == document.Id);
            }
        }

        public ILiteDatabase GetDatabase()
        {
            return new LiteDatabase(connectionString);
        }

        //public ILiteDatabase GetDatabase()
        //{
        //    if (SingletonLiteDB == null)
        //    {
        //        SingletonLiteDB = new LiteDatabase(connectionString);
        //    }
        //    return SingletonLiteDB;
        //}
        //private static ILiteDatabase SingletonLiteDB = null;
    }
}
