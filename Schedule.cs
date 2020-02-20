using Discord.Commands;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Color = System.Drawing.Color;
using ImageFormat = System.Drawing.Imaging.ImageFormat;

namespace Alfie_Host
{
    [Group("schedule"), Alias("sch", "s")]
    public class Schedule : ModuleBase<SocketCommandContext>
    {
        private Color defaultBackground = Color.FromArgb(255, 54, 57, 63);
        private Color defaultForeground = Color.FromArgb(255, 255, 255, 255);
        private const string scheduleTimeFile = "scheduletime.txt";

        [Command("day"), Alias("d")]
        public async Task Day([Remainder] string day = "today")
        {
            DateTime date;
            switch (day)
            {
                case "today":
                    date = DateTime.Today;
                    break;
                case "yesterday":
                case "y":
                    date = DateTime.Today.AddDays(-1);
                    break;
                case "tomorrow":
                case "t":
                    date = DateTime.Today.AddDays(1);
                    break;
                default:
                    if (!DateTime.TryParseExact(day, new string[]{ "d.M.y", "d/M/y", "d M y", "d.M", "d/M", "d M" },CultureInfo.InvariantCulture, DateTimeStyles.None, out date))
                    {
                        if (int.TryParse(day, out int tmp))
                        {
                            try
                            {
                                date = new DateTime(DateTime.Today.Year, DateTime.Today.Month, tmp);
                            }
                            catch
                            {
                                await ReplyAsync("Incorrect argument.");
                                return;
                            }
                        }
                        else
                        {
                            await ReplyAsync("Incorrect argument.");
                            return;
                        }
                    }
                    break;
            }

            GroupAndColors groupAndColors = await _getGroupAndColors();

            if (groupAndColors.Group == 0)
                return;

            var schedule = await RSUHScheduleAPI.GetSchedule(groupAndColors.Group, date, date);
            if (schedule == null)
            {
                await ReplyAsync("No data found for specified period.");
                return;
            }
            MemoryStream imgStream = new MemoryStream();
            RSUHScheduleAPI.ScheduleToImage(
                schedule,
                groupAndColors.ForegroundColor,
                groupAndColors.BackgroundColor
            ).Save(imgStream, ImageFormat.Png);
            imgStream.Position = 0;
            await Context.Channel.SendFileAsync(imgStream, DateTime.Now.ToString().Replace(' ', '_') + ".png");
            imgStream.Close();
            imgStream.Dispose();

            return;
        }
        private struct GroupAndColors
        {
            public int Group;
            public Color BackgroundColor;
            public Color ForegroundColor;
        }
        private async Task<GroupAndColors> _getGroupAndColors()
        {
            var groups = await Data.GroupStorage.Load();
            if (groups == null)
            {
                await ReplyAsync("No groups data found, fetching it may take a while...");
                groups = await RSUHScheduleAPI.GetGroups();
                await Data.GroupStorage.Save(groups);
            }
            var user = await Data.UserStorage.GetData(Context.User.Id);
            if (user == null)
            {
                await ReplyAsync("Specify your group id using `schedule id <id>` first.\n" +
                    "You can find out your group id using `schedule find <search pattern>`.");
                return new GroupAndColors { Group = 0 };
            }
            string groupkey;
            try
            {
                groupkey = user["group_id"] as string;
            }
            catch
            {
                await ReplyAsync("Specify your group id using `schedule id <id>` first.\n" +
                    "You can find out your group id using `schedule find <search pattern>`.");
                return new GroupAndColors { Group = 0 };
            }
            int group;
            Color background;
            Color foreground;
            try
            {
                group = groups[groupkey];
            }
            catch
            {
                await ReplyAsync($"Couldn't find group id for **`{groupkey}`**. Try choosing a group again with `schedule id <id>`.");
                return new GroupAndColors { Group = 0 };
            }
            try
            {
                background = ColorTranslator.FromHtml(user["schedule_background"] as string);
            }
            catch
            {
                background = defaultBackground;
            }

            try
            {
                foreground = ColorTranslator.FromHtml(user["schedule_foreground"] as string);
            }
            catch
            {
                foreground = defaultForeground;
            }
            
            return new GroupAndColors
            {
                Group = group,
                BackgroundColor = background,
                ForegroundColor = foreground
            };
        }

        [Command("week"), Alias("w")]
        public async Task Week(string period = "this")
        {
            DateTime startDate;
            DateTime endDate;
            switch (period)
            {
                case "this":
                case "t":
                    startDate = DateTime.Today.Date.AddDays(-(int)DateTime.Today.DayOfWeek);
                    endDate = DateTime.Today.Date.AddDays(7 - (int)DateTime.Today.DayOfWeek);
                    break;
                case "previous":
                case "p":
                    startDate = DateTime.Today.Date.AddDays(-7 - (int)DateTime.Today.DayOfWeek);
                    endDate = DateTime.Today.Date.AddDays(-(int)DateTime.Today.DayOfWeek);
                    break;
                case "next":
                case "n":
                    startDate = DateTime.Today.Date.AddDays(7 - (int)DateTime.Today.DayOfWeek);
                    endDate = DateTime.Today.Date.AddDays(14 - (int)DateTime.Today.DayOfWeek);
                    break;
                default:
                    if (int.TryParse(period, out int week))
                    {
                        startDate = DateTime.Today.Date.AddDays(7 * week - (int)DateTime.Today.DayOfWeek);
                        endDate = DateTime.Today.Date.AddDays(7 * week - (int)DateTime.Today.DayOfWeek + 7);
                    }
                    else
                    {
                        await ReplyAsync("Incorrect argument.");
                        return;
                    }
                    break;
            }

            GroupAndColors groupAndColors = await _getGroupAndColors();

            if (groupAndColors.Group == 0)
                return;



            var schedule = await RSUHScheduleAPI.GetSchedule(groupAndColors.Group, startDate, endDate);
            if (schedule == null)
            {
                await ReplyAsync("No data found for specified period.");
                return;
            }
            MemoryStream imgStream = new MemoryStream();
            RSUHScheduleAPI.ScheduleToImage(
                schedule,
                groupAndColors.ForegroundColor,
                groupAndColors.BackgroundColor
            ).Save(imgStream, ImageFormat.Png);
            imgStream.Position = 0;
            await Context.Channel.SendFileAsync(imgStream, DateTime.Now.ToString().Replace(' ', '_') + ".png");
            imgStream.Close();
            imgStream.Dispose();

            return;
        }

        [Command("group"), Alias("id", "groupid")]
        public async Task GroupId(int id)
        {
            var groups = await Data.GroupStorage.Load();
            if (groups == null)
            {
                await ReplyAsync("No groups data found, fetching it may take a while...");
                groups = await RSUHScheduleAPI.GetGroups();
                await Data.GroupStorage.Save(groups);
            }
            string group = groups.Single((KeyValuePair<string, int> pair) => { return pair.Value == id; }).Key;
            if (!Data.UserStorage.Exists(Context.User.Id))
                await Data.UserStorage.Create(Context.User.Id);
            var user = await Data.UserStorage.GetData(Context.User.Id);
            try
            {
                user["group_id"] = group;
            }
            catch
            {
                user.Add("group_id", group);
            }
            await Data.UserStorage.Save(Context.User.Id, user);
            await ReplyAsync($"Setting schedule group of {Context.User.Mention} to **`{group}`**.");
        }

        [Command("find"), Alias("search", "f", "s")]
        public async Task Find([Remainder] string pattern)
        {
            var groups = await Data.GroupStorage.Load();
            if (groups == null)
            {
                await ReplyAsync("No groups data found, fetching it may take a while...");
                groups = await RSUHScheduleAPI.GetGroups();
                await Data.GroupStorage.Save(groups);
            }
            string prosessedpattern = pattern;
            char[] specialchars = { '.', '$', '^', '{', '[', '(', '|', ')', '+', '\\'};
            foreach (char c in specialchars)
                prosessedpattern = prosessedpattern.Replace(c.ToString(), "[" + c + "]");
            prosessedpattern = prosessedpattern.Replace("*", ".*");
            prosessedpattern = prosessedpattern.Replace("?", ".?");
            prosessedpattern = ".*" + prosessedpattern + ".*";
            List<KeyValuePair<string, int>> output = groups.Where((KeyValuePair<string, int> pair) => { return Regex.IsMatch(pair.Key, prosessedpattern, RegexOptions.IgnoreCase); }).ToList();
            string reply = "";
            if (output.Count == 0)
                reply += "Nothing found matching your pattern.";
            else if (output.Count > 30)
                reply += "Too many groups found, try using more specific pattern.";
            else
            {
                reply += "Found following groups:\n" +
                         "```ID   Group\n";
                foreach (var group in output)
                    reply += $"{group.Value.ToString().PadRight(4)} {group.Key}\n";
                reply += "```";
            }
            await ReplyAsync(reply);
            return;
        }

        [Command("color"), Alias("colour", "c")]
        public async Task SColor(string background, string foreground)
        {
            Color backgroundColor;
            Color foregroundColor;
            try
            {
                backgroundColor = ColorTranslator.FromHtml(background);
            }
            catch
            {
                backgroundColor = defaultBackground;
            }
            try
            {
                foregroundColor = ColorTranslator.FromHtml(foreground);
            }
            catch
            {
                foregroundColor = defaultForeground;
            }
            
            if (!Data.UserStorage.Exists(Context.User.Id))
                await Data.UserStorage.Create(Context.User.Id);
            var user = await Data.UserStorage.GetData(Context.User.Id);
            try
            {
                user["schedule_background"] = backgroundColor;
            }
            catch
            {
                user.Add("schedule_background", backgroundColor);
            }
            try
            {
                user["schedule_foreground"] = foregroundColor;
            }
            catch
            {
                user.Add("schedule_foreground", foregroundColor);
            }
            await Data.UserStorage.Save(Context.User.Id, user);
            await ReplyAsync($"Setting background and foreground colors for {Context.User.Mention} to " +
                $"`{"#" + backgroundColor.A.ToString("X2") + backgroundColor.R.ToString("X2") + backgroundColor.G.ToString("X2") + backgroundColor.B.ToString("X2")}` and " +
                $"`{"#" + foregroundColor.A.ToString("X2") + foregroundColor.R.ToString("X2") + foregroundColor.G.ToString("X2") + foregroundColor.B.ToString("X2")}` respectively.");
        }
        [Command("time"), Alias("t")]
        public async Task Time()
        {
            if(File.Exists(Program.StartUpPath + scheduleTimeFile))
            {
                StreamReader reader = new StreamReader(Program.StartUpPath + scheduleTimeFile);
                await ReplyAsync(await reader.ReadToEndAsync());
                reader.Close();
                reader.Dispose();
            }
            else
            {
                StreamWriter writer = new StreamWriter(Program.StartUpPath + scheduleTimeFile);
                await writer.WriteAsync("No data set to display for this command.");
                writer.Close();
                writer.Dispose();
                await ReplyAsync("No data set to display for this command.");
            }
            return;
        }
    }
}
