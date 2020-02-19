using System;
using Discord;
using Discord.Net;
using Discord.API;
using Discord.Webhook;
using Discord.WebSocket;
using Discord.Commands;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace Alfie_Host
{
    public class Say : ModuleBase<SocketCommandContext>
    {
        [Command("say")]
        public Task SayAsync([Remainder] string echo)
            => ReplyAsync(echo);
    }
}
