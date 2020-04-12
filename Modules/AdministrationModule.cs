using Discord.Commands;
using konlulu.DAL;
using konlulu.DAL.Entity;
using konlulu.DAL.Interfaces;
using LiteDB;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace konlulu.Modules
{
    [Name("Administration and debug module")]
    [Summary("For when the game doesn't work as intended")]
    [RequireRole("game_admin")]
    public class AdministrationModule : ModuleBase<SocketCommandContext>
    {
        private readonly IGameRepository gameDb;
        private readonly IPlayerRepository playerDb;
        private readonly IGamePlayerRepository gepDb;
        private readonly IConfigRepository configDb;

        public AdministrationModule(IGameRepository gameDb, IPlayerRepository playerDb, IGamePlayerRepository gepDb, IConfigRepository configDb)
        {
            this.gameDb = gameDb;
            this.playerDb = playerDb;
            this.gepDb = gepDb;
            this.configDb = configDb;
        }

        [Command("admin.list")]
        [Summary("set a game's status to end")]
        public Task ListGameAsync()
        {
            IEnumerable<GameEntity> games = gameDb.GetAll().OrderBy(g => g.StartTime);
            StringBuilder sb = new StringBuilder();
            foreach (GameEntity game in games)
            {
                sb.AppendLine(game.Id.ToString());
                sb.AppendLine(game.StartTime.ToString());
                sb.AppendLine(game.GameStatus.ToString());
                sb.AppendLine();
            }
            return ReplyAsync(sb.ToString());
        }

        [Command("admin.get")]
        [Summary("get a game's info")]
        public Task GetGameAsync([Summary("game's id")] string id)
        {
            ObjectId gameId = new ObjectId(id);
            GameEntity game = gameDb.Get(gameId);
            IEnumerable<GamePlayerEntity> gamePlayerEntities = gepDb.GetPlayerInGame(gameId);
            StringBuilder sb = new StringBuilder();
            sb.AppendLine(game.Id.ToString());
            sb.AppendLine(game.StartTime.ToString());
            sb.AppendLine(game.GameStatus.ToString());
            sb.AppendLine(game.Holder?.ToString());
            sb.AppendLine("Occurring at: " + game.ChannelName?.ToString());
            sb.AppendLine("Player list: " + gamePlayerEntities.Count());
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


        [Command("admin.config")]
        [Summary("get a game's config")]
        public Task ConfigAsync()
        {
            IEnumerable<ConfigEntity> configs = configDb.GetAll();

            StringBuilder sb = new StringBuilder();
            foreach (ConfigEntity config in configs)
            {
                sb.AppendLine(config.ToString());
            }

            return ReplyAsync(sb.ToString());
        }

        [Command("admin.config")]
        [Summary("set a game's status to init")]
        public Task ConfigAsync([Summary("config's name")] string config, [Summary("config's value")] int value)
        {


            return ReplyAsync($"set game to state init");
        }
    }
}
