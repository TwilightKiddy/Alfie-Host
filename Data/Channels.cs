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

namespace Alfie_Host.Data
{
    [Group("channel"), Alias("channels", "c")]
    public class Channels : ModuleBase<SocketCommandContext>
    {
        
        [Command("remember"), Alias("r")]
        public async Task Remember()
        {
            if(await ChannelStorage.Contains(Context.Channel.Id))
                await ReplyAsync("This channel is already in the list of remembered.");
            else
            {
                await ChannelStorage.Add(Context.Channel.Id);
                await ReplyAsync("Remembered channel.");
            }
            return;
        }

        [Command("forget"), Alias("f")]
        public async Task Forget()
        {
            await ChannelStorage.Remove(Context.Channel.Id);
            await ReplyAsync("Forgot channel.");

            return;
        }
    }

    static class ChannelStorage
    {
        const string ChannelsFile = "data\\channels.json";
        public static async Task<bool> Contains(ulong id)
        {
            if (!File.Exists(Program.StartUpPath + ChannelsFile))
                return false;
            StreamReader reader = new StreamReader(Program.StartUpPath + ChannelsFile);
            ulong[] channels = JsonConvert.DeserializeObject<ulong[]>(await reader.ReadToEndAsync());
            reader.Close();
            reader.Dispose();
            return channels.Contains(id);
        }

        public static async Task Add(ulong id)
        {
            ulong[] channels = new ulong[] { };
            if (!Directory.Exists(Path.GetDirectoryName(Program.StartUpPath + ChannelsFile)))
                Directory.CreateDirectory(Path.GetDirectoryName(Program.StartUpPath + ChannelsFile));
            if (File.Exists(Program.StartUpPath + ChannelsFile))
            {
                StreamReader reader = new StreamReader(Program.StartUpPath + ChannelsFile);
                channels = JsonConvert.DeserializeObject<ulong[]>(await reader.ReadToEndAsync());
                reader.Close();
                reader.Dispose();
            }
            if (channels.Contains(id))
                return;
            StreamWriter writer = new StreamWriter(Program.StartUpPath + ChannelsFile);
            
            
            await writer.WriteAsync(JsonConvert.SerializeObject(channels.Append(id)));
            writer.Close();
            writer.Dispose();
        }

        public static async Task Remove(ulong id)
        {
            ulong[] channels = new ulong[] { };
            if (File.Exists(Program.StartUpPath + ChannelsFile))
            {
                StreamReader reader = new StreamReader(Program.StartUpPath + ChannelsFile);
                channels = JsonConvert.DeserializeObject<ulong[]>(await reader.ReadToEndAsync());
                reader.Close();
                reader.Dispose();
            }
            if (!Directory.Exists(Path.GetDirectoryName(Program.StartUpPath + ChannelsFile)))
                Directory.CreateDirectory(Path.GetDirectoryName(Program.StartUpPath + ChannelsFile));
            StreamWriter writer = new StreamWriter(Program.StartUpPath + ChannelsFile);
            await writer.WriteAsync(JsonConvert.SerializeObject(channels.Where((ulong _id) => { return _id != id; })));
            writer.Close();
            writer.Dispose();
        }
    }
}
