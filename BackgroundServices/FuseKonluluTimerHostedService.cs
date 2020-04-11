using Discord.WebSocket;
using konlulu.DAL;
using konlulu.DAL.Entity;
using konlulu.DAL.Interfaces;
using konlulu.Modules;
using LiteDB;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace konlulu.BackgroundServices
{
    class FuseKonluluTimerHostedService : BackgroundService
    {
        private readonly IBackgroundTaskQueue<(FuseTimer, ObjectId)> taskQueue;
        private readonly IServiceScopeFactory serviceScopeFactory;

        public FuseKonluluTimerHostedService(IBackgroundTaskQueue<(FuseTimer, ObjectId)> taskQueue, IServiceScopeFactory serviceScopeFactory)
        {
            this.taskQueue = taskQueue;
            this.serviceScopeFactory = serviceScopeFactory;
        }
        public override Task StartAsync(CancellationToken cancellationToken)
        {
            Console.WriteLine("Fuse Timer Manager Service started");
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
                                IGameDatabaseHandler gameDb = scope.ServiceProvider.GetRequiredService<IGameDatabaseHandler>();
                                ObjectId gameId = new ObjectId(konluluTimer.gameId);
                                GameEntity game = gameDb.Get(gameId);
                                game.FuseCount++;
                                Console.WriteLine($"{konluluTimer.gameId}:{game.FuseCount}");
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

                                    IGamePlayerDatabaseHandler gepDb = scope.ServiceProvider.GetRequiredService<IGamePlayerDatabaseHandler>();
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

                                    IPlayerDatabaseHandler playerDb = scope.ServiceProvider.GetRequiredService<IPlayerDatabaseHandler>();

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
                        Console.WriteLine(ex.Message);
                        Console.WriteLine(ex.StackTrace);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                Console.WriteLine(ex.StackTrace);
            }
        }
        public override Task StopAsync(CancellationToken cancellationToken)
        {
            Console.WriteLine("Fuse Timer Manager Service stopped");
            return base.StopAsync(cancellationToken);
        }
    }
}
