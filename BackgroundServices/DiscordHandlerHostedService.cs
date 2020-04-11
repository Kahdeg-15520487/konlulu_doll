using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace konlulu.BackgroundServices
{
    class DiscordHandlerHostedService : BackgroundService
    {
        private readonly IServiceProvider services;
        private readonly IConfiguration configuration;

        public DiscordHandlerHostedService(IServiceProvider services, IConfiguration configuration)
        {
            this.services = services;
            this.configuration = configuration;
        }

        public override Task StartAsync(CancellationToken cancellationToken)
        {
            return base.StartAsync(cancellationToken);
        }
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            DiscordSocketClient client = services.GetRequiredService<DiscordSocketClient>();
            client.Log += Log;
            services.GetRequiredService<CommandService>().Log += Log;

            string token = configuration["_BOTTOKEN"];

            await client.LoginAsync(TokenType.Bot, token);
            await client.StartAsync();
            try
            {
                await services.GetRequiredService<CommandHandler>().InstallCommandsAsync();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                Console.WriteLine(ex.StackTrace);
            }

            //infinite wait
            await Task.Delay(-1);
        }
        public override Task StopAsync(CancellationToken cancellationToken)
        {
            return base.StopAsync(cancellationToken);
        }

        private Task Log(LogMessage msg)
        {
            Console.WriteLine(msg.ToString());
            return Task.CompletedTask;
        }
    }
}
