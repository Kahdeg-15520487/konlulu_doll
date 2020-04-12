using Discord;
using Discord.Commands;
using Discord.WebSocket;
using konlulu.BackgroundServices;
using konlulu.DAL;
using konlulu.DAL.Entity;
using konlulu.DAL.Interfaces;
using konlulu.Modules;
using LiteDB;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Net.Http;
using System.Text;
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
                      .ConfigureLogging((hostContext, builder) =>
                      {
                          builder.ClearProviders();
                          builder.AddConsole();
                          builder.AddFile("konluludoll-{Date}.txt");
                      })
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

            configuration = LoadConfiguration();

            InitDatabase(configuration);

            services.AddSingleton<IConfiguration>(configuration)
                    .AddSingleton<DiscordSocketClient>()
                    .AddSingleton<CommandService>()
                    .AddSingleton<CommandHandler>()
                    .AddSingleton<HttpClient>()

                    .AddSingleton<ILiteDatabase>(new LiteDatabase(configuration["_CONNSTR"]))
                    .AddTransient(typeof(IBaseRepository<>), typeof(BaseRepository<>))
                    .AddTransient<IGameRepository, GameRepository>()
                    .AddTransient<IPlayerRepository, PlayerRepository>()
                    .AddTransient<IGamePlayerRepository, GamePlayerRepository>()
                    .AddTransient<IConfigRepository, ConfigRepository>()

                    .AddSingleton(typeof(IBackgroundTaskQueue<>), typeof(BackgroundTaskQueue<>))
                    .AddHostedService<DiscordHandlerHostedService>()
                    .AddHostedService<RecurringKonluluTimerHostedService>()
                    .AddHostedService<FuseKonluluTimerHostedService>()
                    .AddSingleton<Random>(new Random())
                    ;
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("Loaded service: ");
            foreach (ServiceDescriptor service in services)
            {
                sb.AppendLine($"Service: {service.ServiceType.FullName}\n      Lifetime: {service.Lifetime}\n      Instance: {service.ImplementationType?.FullName}");
            }

        }

        private static void InitDatabase(IConfiguration configuration)
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

            //seed config
            using (LiteDatabase db = new LiteDatabase(configuration["_CONNSTR"]))
            {
                ConfigRepository configDb = new ConfigRepository(db);

                ConfigEntity OFFER_COOLDOWN_config = new ConfigEntity(nameof(KonluluModule.OFFER_COOLDOWN), KonluluModule.OFFER_COOLDOWN);
                ConfigEntity KON_TIME_config = new ConfigEntity(nameof(KonluluModule.KON_TIME), KonluluModule.KON_TIME);
                ConfigEntity MAX_FUSE_TIME_config = new ConfigEntity(nameof(KonluluModule.MAX_FUSE_TIME), KonluluModule.MAX_FUSE_TIME);
                ConfigEntity MIN_FUSE_TIME_config = new ConfigEntity(nameof(KonluluModule.MIN_FUSE_TIME), KonluluModule.MIN_FUSE_TIME);
                ConfigEntity MAX_OFFER_config = new ConfigEntity(nameof(KonluluModule.MAX_OFFER), KonluluModule.MAX_OFFER);
                ConfigEntity MIN_PLAYER_COUNT_config = new ConfigEntity(nameof(KonluluModule.MIN_PLAYER_COUNT), KonluluModule.MIN_PLAYER_COUNT);

                configDb.SaveWithoutUpdate(OFFER_COOLDOWN_config);
                configDb.SaveWithoutUpdate(KON_TIME_config);
                configDb.SaveWithoutUpdate(MAX_FUSE_TIME_config);
                configDb.SaveWithoutUpdate(MIN_FUSE_TIME_config);
                configDb.SaveWithoutUpdate(MAX_OFFER_config);
                configDb.SaveWithoutUpdate(MIN_PLAYER_COUNT_config);
            }
        }
    }
}
