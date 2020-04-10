using Discord;
using Discord.Commands;
using Discord.WebSocket;
using konlulu.DAL;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
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
            using (ServiceProvider services = ConfigureServices())
            {
                LoadConfiguration();
                DiscordSocketClient client = services.GetRequiredService<DiscordSocketClient>();
                client.Log += Log;
                services.GetRequiredService<CommandService>().Log += Log;

                string token = configuration["_BOTTOKEN"];

                await client.LoginAsync(TokenType.Bot, token);
                await client.StartAsync();

                var dd = services.GetRequiredService<IDatabaseHandler>();

                await services.GetRequiredService<CommandHandler>().InstallCommandsAsync();
                
                //infinite wait
                await Task.Delay(-1);
            }
        }

        private static IConfiguration configuration;

        private Task Log(LogMessage msg)
        {
            Console.WriteLine(msg.ToString());
            return Task.CompletedTask;
        }

        private static IConfiguration LoadConfiguration()
        {
            string env = Environment.GetEnvironmentVariable("KONLULU_ENVIRONMENT");

            IConfigurationBuilder builder = new ConfigurationBuilder()
                              .AddJsonFile($"appsettings.json", true, true)
                              .AddEnvironmentVariables("KONLULU");
            return builder.Build();
        }

        private static ServiceProvider ConfigureServices()
        {
            configuration = LoadConfiguration();
            IServiceCollection serviceCollection = new ServiceCollection()
                                                    .AddSingleton<IConfiguration>(configuration)
                                                    .AddSingleton<DiscordSocketClient>()
                                                    .AddSingleton<CommandService>()
                                                    .AddSingleton<CommandHandler>()
                                                    .AddSingleton<HttpClient>()
                                                    .AddTransient<IDatabaseHandler, DatabaseHandler>()
                                                    ;
            foreach (ServiceDescriptor service in serviceCollection)
            {
                Console.WriteLine($"Service: {service.ServiceType.FullName}\n      Lifetime: {service.Lifetime}\n      Instance: {service.ImplementationType?.FullName}");
            }

            return serviceCollection.BuildServiceProvider();
        }
    }
}
