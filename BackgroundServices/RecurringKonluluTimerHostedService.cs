using Discord.WebSocket;
using konlulu.DAL;
using konlulu.DAL.Entity;
using konlulu.DAL.Interfaces;
using konlulu.Modules;
using LiteDB;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace konlulu.BackgroundServices
{
    class RecurringKonluluTimerHostedService : BackgroundService
    {
        private readonly IBackgroundTaskQueue<(RecurringTimer, ObjectId)> taskQueue;
        private readonly IServiceScopeFactory serviceScopeFactory;

        public RecurringKonluluTimerHostedService(IBackgroundTaskQueue<(RecurringTimer, ObjectId)> taskQueue, IServiceScopeFactory serviceScopeFactory)
        {
            this.taskQueue = taskQueue;
            this.serviceScopeFactory = serviceScopeFactory;
        }
        public override Task StartAsync(CancellationToken cancellationToken)
        {
            Console.WriteLine("Timer Manager Service started");
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
                        Func<CancellationToken, Task<(RecurringTimer, ObjectId)>> workItem = await taskQueue.DequeueAsync(cancelToken);
                        (RecurringTimer t, ObjectId gameId) konluluTimer = await workItem(cancelToken);
                        {
                            using (IServiceScope scope = serviceScopeFactory.CreateScope())
                            {
                                IGameDatabaseHandler gameDb = scope.ServiceProvider.GetRequiredService<IGameDatabaseHandler>();
                                DiscordSocketClient discordClient = scope.ServiceProvider.GetRequiredService<DiscordSocketClient>();
                                ObjectId gameId = new ObjectId(konluluTimer.gameId);
                                GameEntity game = gameDb.Get(gameId);

                                if (game.GameStatus == GameStatus.Playing)
                                {
                                    game.KonCount++;
                                    gameDb.Save(game);
                                    Console.WriteLine($"{konluluTimer.gameId}:{game.KonCount}");
                                    if (game.KonCount * 5000 <= game.FuseTime)
                                    {
                                        ISocketMessageChannel channel = discordClient.GetChannel(game.ChannelId) as ISocketMessageChannel;
                                        await channel.SendMessageAsync("Kon~ Kon~");
                                        taskQueue.QueueBackgroundWorkItem((c) => KonluluModule.RecurringTimer(c, game));
                                    }
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
            Console.WriteLine("Timer Manager Service stopped");
            return base.StopAsync(cancellationToken);
        }
    }
}
