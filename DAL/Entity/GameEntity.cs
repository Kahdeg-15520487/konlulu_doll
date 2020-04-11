using LiteDB;
using System;
using System.Text;

namespace konlulu.DAL.Entity
{
    public enum GameStatus
    {
        Initiating,
        Playing,
        Ended
    }

    public class GameEntity : BaseEntity
    {
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
        public GameStatus GameStatus { get; set; }
        public PlayerEntity PlayerWon { get; set; }
        public PlayerEntity Holder { get; set; }
        public int KonTime { get; set; }
        public int KonCount { get; set; }
        public int FuseTime { get; set; }
        public int FuseCount { get; set; }
        public int PlayerCount { get; set; }
        public ulong ChannelId { get; set; }
        public string ChannelName { get; set; }

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("GameId: " + Id.ToString());
            sb.AppendLine("Channel: " + ChannelName);
            sb.AppendLine("Player count: " + PlayerCount);
            sb.AppendLine("Start time: " + StartTime.ToShortTimeString());
            sb.AppendLine("End time: " + EndTime.ToShortTimeString());
            sb.AppendLine("Status: " + GameStatus);
            sb.AppendLine("Kon time: " + KonTime);
            sb.AppendLine("Fuse time: " + FuseTime);
            sb.AppendLine("Holder: " + Holder?.ToString());
            sb.AppendLine("Player won: " + PlayerWon?.ToString());
            return sb.ToString();
        }
    }
}
