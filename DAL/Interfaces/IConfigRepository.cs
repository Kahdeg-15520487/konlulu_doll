using konlulu.DAL.Entity;
using LiteDB;

namespace konlulu.DAL.Interfaces
{
    public interface IConfigRepository : IBaseRepository<ConfigEntity>
    {
        ConfigEntity GetConfigByName(string name);
        int GetConfigValueByName(string name);
        ObjectId SaveWithoutUpdate(ConfigEntity config);
    }
}
