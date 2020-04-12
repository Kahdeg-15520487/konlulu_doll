using Discord.Commands;
using konlulu.DAL;
using konlulu.DAL.Entity;
using konlulu.DAL.Interfaces;
using LiteDB;
using Microsoft.Extensions.Logging;
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
        private readonly ILogger<AdministrationModule> logger;

        public AdministrationModule(IGameRepository gameDb, IPlayerRepository playerDb, IGamePlayerRepository gepDb, IConfigRepository configDb, ILogger<AdministrationModule> logger)
        {
            this.gameDb = gameDb;
            this.playerDb = playerDb;
            this.gepDb = gepDb;
            this.configDb = configDb;
            this.logger = logger;
        }

        [Command("admin.listg")]
        [Summary("list all game")]
        public Task ListGameAsync()
        {
            IEnumerable<GameEntity> games = gameDb.GetAll().OrderBy(g => g.StartTime);

            StringBuilder sb = new StringBuilder();
            foreach (GameEntity game in games)
            {
                sb.AppendLine("GameId: " + game.Id);
                sb.AppendLine("Start time: " + game.StartTime);
                sb.AppendLine("Status: " + game.GameStatus);
                sb.AppendLine("Player count: " + game.PlayerCount);
                sb.AppendLine();
            }

            return ReplyAsync(sb.ToString());
        }

        [Command("admin.listp")]
        [Summary("list all player")]
        public Task ListPlayerAsync()
        {
            IEnumerable<PlayerEntity> players = playerDb.GetAll().OrderBy(p => p.TotalOffer);

            StringBuilder sb = new StringBuilder();
            foreach (PlayerEntity player in players)
            {
                sb.AppendLine(player.ToString());
                sb.AppendLine();
            }

            return ReplyAsync(sb.ToString());
        }

        [Command("admin.getg")]
        [Summary("get a game's info")]
        public Task GetGameAsync([Summary("game's id")] string id)
        {
            ObjectId gameId = null;
            try
            {
                gameId = new ObjectId(id);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "player's id ({0}) is likely wrong format", id);
                return ReplyAsync($"game's id ({id}) is likely wrong format");
            }

            GameEntity game = gameDb.Get(gameId);
            IEnumerable<GamePlayerEntity> gamePlayerEntities = gepDb.GetPlayerInGame(gameId);

            StringBuilder sb = new StringBuilder();
            sb.AppendLine("GameId: " + game.Id);
            sb.AppendLine("Start time: " + game.StartTime);
            sb.AppendLine("Status: " + game.GameStatus);

            switch (game.GameStatus)
            {
                case GameStatus.Initiating:
                    sb.AppendLine("Initiating at: " + game.ChannelName?.ToString());
                    break;
                case GameStatus.Playing:
                    sb.AppendLine("Kon time: " + game.KonTime);
                    sb.AppendLine("Fuse time: " + game.FuseTime);
                    sb.AppendLine("Holder: " + game.Holder?.ToString());
                    sb.AppendLine("Occurring at: " + game.ChannelName?.ToString());
                    break;
                case GameStatus.Ended:
                    sb.AppendLine("Kon time: " + game.KonTime);
                    sb.AppendLine("Fuse time: " + game.FuseTime);
                    sb.AppendLine("Occurred at: " + game.ChannelName?.ToString());
                    break;
                default:
                    break;
            }

            sb.AppendLine("Player list: " + gamePlayerEntities.Count());
            foreach (GamePlayerEntity gep in gamePlayerEntities)
            {
                sb.AppendFormat("{0}: {1}", gep.JoinOrder, gep.Player.UserName);
            }
            return ReplyAsync(sb.ToString());
        }

        [Command("admin.getp")]
        [Summary("get a game's info")]
        public Task GetPlayerAsync([Summary("player's id")] string id)
        {
            ObjectId playerId = null;
            try
            {
                playerId = new ObjectId(id);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "player's id ({0}) is likely wrong format", id);
                return ReplyAsync($"player's id ({id}) is likely wrong format");
            }
            PlayerEntity player = playerDb.Get(playerId);
            IEnumerable<GamePlayerEntity> games = gepDb.GetGameByPlayer(playerId);

            StringBuilder sb = new StringBuilder();
            sb.AppendLine(player.ToString());
            sb.AppendLine("Games played: " + games.Count());

            foreach (GamePlayerEntity gep in games)
            {
                sb.AppendLine("GameId: " + gep.Game.Id);
                sb.AppendLine("Join time: " + gep.JoinTime);
                sb.AppendLine("Join order: " + gep.JoinOrder);
                sb.AppendLine("Offered: " + gep.Offer);
                sb.AppendLine("-----");
            }

            return ReplyAsync(sb.ToString());
        }

        [Command("admin.getp")]
        [Summary("get a game's info")]
        public Task GetPlayerByDiscordIdAsync([Summary("player's id")] ulong id)
        {
            PlayerEntity player = playerDb.GetPlayer(id);
            IEnumerable<GamePlayerEntity> games = gepDb.GetGameByPlayer(player.Id);

            StringBuilder sb = new StringBuilder();
            sb.AppendLine(player.ToString());
            sb.AppendLine("Games played: " + games.Count());

            foreach (GamePlayerEntity gep in games)
            {
                sb.AppendLine("GameId: " + gep.Game.Id);
                sb.AppendLine("Join time: " + gep.JoinTime);
                sb.AppendLine("Join order: " + gep.JoinOrder);
                sb.AppendLine("Offered: " + gep.Offer);
                sb.AppendLine("-----");
            }

            return ReplyAsync(sb.ToString());
        }

        [Command("admin.getpu")]
        [Summary("get a game's info")]
        public Task GetPlayerByusernameAsync([Summary("player's username")] string username)
        {
            IEnumerable<PlayerEntity> players = playerDb.Querry(p => p.UserName.Equals(username));

            StringBuilder sb = new StringBuilder();
            foreach (PlayerEntity player in players)
            {
                IEnumerable<GamePlayerEntity> games = gepDb.GetGameByPlayer(player.Id);

                sb.AppendLine(player.ToString());
                sb.AppendLine("Games played: " + games.Count());

                foreach (GamePlayerEntity gep in games)
                {
                    sb.AppendLine("GameId: " + gep.Game.Id);
                    sb.AppendLine("Join time: " + gep.JoinTime);
                    sb.AppendLine("Join order: " + gep.JoinOrder);
                    sb.AppendLine("Offered: " + gep.Offer);
                    sb.AppendLine("-----");
                }
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
        [Summary("modify a game's config")]
        public Task ConfigAsync([Summary("config's name")] string configName, [Summary("config's value")] int value)
        {
            ConfigEntity config = configDb.GetConfigByName(configName);
            if (config == null)
            {
                return ReplyAsync($"there are no config named {configName}");
            }

            config.ConfigValue = value;
            configDb.Save(config);

            return ReplyAsync($"modified config {configName}");
        }
    }
}
