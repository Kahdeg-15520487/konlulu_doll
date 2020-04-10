using Discord.Commands;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace konlulu.Modules
{
    //698288795577221200
    public class TestModule : ModuleBase<SocketCommandContext>
    {
        [Command("emoji")]
        [Summary("print animated emoji")]
        public Task SayAsync([Summary("The emoji's name")] string name, [Summary("the emoji's id")] string id)
        {
            Console.WriteLine($"<a:{name}:{id}>");
            return ReplyAsync($"<a:{name}:{id}>");
        }
    }
}
