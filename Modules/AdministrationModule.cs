using Discord.Commands;
using konlulu.DAL;
using konlulu.DAL.Entity;
using konlulu.DAL.Interfaces;
using LiteDB;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace konlulu.Modules
{
    public class AdministrationModule : ModuleBase<SocketCommandContext>
    {
        private readonly IGameDatabaseHandler gameDb;
        private readonly IPlayerDatabaseHandler playerDb;
        private readonly IGamePlayerDatabaseHandler gepDb;

        public AdministrationModule(IGameDatabaseHandler gameDb, IPlayerDatabaseHandler playerDb, IGamePlayerDatabaseHandler gepDb)
        {
            this.gameDb = gameDb;
            this.playerDb = playerDb;
            this.gepDb = gepDb;
        }

        [Command("admin.list")]
        [Summary("set a game's status to end")]
        public Task ListGameAsync()
        {
            IEnumerable<GameEntity> games = gameDb.GetAll();
            StringBuilder sb = new StringBuilder();
            foreach (GameEntity game in games)
            {
                sb.AppendLine(game.ToString());
            }
            return ReplyAsync(sb.ToString());
        }

        [Command("admin.get")]
        [Summary("get a game's info")]
        public Task GetGameAsync([Summary("game's id")] string id)
        {
            ObjectId gameId = new ObjectId(id);
            GameEntity game = gameDb.Get(gameId);
            IEnumerable<GamePlayerEntity> gamePlayerEntities
                = gepDb.Querry(gep => gep.Game.Id.Equals(gameId));
            StringBuilder sb = new StringBuilder();
            sb.AppendLine(game.ToString());
            sb.AppendLine("Player list:");
            foreach (GamePlayerEntity gep in gamePlayerEntities)
            {
                sb.AppendFormat("{0}: {1}", gep.JoinOrder, gep.Player.UserName);
            }
            return ReplyAsync(sb.ToString());
        }

        [Command("admin.end")]
        [Summary("set a game's status to end")]
        public Task EndGameAsync([Summary("game's id")] string id)
        {
            ObjectId gameId = new ObjectId(id);

            GameEntity game = gameDb.Get(gameId);
            game.GameStatus = GameStatus.Ended;
            gameDb.Save(game);

            return ReplyAsync($"set game {gameId} to state end");
        }

        [Command("admin.init")]
        [Summary("set a game's status to init")]
        public Task InitGameAsync([Summary("game's id")] string id)
        {
            ObjectId gameId = new ObjectId(id);

            GameEntity game = gameDb.Get(gameId);
            game.GameStatus = GameStatus.Initiating;
            gameDb.Save(game);

            return ReplyAsync($"set game {gameId} to state init");
        }
    }
}
