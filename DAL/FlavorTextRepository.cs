using konlulu.DAL.Entity;
using konlulu.DAL.Interfaces;
using LiteDB;
using System;

namespace konlulu.DAL
{
    class FlavorTextRepository : BaseRepository<FlavorTextEntity>, IFlavorTextRepository
    {
        public FlavorTextRepository(ILiteDatabase db) : base(db) { }

        public FlavorTextEntity GetByName(string name)
        {
            ILiteCollection<FlavorTextEntity> querry = db.GetCollection<FlavorTextEntity>();
            FlavorTextEntity flavorText = querry.FindOne(ft => ft.Identifier.Equals(name, StringComparison.OrdinalIgnoreCase));
            return flavorText;
        }
    }
}
