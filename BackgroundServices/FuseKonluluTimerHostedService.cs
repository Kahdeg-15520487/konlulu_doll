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
                                    ISocketMessageChannel channel = discordClient.GetChannel(game.ChannelId) as ISocketMessageChannel;

                                    //Do your stuff
                                    game.GameStatus = GameStatus.Ended;
                                    gameDb.Save(game);
                                    await channel.SendMessageAsync("Boom!");
                                    //announce winner

                                    PlayerEntity winner = null;
                                    string announcement = null;

                                    IGamePlayerRepository gepDb = scope.ServiceProvider.GetRequiredService<IGamePlayerRepository>();
                                    IOrderedEnumerable<GamePlayerEntity> playerOrderByOffer = gepDb.GetPlayerInGame(game.Id).OrderByDescending(gep => gep.Offer);
                                    GamePlayerEntity mostOffer = playerOrderByOffer.First();
                                    if (mostOffer.Player.Id.Equals(game.Holder.Id)
                                        && game.PlayerCount >= 3)
                                    {
                                        winner = playerOrderByOffer.ElementAt(1).Player;
                                        announcement = $"Although {mostOffer.Player.Mention} has offered most of his soul to KonLulu~ he got exploded due to his greed and thus {winner.Mention} is the final Winner";
                                    }
                                    else
                                    {
                                        winner = mostOffer.Player;
                                        announcement = $"Through dedication to the cause of the Debilulu Church, {winner.Mention} has emerged as a new man, blessed by the Queen of Yharnam herself";
                                    }

                                    IPlayerRepository playerDb = scope.ServiceProvider.GetRequiredService<IPlayerRepository>();

                                    // update stat
                                    winner.GameWin++;
                                    foreach (GamePlayerEntity gep in playerOrderByOffer)
                                    {
                                        PlayerEntity player = gep.Player;
                                        player.GamePlayed++;
                                        player.TotalOffer += gep.Offer;
                                        playerDb.Save(player);
                                    }

                                    await channel.SendMessageAsync(announcement);
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
