using konlulu.DAL.Entity;
using konlulu.DAL.Interfaces;
using LiteDB;
using System;

namespace konlulu.DAL
{
    class ConfigRepository : BaseRepository<ConfigEntity>, IConfigRepository
    {
        public ConfigRepository(ILiteDatabase db) : base(db) { }

        public ConfigEntity GetConfigByName(string name)
        {
            ILiteCollection<ConfigEntity> querry = db.GetCollection<ConfigEntity>();
            ConfigEntity result = querry.FindOne(c => c.ConfigName.Equals(name, StringComparison.OrdinalIgnoreCase));
            return result;
        }

        public int GetConfigValueByName(string name)
        {
            ILiteCollection<ConfigEntity> querry = db.GetCollection<ConfigEntity>();
            ConfigEntity result = querry.FindOne(c => c.ConfigName.Equals(name, StringComparison.OrdinalIgnoreCase));
            return result.ConfigValue;
        }

        public ObjectId SaveWithoutUpdate(ConfigEntity config)
        {
            ILiteCollection<ConfigEntity> collection = db.GetCollection<ConfigEntity>();
            ObjectId result = null;

            if (!collection.Exists(x => x.ConfigName.Equals(config.ConfigName)))
            {
                result = collection.Insert(config);
            }
            return result;
        }
    }
}
