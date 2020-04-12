using Discord.Commands;
using Discord.WebSocket;
using konlulu.BackgroundServices;
using konlulu.DAL;
using konlulu.DAL.Entity;
using konlulu.DAL.Interfaces;
using LiteDB;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace konlulu.Modules
{
    [Name("KonLulu~")]
    [Summary("Main module for playing the game")]
    public class KonluluModule : ModuleBase<SocketCommandContext>
    {
        public static readonly int OFFER_COOLDOWN = 5;
        public static readonly int MAX_OFFER = 10;
        public static readonly int KON_TIME = 5000;
        public static readonly int MAX_FUSE_TIME = 40;
        public static readonly int MIN_FUSE_TIME = 30;
        public static readonly int MIN_PLAYER_COUNT = 3;

        private readonly IGameRepository gameDb;
        private readonly IPlayerRepository playerDb;
        private readonly IGamePlayerRepository gepDb;
        private readonly IConfigRepository configDb;
        private readonly Random random;
        private readonly IBackgroundTaskQueue<(RecurringTimer, ObjectId)> recurringTimerQueue;
        private readonly IBackgroundTaskQueue<(FuseTimer, ObjectId)> fuseTimerQueue;
        private readonly ILogger<KonluluModule> logger;

        public KonluluModule(IGameRepository gameDb, IPlayerRepository playerDb, IGamePlayerRepository gepDb, IConfigRepository configDb, Random random, IBackgroundTaskQueue<(RecurringTimer, ObjectId)> recurringTimerQueue, IBackgroundTaskQueue<(FuseTimer, ObjectId)> fuseTimerQueue, ILogger<KonluluModule> logger)
        {
            this.gameDb = gameDb;
            this.playerDb = playerDb;
            this.gepDb = gepDb;
            this.configDb = configDb;
            this.random = random;
            this.recurringTimerQueue = recurringTimerQueue;
            this.fuseTimerQueue = fuseTimerQueue;
            this.logger = logger;
        }

        protected override void BeforeExecute(CommandInfo command)
        {
            base.BeforeExecute(command);
        }

        [Command("init")]
        [Summary("Initiate a konlulu~ doll game")]
        public Task InitiateAsync()
        {
            GameEntity game = new GameEntity()
            {
                StartTime = DateTime.Now,
                GameStatus = GameStatus.Initiating,
                ChannelId = this.Context.Channel.Id,
                ChannelName = this.Context.Channel.Name
            };
            LiteDB.ObjectId gameId = gameDb.Save(game);
            logger.LogInformation(gameId.ToString());
            return base.ReplyAsync($"init game {gameId.ToString()} on channel {game.ChannelName}");
        }

        [Command("reg")]
        [Summary("Participate in a konlulu~ doll game")]
        public Task RegisterAsync()
        {
            GameEntity game = gameDb.GetInitiatingGame();
            if (game == null)
            {
                return ReplyAsync("There is no game that is initiating!");
            }
            if (game.ChannelId != Context.Channel.Id)
            {
                return ReplyAsync($"Wrong channel, please turn back to channel {game.ChannelName}");
            }

            PlayerEntity player = GetPlayerFromContext();
            if (player == null)
            {
                player = CreateUser();
            }

            GamePlayerEntity gep = new GamePlayerEntity()
            {
                Game = game,
                Player = player,
                JoinTime = DateTime.Now,
                JoinOrder = gepDb.GetJoinOrder(game.Id),
                Offer = 0,
                LastOffer = DateTime.Now
            };
            gepDb.Save(gep);

            game.PlayerCount++;
            gameDb.Save(game);

            return base.ReplyAsync($"registered player {player.Mention} to game {game.Id}");
        }

        [Command("start")]
        [Summary("Start a konlulu~ doll game")]
        public Task StartAsync()
        {
            GameEntity game = gameDb.GetInitiatingGame();
            if (game == null)
            {
                return base.ReplyAsync("There is no game that is initiating!");
            }
            if (game.ChannelId != Context.Channel.Id)
            {
                return ReplyAsync($"Wrong channel, please turn back to channel {game.ChannelName}");
            }

            game.PlayerCount = gepDb.GetPlayerInGame(game.Id).Count();
            if (game.PlayerCount < configDb.GetConfigByName(nameof(MIN_PLAYER_COUNT)).ConfigValue)
            {
                logger.LogInformation($"game.PlayerCount: " + game.PlayerCount);
                return base.ReplyAsync("There is not enough player to start the game!");
            }

            game.StartTime = DateTime.Now;
            game.GameStatus = GameStatus.Playing;
            game.KonCount = 0;
            game.FuseTime = random.Next(
                                        configDb.GetConfigByName(nameof(MIN_FUSE_TIME)).ConfigValue,
                                        configDb.GetConfigByName(nameof(MAX_FUSE_TIME)).ConfigValue
                                       ) * 1000;
            game.KonTime = configDb.GetConfigByName(nameof(KON_TIME)).ConfigValue;
            game.Holder = GetRandomPlayerInGame(game.Id);
            game.PlayerCount = gepDb.GetPlayerInGame(game.Id).Count();
            gameDb.Save(game);

            logger.LogInformation("game.FuseTime:" + game.FuseTime.ToString());

            recurringTimerQueue.QueueBackgroundWorkItem((c) => RecurringTimer(c, game));
            fuseTimerQueue.QueueBackgroundWorkItem((c) => FuseTimer(c, game));
            return base.ReplyAsync($"started game {game.Id}");
        }

        [Command("pass")]
        [Summary("Pass the konlulu~ doll")]
        public Task PassAsync()
        {
            PlayerEntity holder = this.GetPlayerFromContext();
            GameEntity game = this.GetGameFromPlayer(holder);
            if (game == null)
            {
                return Task.CompletedTask;
            }
            if (game.ChannelId != Context.Channel.Id)
            {
                return ReplyAsync($"Wrong channel, please turn back to channel {game.ChannelName}");
            }

            PlayerEntity nextPlayer = this.GetNextPlayerInGame(game, holder);

            game.Holder = nextPlayer;
            gameDb.Save(game);

            StringBuilder sb = new StringBuilder();
            sb.AppendLine($"{holder.Mention} passed the doll to {nextPlayer.Mention}");
            sb.AppendLine($"{nextPlayer.Mention} is now the holder");

            return base.ReplyAsync(sb.ToString());
        }

        [Command("offer")]
        [Summary("Offer to the konlulu~ doll")]
        public Task OfferAsync([Summary("amount of offer")]int offer)
        {
            PlayerEntity holder = this.GetPlayerFromContext();
            GameEntity game = this.GetGameFromPlayer(holder);
            if (game == null)
            {
                return Task.CompletedTask;
            }
            if (game.ChannelId != Context.Channel.Id)
            {
                return ReplyAsync($"Wrong channel, please turn back to channel {game.ChannelName}");
            }

            GamePlayerEntity gep = this.GetGEPFromPlayerAndGame(holder, game);

            TimeSpan timeSinceLastOffer = (DateTime.Now - gep.LastOffer);
            int offerCooldown = configDb.GetConfigByName(nameof(OFFER_COOLDOWN)).ConfigValue;
            if (timeSinceLastOffer.TotalSeconds <= offerCooldown)
            {
                return ReplyAsync($"You can't offer that fast, please wait {offerCooldown - timeSinceLastOffer.TotalSeconds}");
            }

            if (offer >= configDb.GetConfigByName(nameof(MAX_OFFER)).ConfigValue)
            {
                offer = configDb.GetConfigByName(nameof(MAX_OFFER)).ConfigValue;
            }

            // calculate offer
            int calculatedOffer = offer / 2 + 1;
            calculatedOffer = calculatedOffer > 5 ? 5 : calculatedOffer;
            game.FuseTime += calculatedOffer;
            gameDb.Save(game);

            gep.Offer += offer;
            gep.LastOffer = DateTime.Now;
            gepDb.Save(gep);

            return base.ReplyAsync($"{holder.Mention} offered {offer} to speed up the fuse by {calculatedOffer} seconds");
        }

        private GamePlayerEntity GetGEPFromPlayerAndGame(PlayerEntity holder, GameEntity game)
        {
            return gepDb.Querry(gep => gep.Game.Id.Equals(game.Id) && gep.Player.Id.Equals(holder.Id)).FirstOrDefault();
        }

        private PlayerEntity CreateUser()
        {
            PlayerEntity player;
            SocketUser user = this.Context.User;
            // create new player
            player = new PlayerEntity()
            {
                UserId = user.Id,
                UserName = user.Username,
                Mention = user.Mention,
                TotalOffer = 0,
                GameWin = 0,
                GamePlayed = 0
            };
            playerDb.Save(player);
            return player;
        }

        private GameEntity GetGameFromPlayer(PlayerEntity player)
        {
            GamePlayerEntity gep = gepDb.GetGameByPlayer(player.Id).FirstOrDefault(g => g.Game.GameStatus == GameStatus.Playing);
            return gep?.Game;
        }

        private GamePlayerEntity GetGEPFromPlayer(PlayerEntity player)
        {
            GamePlayerEntity gep = gepDb.GetGameByPlayer(player.Id).FirstOrDefault(g => g.Game.GameStatus == GameStatus.Playing);
            return gep;
        }

        private PlayerEntity GetPlayerFromContext()
        {
            SocketUser user = this.Context.User;
            PlayerEntity player = playerDb.GetPlayer(user.Id);
            return player;
        }

        private PlayerEntity GetRandomPlayerInGame(ObjectId id)
        {
            List<GamePlayerEntity> players = gepDb.GetPlayerInGame(id).ToList();
            int randomIndex = random.Next(0, players.Count);
            return players[randomIndex].Player;
        }

        private PlayerEntity GetNextPlayerInGame(GameEntity game, PlayerEntity player)
        {
            List<GamePlayerEntity> players = gepDb.GetPlayerInGame(game.Id).ToList();
            GamePlayerEntity holder = players.FirstOrDefault(gep => gep.Player.Id.Equals(player.Id));
            int currentJoinOrder = holder.JoinOrder;
            int nextJoinOrder = currentJoinOrder + 1;
            if (currentJoinOrder == game.PlayerCount - 1)
            {
                nextJoinOrder = 0;
            }
            GamePlayerEntity nextPlayer = players.FirstOrDefault(gep => gep.JoinOrder == nextJoinOrder);
            return nextPlayer.Player;
        }

        public static Task<(RecurringTimer, ObjectId)> RecurringTimer(CancellationToken cancellationToken, GameEntity game)
        {
            return Task.Delay(game.KonTime).ContinueWith((c) =>
            {
                return (new RecurringTimer(), game.Id);
            });
        }

        public static Task<(FuseTimer, ObjectId)> FuseTimer(CancellationToken cancellationToken, GameEntity game)
        {
            return Task.Delay(1000).ContinueWith((c) =>
            {
                return (new FuseTimer(), game.Id);
            });
        }
    }
}
