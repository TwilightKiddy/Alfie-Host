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
using System.Reflection;

namespace Alfie_Host
{
    public class CommandHandler
    {
        private readonly DiscordSocketClient _client;
        private readonly CommandService _commands;
        private Func<LogMessage, Task> _logmethod;

        public CommandHandler(DiscordSocketClient client, CommandService commands, Func<LogMessage, Task> logmethod)
        {
            _commands = commands;
            _client = client;
            _logmethod = logmethod;
        }

        public async Task InstallCommandsAsync()
        {
            _client.MessageReceived += HandleCommandAsync;


            await _commands.AddModulesAsync(assembly: Assembly.GetEntryAssembly(),
                                            services: null);
        }

        private async Task HandleCommandAsync(SocketMessage messageParam)
        {
            var message = messageParam as SocketUserMessage;
            if (message == null) return;

            int argPos = 0;
            var context = new SocketCommandContext(_client, message);
            if (!(message.HasMentionPrefix(_client.CurrentUser, ref argPos) ||
                await Data.ChannelStorage.Contains(context.Channel.Id) ||
                context.IsPrivate) ||
                message.Author.IsBot)
                return;
            
            var result = await _commands.ExecuteAsync(
                context: context,
                argPos: argPos,
                services: null);

            await _logmethod(new LogMessage(
                severity: LogSeverity.Info,
                source: "Chat",
                message: $"{context.User.Username}#{context.User.Discriminator} executed a command: {message}."
            ));
            if (!result.IsSuccess)
                await context.Channel.SendMessageAsync(result.ErrorReason);
        }
    }
}
