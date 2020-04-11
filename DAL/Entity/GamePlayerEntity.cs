using System;
using System.Collections.Generic;
using System.Text;

namespace konlulu.DAL.Entity
{
    public class GamePlayerEntity : BaseEntity
    {
        public PlayerEntity Player { get; set; }
        public GameEntity Game { get; set; }
        public int JoinOrder { get; set; }
        public DateTime JoinTime { get; set; }
        public int Offer { get; set; }
        public DateTime LastOffer { get; set; }
    }
}
