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
using System.Timers;

namespace Alfie_Host
{
    public class Program
    {
        const string SettingsFile = "settings.json";
        struct Settings
        {
            public TokenType TokenType;
            public string Token;
            public ActivityType ActivityType;
            public string Activity;
        }

        public static void Main()
        {
            Program program = new Program();
            StartUpPath = Path.GetDirectoryName(new Uri(System.Reflection.Assembly.GetExecutingAssembly().CodeBase).LocalPath) + "\\";
            if (File.Exists(StartUpPath + SettingsFile))
            {
                StreamReader reader = new StreamReader(StartUpPath + SettingsFile);
                program._settings = JsonConvert.DeserializeObject<Settings>(reader.ReadToEnd());
                reader.Close();
                reader.Dispose();
            }
            else
            {
                program._settings = new Settings
                {
                    Token = "XXXXXXXXXXXXXXXXXXXXXXXX.XXXXXX.XXXXXXXXXXXXXXXXXXXXXXXXXXX",
                    TokenType = TokenType.Bot,
                    ActivityType = ActivityType.Playing,
                    Activity = ""
                };
                StreamWriter writer = new StreamWriter(StartUpPath + SettingsFile);
                writer.Write(JsonConvert.SerializeObject(program._settings, Formatting.Indented));
                writer.Close();
                writer.Dispose();
                Console.WriteLine("A default settings file was generated. Review it and restart the application.");
                Console.ReadKey();
                return;
            }
            Console.OutputEncoding = Encoding.UTF8;

            Timer dailyTimer = new Timer((DateTime.Today.AddDays(1) - DateTime.Now).TotalMilliseconds);
            dailyTimer.AutoReset = false;
            dailyTimer.Elapsed += DailyTimer_Elapsed;
            dailyTimer.Start();

            program.MainAsync().GetAwaiter().GetResult();
        }

        private static void DailyTimer_Elapsed(object sender, ElapsedEventArgs e)
        {
            (sender as Timer).Interval = (DateTime.Today.AddDays(1) - DateTime.Now).TotalMilliseconds;
            (sender as Timer).Start();
            try
            {
                Log(new LogMessage(LogSeverity.Info, "Groups", "Group update started."));
                var newGroups = RSUHScheduleAPI.GetGroups().GetAwaiter().GetResult();
                var oldGroups = Data.GroupStorage.Load().GetAwaiter().GetResult();
                var resultGroups = oldGroups.Intersect(newGroups).ToDictionary(pair => pair.Key, pair => pair.Value);
                if (resultGroups == null)
                    resultGroups = new Dictionary<string, int>();
                int cnt = 0;
                foreach(var group in newGroups)
                    if(!resultGroups.Contains(group))
                    {
                        cnt++;
                        resultGroups.Add(group.Key, group.Value);
                    }
                Data.GroupStorage.Save(resultGroups).GetAwaiter().GetResult();
                Log(new LogMessage(LogSeverity.Info, "Groups", $"Group update finished. {cnt} change(s) made."));
            }
            catch(Exception ex)
            {
                Log(new LogMessage(LogSeverity.Info, "Groups", "", ex));
            }
        }

        public static string StartUpPath;
        private DiscordSocketClient _client;
        private Settings _settings;
        private CommandService _commandservice;

        private static Task Log(LogMessage msg)
        {
            if(msg.Exception != null)
                if(msg.Exception.Message == "WebSocket connection was closed")
                    return Task.CompletedTask;
            Console.WriteLine(DateTime.Now.ToString("[dd.MM.yyyy hh:mm:ss] ") + msg.ToString(fullException: false, prependTimestamp: false));
            return Task.CompletedTask;
        }

        public async Task MainAsync()
        {
            _client = new DiscordSocketClient(new DiscordSocketConfig() {
                LogLevel = LogSeverity.Info,
                DefaultRetryMode = RetryMode.AlwaysRetry
            });

            _client.Log += Log;

            _commandservice = new CommandService(new CommandServiceConfig() {
                CaseSensitiveCommands = false,
                LogLevel = LogSeverity.Info
            });

            await _client.LoginAsync(_settings.TokenType, _settings.Token);
            await _client.StartAsync();
            await _client.SetActivityAsync(new Game(_settings.Activity, _settings.ActivityType));
            await new CommandHandler(_client, _commandservice, Log).InstallCommandsAsync();

            await Task.Delay(-1);
        }
    }
}
