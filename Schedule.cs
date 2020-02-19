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

using Color = System.Drawing.Color;
using ImageFormat = System.Drawing.Imaging.ImageFormat;

namespace Alfie_Host
{
    [Group("schedule"), Alias("sch", "s")]
    public class Schedule : ModuleBase<SocketCommandContext>
    {
        [Command("week"), Alias("w")]
        public async Task SayAsync(string period = "today")
        {
            
            MemoryStream imgStream = new MemoryStream();
            RSUHScheduleAPI.ScheduleToImage(await RSUHScheduleAPI.GetSchedule(581, DateTime.Today.Date.AddDays(-(int)DateTime.Today.DayOfWeek), DateTime.Today.Date.AddDays(7-(int)DateTime.Today.DayOfWeek)), Color.White, Color.Black).Save(imgStream, ImageFormat.Png);
            //await RSUHScheduleAPI.GetSchedule(581, DateTime.Today, DateTime.Today.AddDays(1));
            imgStream.Position = 0;
            await Context.Channel.SendFileAsync(imgStream, DateTime.Today.ToString().Replace(' ', '_') + ".png");
            return;
        }
    }
}
