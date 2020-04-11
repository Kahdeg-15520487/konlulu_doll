using Discord;
using Discord.Commands;
using Discord.WebSocket;
using konlulu.BackgroundServices;
using konlulu.DAL;
using konlulu.DAL.Entity;
using konlulu.DAL.Interfaces;
using LiteDB;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System;
using System.Net.Http;
using System.Threading.Tasks;

namespace konlulu
{
    class Program
    {
        public static void Main(string[] args)
            => new Program().MainAsync().GetAwaiter().GetResult();

        public async Task MainAsync()
        {
            await new HostBuilder()
                      .ConfigureServices(ConfigureServices)
                      .RunConsoleAsync();
        }

        private static IConfiguration configuration;

        private static IConfiguration LoadConfiguration()
        {
            string env = Environment.GetEnvironmentVariable("KONLULU_ENVIRONMENT");

            IConfigurationBuilder builder = new ConfigurationBuilder()
                              .AddJsonFile($"appsettings.json", true, true)
                              .AddEnvironmentVariables("KONLULU");
            return builder.Build();
        }

        private static void ConfigureServices(HostBuilderContext context, IServiceCollection services)
        {
            InitDatabase();

            configuration = LoadConfiguration();
            services.AddSingleton<IConfiguration>(configuration)
                    .AddSingleton<DiscordSocketClient>()
                    .AddSingleton<CommandService>()
                    .AddSingleton<CommandHandler>()
                    .AddSingleton<HttpClient>()

                    .AddSingleton<ILiteDatabase>(new LiteDatabase(configuration["_CONNSTR"]))
                    .AddTransient(typeof(IBaseDatabaseHandler<>), typeof(BaseDatabaseHandler<>))
                    .AddTransient<IGameDatabaseHandler, GameDatabaseHandler>()
                    .AddTransient<IPlayerDatabaseHandler, PlayerDatabaseHandler>()
                    .AddTransient<IGamePlayerDatabaseHandler, GamePlayerDatabaseHandler>()

                    .AddSingleton(typeof(IBackgroundTaskQueue<>), typeof(BackgroundTaskQueue<>))
                    .AddHostedService<DiscordHandlerHostedService>()
                    .AddHostedService<RecurringKonluluTimerHostedService>()
                    .AddHostedService<FuseKonluluTimerHostedService>()
                    .AddSingleton<Random>(new Random())
                    ;
            foreach (ServiceDescriptor service in services)
            {
                Console.WriteLine($"Service: {service.ServiceType.FullName}\n      Lifetime: {service.Lifetime}\n      Instance: {service.ImplementationType?.FullName}");
            }
        }

        private static void InitDatabase()
        {
            BsonMapper mapper = BsonMapper.Global;

            mapper.Entity<PlayerEntity>()
                  .Id(x => x.Id);
            mapper.Entity<GameEntity>()
                  .Id(x => x.Id)
                  .DbRef(x => x.PlayerWon, nameof(PlayerEntity));
            mapper.Entity<GamePlayerEntity>()
                  .Id(x => x.Id)
                  .DbRef(x => x.Player, nameof(PlayerEntity))
                  .DbRef(x => x.Game, nameof(GameEntity));
        }
    }
}
