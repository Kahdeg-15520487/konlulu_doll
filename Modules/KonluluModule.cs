using Discord.Commands;
using Discord.WebSocket;
using konlulu.BackgroundServices;
using konlulu.DAL;
using konlulu.DAL.Entity;
using konlulu.DAL.Interfaces;
using LiteDB;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace konlulu.Modules
{
    public class KonluluModule : ModuleBase<SocketCommandContext>
    {
        private static readonly int OFFER_COOLDOWN = 5;
        private static readonly int MAX_OFFER = 10;
        private static readonly int KON_TIME = 5000;
        private static readonly int MAX_FUSE_TIME = 40;
        private static readonly int MIN_FUSE_TIME = 30;
        private static readonly int MIN_PLAYER_COUNT = 3;

        private readonly IGameDatabaseHandler gameDb;
        private readonly IPlayerDatabaseHandler playerDb;
        private readonly IGamePlayerDatabaseHandler gepDb;
        private readonly Random random;
        private readonly IBackgroundTaskQueue<(RecurringTimer, ObjectId)> recurringTimerQueue;
        private readonly IBackgroundTaskQueue<(FuseTimer, ObjectId)> fuseTimerQueue;

        public KonluluModule(IGameDatabaseHandler gameDb, IPlayerDatabaseHandler playerDb, IGamePlayerDatabaseHandler gepDb, Random random, IBackgroundTaskQueue<(RecurringTimer, ObjectId)> recurringTimerQueue, IBackgroundTaskQueue<(FuseTimer, ObjectId)> fuseTimerQueue)
        {
            this.gameDb = gameDb;
            this.playerDb = playerDb;
            this.gepDb = gepDb;
            this.random = random;
            this.recurringTimerQueue = recurringTimerQueue;
            this.fuseTimerQueue = fuseTimerQueue;
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
                StartTime = DateTime.UtcNow,
                GameStatus = GameStatus.Initiating,
                ChannelId = this.Context.Channel.Id,
                ChannelName = this.Context.Channel.Name
            };
            LiteDB.ObjectId gameId = gameDb.Save(game);
            Console.WriteLine(gameId.ToString());
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
                JoinTime = DateTime.UtcNow,
                JoinOrder = gepDb.GetJoinOrder(game.Id),
                Offer = 0,
                LastOffer = DateTime.UtcNow
            };

            gepDb.Save(gep);

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
            //if (game.PlayerCount <= MIN_PLAYER_COUNT)
            //{
            //    return base.ReplyAsync("There is not enough player to start the game!");
            //}

            game.StartTime = DateTime.UtcNow;
            game.GameStatus = GameStatus.Playing;
            game.KonCount = 0;
            game.FuseTime = random.Next(MIN_FUSE_TIME, MAX_FUSE_TIME) * 1000;
            game.KonTime = KON_TIME;
            game.Holder = GetRandomPlayerInGame(game.Id);
            game.PlayerCount = gepDb.GetPlayerInGame(game.Id).Count();
            gameDb.Save(game);

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
                return base.ReplyAsync("There is no game that is playing!");
            }
            if (game.ChannelId != Context.Channel.Id)
            {
                return ReplyAsync($"Wrong channel, please turn back to channel {game.ChannelName}");
            }

            PlayerEntity nextPlayer = this.GetNextPlayerInGame(game, holder);

            game.Holder = nextPlayer;
            gameDb.Save(game);

            return base.ReplyAsync($"{holder.Mention} passed the doll to {nextPlayer.Mention}");
        }

        [Command("offer")]
        [Summary("Offer to the konlulu~ doll")]
        public Task OfferAsync([Summary("amount of offer")]int offer)
        {
            PlayerEntity holder = this.GetPlayerFromContext();
            GameEntity game = this.GetGameFromPlayer(holder);
            if (game == null)
            {
                return base.ReplyAsync("There is no game that is playing!");
            }
            if (game.ChannelId != Context.Channel.Id)
            {
                return ReplyAsync($"Wrong channel, please turn back to channel {game.ChannelName}");
            }
            GamePlayerEntity gep = this.GetGEPFromPlayer(holder);
            TimeSpan timeSinceLastOffer = (DateTime.UtcNow - gep.LastOffer);
            if (timeSinceLastOffer.TotalSeconds <= OFFER_COOLDOWN)
            {
                return ReplyAsync($"You can't offer that fast, please wait {OFFER_COOLDOWN - timeSinceLastOffer.TotalSeconds}");
            }

            if (offer >= MAX_OFFER)
            {
                offer = MAX_OFFER;
            }

            // calculate offer
            game.FuseCount += offer / 2;
            gameDb.Save(game);

            gep.Offer += offer;
            gep.LastOffer = DateTime.UtcNow;
            gepDb.Save(gep);

            return base.ReplyAsync($"{holder.Mention} offered {offer}");
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
