using Discord.Commands;
using System.Threading.Tasks;

namespace Alfie_Host
{
    public class Say : ModuleBase<SocketCommandContext>
    {
        [Command("say")]
        public async Task SayAsync([Remainder] string echo)
        {
            try
            {
                await Context.Message.DeleteAsync();
            }
            catch { }
            await ReplyAsync(echo);
            return;
        }
    }
}
