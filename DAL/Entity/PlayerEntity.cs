using System.Text;

namespace konlulu.DAL.Entity
{
    public class PlayerEntity : BaseEntity
    {
        public ulong UserId { get; set; }
        public string UserName { get; set; }
        public string Mention { get; set; }
        public int GamePlayed { get; set; }
        public int GameWin { get; set; }
        public int TotalOffer { get; set; }

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("UserId: " + UserId);
            sb.AppendLine("UserName: " + UserName);
            sb.AppendLine("Game played: " + GamePlayed);
            sb.AppendLine("Game won: " + GameWin);
            sb.AppendLine("Total offer: " + TotalOffer);
            return sb.ToString();
        }
    }
}
