using Discord.Commands;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace Alfie_Host
{
    [Group("cats"), Alias("cat")]
    public class Cats : ModuleBase<SocketCommandContext>
    {
        [Command(RunMode = RunMode.Async)]
        public async Task RandomImage()
        {
            var response = await Program.client.GetAsync("https://api.thecatapi.com/v1/images/search");
            var jsonObject = JsonConvert.DeserializeObject<object[]>(await response.Content.ReadAsStringAsync());
            response = await Program.client.GetAsync(JsonConvert.DeserializeObject<Dictionary<string, object>>(jsonObject[0].ToString())["url"] as string);
            Stream catImage = await response.Content.ReadAsStreamAsync();
            await Context.Channel.SendFileAsync(catImage, DateTime.Now.ToString().Replace(' ', '_') + ".jpg");
            
            return;
        }

        [Command("fact", RunMode = RunMode.Async), Alias("facts","f")]
        public async Task RandomFact()
        {
            var response = await Program.client.GetAsync("https://catfact.ninja/fact");
            var jsonObject = JsonConvert.DeserializeObject<Dictionary<string, object>>(await response.Content.ReadAsStringAsync());
            await Context.Channel.SendMessageAsync(jsonObject["fact"] as string);

            return;
        }
    }
}
