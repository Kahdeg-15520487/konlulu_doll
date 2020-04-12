using Discord.WebSocket;
using konlulu.DAL;
using konlulu.DAL.Entity;
using konlulu.DAL.Interfaces;
using konlulu.Modules;
using LiteDB;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace konlulu.BackgroundServices
{
    class FuseKonluluTimerHostedService : BackgroundService
    {
        private readonly IBackgroundTaskQueue<(FuseTimer, ObjectId)> taskQueue;
        private readonly IServiceScopeFactory serviceScopeFactory;
        private readonly ILogger<RecurringKonluluTimerHostedService> logger;

        public FuseKonluluTimerHostedService(IBackgroundTaskQueue<(FuseTimer, ObjectId)> taskQueue, IServiceScopeFactory serviceScopeFactory, ILogger<RecurringKonluluTimerHostedService> logger)
        {
            this.taskQueue = taskQueue;
            this.serviceScopeFactory = serviceScopeFactory;
            this.logger = logger;
        }
        public override Task StartAsync(CancellationToken cancellationToken)
        {
            logger.LogInformation("Fuse Timer Manager Service started");
            return base.StartAsync(cancellationToken);
        }

        protected override async Task ExecuteAsync(CancellationToken cancelToken)
        {
            try
            {
                while (!cancelToken.IsCancellationRequested)
                {
                    try
                    {
                        Func<CancellationToken, Task<(FuseTimer, ObjectId)>> workItem = await taskQueue.DequeueAsync(cancelToken);
                        (FuseTimer t, ObjectId gameId) konluluTimer = await workItem(cancelToken);
                        {
                            using (IServiceScope scope = serviceScopeFactory.CreateScope())
                            {
                                IGameRepository gameDb = scope.ServiceProvider.GetRequiredService<IGameRepository>();
                                ObjectId gameId = new ObjectId(konluluTimer.gameId);
                                GameEntity game = gameDb.Get(gameId);
                                game.FuseCount++;
                                logger.LogInformation($"fuse:{konluluTimer.gameId}:{game.FuseCount}");

                                if (game.FuseCount * 1000 < game.FuseTime)
                                {
                                    gameDb.Save(game);
                                    taskQueue.QueueBackgroundWorkItem((c) => KonluluModule.FuseTimer(c, game));
                                }
                                else
                                {
                                    DiscordSocketClient discordClient = scope.ServiceProvider.GetRequiredService<DiscordSocketClient>();
                                    IGamePlayerRepository gepDb = scope.ServiceProvider.GetRequiredService<IGamePlayerRepository>();
                                    IFlavorTextRepository ftDb = scope.ServiceProvider.GetRequiredService<IFlavorTextRepository>();

                                    //get the channel that the game is occurring in
                                    ISocketMessageChannel channel = discordClient.GetChannel(game.ChannelId) as ISocketMessageChannel;

                                    //end the game
                                    game.GameStatus = GameStatus.Ended;
                                    gameDb.Save(game);
                                    await channel.SendMessageAsync("Boom!");

                                    //fetch flavor text
                                    //todo add flavor text

                                    //select winner
                                    PlayerEntity winner = null;
                                    string announcement = null;

                                    //get the player who offer most
                                    IOrderedEnumerable<GamePlayerEntity> playerOrderByOffer = gepDb.GetPlayerInGame(game.Id).OrderByDescending(gep => gep.Offer);
                                    GamePlayerEntity mostOffer = playerOrderByOffer.First();

                                    //check for mostoffer player's win condition
                                    if (mostOffer.Player.Id.Equals(game.Holder.Id)
                                        && game.PlayerCount > 1)
                                    {
                                        //runner up win
                                        winner = playerOrderByOffer.ElementAt(1).Player;
                                        announcement = string.Format("Although {0} has offered most of his soul to KonLulu~ he got exploded due to his greed and thus {1} is the final Winner", mostOffer.Player.Mention, winner.Mention);
                                    }
                                    else
                                    {
                                        //mostoffer win
                                        winner = mostOffer.Player;
                                        announcement = string.Format("Through dedication to the cause of the Debilulu Church, {0} has emerged as a new man, blessed by the Queen of Yharnam herself while the rest perish", winner.Mention);
                                    }

                                    //announce winner
                                    await channel.SendMessageAsync(announcement);

                                    // update player's stat
                                    IPlayerRepository playerDb = scope.ServiceProvider.GetRequiredService<IPlayerRepository>();

                                    winner.GameWin++;
                                    foreach (GamePlayerEntity gep in playerOrderByOffer)
                                    {
                                        PlayerEntity player = gep.Player;
                                        player.GamePlayed++;
                                        player.TotalOffer += gep.Offer;
                                        playerDb.Save(player);
                                    }
                                }
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        StringBuilder sb = new StringBuilder();
                        sb.AppendLine("Exception when loading command modules");
                        sb.AppendLine(ex.Message);
                        sb.AppendLine(ex.StackTrace);
                        logger.LogError(sb.ToString());
                    }
                }
            }
            catch (Exception ex)
            {
                StringBuilder sb = new StringBuilder();
                sb.AppendLine("Exception when loading command modules");
                sb.AppendLine(ex.Message);
                sb.AppendLine(ex.StackTrace);
                logger.LogError(sb.ToString());
            }
        }
        public override Task StopAsync(CancellationToken cancellationToken)
        {
            logger.LogInformation("Fuse Timer Manager Service stopped");
            return base.StopAsync(cancellationToken);
        }
    }
}
