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
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace konlulu.BackgroundServices
{
    class RecurringKonluluTimerHostedService : BackgroundService
    {
        private readonly IBackgroundTaskQueue<(RecurringTimer, ObjectId)> taskQueue;
        private readonly IServiceScopeFactory serviceScopeFactory;
        private readonly ILogger<DiscordHandlerHostedService> logger;

        public RecurringKonluluTimerHostedService(IBackgroundTaskQueue<(RecurringTimer, ObjectId)> taskQueue, IServiceScopeFactory serviceScopeFactory, ILogger<DiscordHandlerHostedService> logger)
        {
            this.taskQueue = taskQueue;
            this.serviceScopeFactory = serviceScopeFactory;
            this.logger = logger;
        }
        public override Task StartAsync(CancellationToken cancellationToken)
        {
            logger.LogInformation("Kon Timer Manager Service started");
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
                                IGameRepository gameDb = scope.ServiceProvider.GetRequiredService<IGameRepository>();
                                DiscordSocketClient discordClient = scope.ServiceProvider.GetRequiredService<DiscordSocketClient>();
                                ObjectId gameId = new ObjectId(konluluTimer.gameId);
                                GameEntity game = gameDb.Get(gameId);

                                if (game.GameStatus == GameStatus.Playing)
                                {
                                    game.KonCount++;
                                    gameDb.Save(game);
                                    logger.LogInformation($"{konluluTimer.gameId}:{game.KonCount}");
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
            logger.LogInformation("Kon Timer Manager Service stopped");
            return base.StopAsync(cancellationToken);
        }
    }
}
