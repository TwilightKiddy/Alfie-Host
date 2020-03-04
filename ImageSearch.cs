using Alfie_Host.Data;
using Discord.Commands;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Alfie_Host
{
    public class ImageSearch : ModuleBase<SocketCommandContext>
    {
        [Command("imagesearch", RunMode = RunMode.Async), Alias("image", "search", "img")]
        public async Task Search(string random, [Remainder] string _query = "")
        {
            if(!Regex.IsMatch(_query, "^[a-z ]*$", RegexOptions.IgnoreCase)||
               !Regex.IsMatch(random, "^[a-z ]*$", RegexOptions.IgnoreCase))
            {
                await ReplyAsync("Query must consist of only english letters and spaces.");
                return;
            }
            string query;
            if (!new string[]{ "r", "random", "rng" }.Contains(random))
                query = Regex.Replace(random.ToLower(), "[ ]{2,}", " ") + " " + Regex.Replace(_query.ToLower(), "[ ]{2,}", " ");
            else
                query = Regex.Replace(_query.ToLower(), "[ ]{2,}", " ");
            string key = await Pixabay.GetKey();
            var _response = await Program.client.GetAsync($"https://pixabay.com/api/?key={key}&q={query}&per_page=3");
            if (_response.StatusCode != HttpStatusCode.OK)
            {
                await ReplyAsync(await _response.Content.ReadAsStringAsync());
                return;
            }
            
            var response = JsonConvert.DeserializeObject<Dictionary<string, object>>(await _response.Content.ReadAsStringAsync());

            int totalHits = int.Parse(response["totalHits"].ToString());
            int hit = Program.Random.Next(totalHits);
            int page = 1;
            if (new string[] { "r", "random", "rng" }.Contains(random))
            {
                page = hit / 3 + 1;
                hit %= 3;
            }
            else
                hit = 0;
            if (page != 1)
            {
                _response = await Program.client.GetAsync($"https://pixabay.com/api/?key={key}&q={query}&per_page=3&page={page}");
                if (_response.StatusCode != HttpStatusCode.OK)
                {
                    await ReplyAsync(await _response.Content.ReadAsStringAsync());
                    return;
                }
            }
            response = JsonConvert.DeserializeObject<Dictionary<string, object>>(await _response.Content.ReadAsStringAsync());

            var hits = JsonConvert.DeserializeObject<Dictionary<string, object>[]>(response["hits"].ToString());
            string imageUrl;
            try
            {
                imageUrl = hits[hit]["largeImageURL"] as string;
            }
            catch
            {
                await ReplyAsync("Nothing found matching your query.");
                return;
            }
            _response = await Program.client.GetAsync(imageUrl);
            Stream image = await _response.Content.ReadAsStreamAsync();
            await Context.Channel.SendFileAsync(image, DateTime.Now.ToString().Replace(' ', '_') + ".jpg", "Powered by https://pixabay.com/.");

            return;
        }
    }
}
