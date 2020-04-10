using Discord.Commands;
using konlulu.DAL;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace konlulu.Modules
{
    public class KonluluModule : ModuleBase<SocketCommandContext>
    {
        private readonly IDatabaseHandler db;

        public KonluluModule(IDatabaseHandler databaseHandler)
        {
            this.db = databaseHandler;
        }

        protected override void BeforeExecute(CommandInfo command)
        {
            base.BeforeExecute(command);
        }

        [Command("init")]
        [Summary("Initiate a konlulu~ doll game")]
        public Task InitiateAsync()
        {
            db.Save<>
            return base.ReplyAsync("init");
        }

        [Command("reg")]
        [Summary("Participate in a konlulu~ doll game")]
        public Task RegisterAsync()
        {
            return base.ReplyAsync("register");
        }

        [Command("start")]
        [Summary("Start a konlulu~ doll game")]
        public Task StartAsync()
        {
            return base.ReplyAsync("start");
        }

        [Command("pass")]
        [Summary("Pass the konlulu~ doll")]
        public Task PassAsync()
        {
            return base.ReplyAsync("pass");
        }

        [Command("offer")]
        [Summary("Offer to the konlulu~ doll")]
        public Task OfferAsync([Summary("amount of offer")]int offer)
        {
            return base.ReplyAsync($"offered {offer}");
        }
    }
}
