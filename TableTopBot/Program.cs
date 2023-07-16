global using Discord;
global using Discord.Rest;
global using Discord.WebSocket;
global using System.Text.Json;

namespace TableTopBot
{
    internal class Program
    {
        private class PrivateVariable
        {
            public string Key { get; set; } = "";
            public ulong Server { get; set; } = 0;
            public ulong LogChannel { get; set; } = 0;
            public SocketGuild SocketServer => Client.GetGuild(Server);
            public SocketTextChannel SocketLogChannel => SocketServer.GetTextChannel(LogChannel);
        }

        ///The bot's client
        private static PrivateVariable PrivateVariables = JsonSerializer.Deserialize<PrivateVariable>(File.ReadAllText("./ProgramPrivateVariables.json")) ?? throw new NullReferenceException("No Private Variable File Found!");
        private static DiscordSocketClient Client = new DiscordSocketClient(new DiscordSocketConfig { GatewayIntents = GatewayIntents.All });
        
        ///Channels
        public static SocketGuild Server => PrivateVariables.SocketServer;

        public static Task Main(string[] args) => new Program().MainAsync();

        private async Task MainAsync()
        {
            Console.Title = "TabletopBot";
            bool acceptingCommands = false;

            ///Required Lambdas
            Client.Log += (LogMessage msg) => ///Console logging
            {
                Console.WriteLine(msg.ToString());
                return Task.CompletedTask;
            };
            Client.SlashCommandExecuted += async (SocketSlashCommand _command) => ///Command logging
            {
                if(!Callbacks.ContainsKey(_command.CommandId)) 
                    return;
                string log = $"[{DateTime.UtcNow}]\nUser: {_command.User.Username}\nCommand: {_command.CommandName}\nParams: ";
                foreach (SocketSlashCommandDataOption o in _command.Data.Options.ToList())
                    log += $"\n{o.Name}: {o.Value}";
                await PrivateVariables.SocketLogChannel.SendMessageAsync(embed: new EmbedBuilder().AddField("Command Executed", log).Build());
            };
            Client.SlashCommandExecuted += async (SocketSlashCommand _command) => ///Command calls
            {
                if (Callbacks.ContainsKey(_command.CommandId))
                {
                    Interactions.Add(_command, new Interaction(_command));
                    try { await Callbacks[_command.CommandId](_command); }
                    catch (Exception ex)
                    {
                        string error = $"[{DateTime.UtcNow}] Error: {ex.Message}";
                        Console.WriteLine(error);
                        await Interactions[_command].Respond(text: error);
                        await PrivateVariables.SocketLogChannel.SendMessageAsync(text: error);
                    }
                }
            };
            Client.ButtonExecuted += async (SocketMessageComponent _button) => ///Buttons
            { 
                if (Button.Buttons.ContainsKey(_button.Data.CustomId))
                {
                    await _button.DeferAsync();
                    await Button.Buttons[_button.Data.CustomId].Press();
                    Button.Buttons[_button.Data.CustomId].DeleteButton();
                }
            };
            Client.SelectMenuExecuted += async (SocketMessageComponent _selectMenu) => ///Buttons
            { 
                switch(_selectMenu.Data.CustomId)
                {
                    case "help":
                        try
                        {
                            await _selectMenu.DeferAsync();
                            Command command = Command.allCommands.Find(c => c.name == string.Join(", ", _selectMenu.Data.Values)) ?? throw new Exception("Error: Cannot find command!");
                            await _selectMenu.ModifyOriginalResponseAsync(m =>{m.Embed = new EmbedBuilder()
                            .WithTitle($"**{command.name}**").WithDescription($"{command.extendedDecription}" + $"\n\n__Parameters__\n{command.parameters}")
                            .WithColor(command.modOnly ? Color.Red : Color.Blue).Build(); });
                        }
                        catch {throw;}
                        break;
                }
            };

            ///Init all moduels
            new XPModule(this);

            ///Fully connected
            Client.Connected += async () =>
            {
                await AddCommand(new Command()
                {
                    name = "help",
                    description = "get information about this bot's commands",
                    extendedDecription = "Displays information about all commands from TabletopBot",
                    callback = async Task (SocketSlashCommand _command) =>
                    {
                        bool mod = Server.GetUser(_command.User.Id).GuildPermissions.KickMembers;
                        //await RecursiveMuliPageEmbed(_command, Command.allCommands.Where(c => mod ? true : !c.modOnly).Select(c => new EmbedBuilder().WithDescription(c.description).WithTitle(c.name)).ToArray());
                        SelectMenuBuilder menu = new SelectMenuBuilder().WithPlaceholder("Select Commands").WithCustomId("help");
                        if(mod){
                            foreach(Command c in Command.allCommands){
                                menu.AddOption(new SelectMenuOptionBuilder().WithLabel(c.name).WithDescription(c.description).WithValue(c.name));
                            }
                        }
                        else
                        {
                            foreach(Command c in Command.allCommands.Where(g => g.modOnly == false)){
                                menu.AddOption(new SelectMenuOptionBuilder().WithLabel(c.name).WithDescription(c.description).WithValue(c.name));
                            }
                        }
                        await _command.RespondAsync(embed: new EmbedBuilder().WithTitle("List of Commands").WithDescription("Select a command below to view their descriptions:").WithColor(Color.Orange).Build(), components: new ComponentBuilder().WithSelectMenu(menu).Build(),ephemeral: true);
                    }
                });
                await Client.SetGameAsync("Board Games");
                acceptingCommands = true;
            };

            ///Threads
            bool end = false;

            Thread update = new Thread(() => {
                while (!end)
                {
                    Interaction.UpdateInteractions();
                    Button.UpdateButtons();
                    Thread.Sleep(1000);
                }
            });
            Thread consoleCommands = new Thread(() => {
                while (!end)
                {
                    string input = (Console.ReadLine() ?? "").ToLower();
                    if (input == "quit")
                        end = true;
                }
            });

            ///Run bot
            await Client.LoginAsync(TokenType.Bot, PrivateVariables.Key);
            await Client.StartAsync();
            await Client.SetGameAsync("Getting The Cart Out");
            while (!acceptingCommands) await Task.Delay(1000);
            update.Start();
            consoleCommands.Start();
            while (!end) await Task.Delay(1000);
            await PrivateVariables.SocketServer.DeleteApplicationCommandsAsync();
            await Client.LogoutAsync();
            
        }

        ///all possible callback events that can be used
        #region Callback Adders
        public void AddApplicationCommandCreatedCallback(Func<SocketApplicationCommand, Task> f) => Client.ApplicationCommandCreated += f; 
        public void AddApplicationCommandDeletedCallback(Func<SocketApplicationCommand, Task> f) => Client.ApplicationCommandDeleted += f; 
        public void AddApplicationCommandUpdatedCallback(Func<SocketApplicationCommand, Task> f) => Client.ApplicationCommandUpdated += f; 
        public void AddAutocompleteExecutedCallback(Func<SocketAutocompleteInteraction, Task> f) => Client.AutocompleteExecuted += f; 
        public void AddButtonExecutedCallback(Func<SocketMessageComponent, Task> f) => Client.ButtonExecuted += f; 
        public void AddChannelCreatedCallback(Func<SocketChannel, Task> f) => Client.ChannelCreated += f; 
        public void AddChannelDestroyedCallback(Func<SocketChannel, Task> f) => Client.ChannelDestroyed += f; 
        public void AddChannelUpdatedCallback(Func<SocketChannel, SocketChannel, Task> f) => Client.ChannelUpdated += f; 
        public void AddConnectedCallback(Func<Task> f) => Client.Connected += f; 
        public void AddCurrentUserUpdatedCallback(Func<SocketSelfUser, SocketSelfUser, Task> f) => Client.CurrentUserUpdated += f; 
        public void AddDisconnectedCallback(Func<Exception, Task> f) => Client.Disconnected += f; 
        public void AddGuildAvailableCallback(Func<SocketGuild, Task> f) => Client.GuildAvailable += f; 
        public void AddGuildJoinRequestDeletedCallback(Func<Cacheable<SocketGuildUser, ulong>, SocketGuild, Task> f) => Client.GuildJoinRequestDeleted += f; 
        public void AddGuildMembersDownloadedCallback(Func<SocketGuild, Task> f) => Client.GuildMembersDownloaded += f; 
        public void AddGuildMemberUpdatedCallback(Func<Cacheable<SocketGuildUser, ulong>, SocketGuildUser, Task> f) => Client.GuildMemberUpdated += f; 
        public void AddGuildScheduledEventCancelledCallback(Func<SocketGuildEvent, Task> f) => Client.GuildScheduledEventCancelled += f; 
        public void AddGuildScheduledEventCompletedCallback(Func<SocketGuildEvent, Task> f) => Client.GuildScheduledEventCompleted += f; 
        public void AddGuildScheduledEventCreatedCallback(Func<SocketGuildEvent, Task> f) => Client.GuildScheduledEventCreated += f; 
        public void AddGuildScheduledEventUpdatedCallback(Func<Cacheable<SocketGuildEvent, ulong>, SocketGuildEvent, Task> f) => Client.GuildScheduledEventUpdated += f; 
        public void AddGuildScheduledEventUserAddCallback(Func<Cacheable<SocketUser, RestUser, IUser, ulong>, SocketGuildEvent, Task> f) => Client.GuildScheduledEventUserAdd += f; 
        public void AddGuildScheduledEventUserRemoveCallback(Func<Cacheable<SocketUser, RestUser, IUser, ulong>, SocketGuildEvent, Task> f) => Client.GuildScheduledEventUserRemove += f; 
        public void AddGuildStickerCreatedCallback(Func<SocketCustomSticker, Task> f) => Client.GuildStickerCreated += f; 
        public void AddGuildStickerDeletedCallback(Func<SocketCustomSticker, Task> f) => Client.GuildStickerDeleted += f; 
        public void AddGuildStickerUpdatedCallback(Func<SocketCustomSticker, SocketCustomSticker, Task> f) => Client.GuildStickerUpdated += f; 
        public void AddGuildUnavailableCallback(Func<SocketGuild, Task> f) => Client.GuildUnavailable += f; 
        public void AddGuildUpdatedCallback(Func<SocketGuild, SocketGuild, Task> f) => Client.GuildUpdated += f; 
        public void AddIntegrationCreatedCallback(Func<IIntegration, Task> f) => Client.IntegrationCreated += f; 
        public void AddIntegrationDeletedCallback(Func<IGuild, ulong, Optional<ulong>, Task> f) => Client.IntegrationDeleted += f; 
        public void AddIntegrationUpdatedCallback(Func<IIntegration, Task> f) => Client.IntegrationUpdated += f; 
        public void AddInteractionCreatedCallback(Func<SocketInteraction, Task> f) => Client.InteractionCreated += f; 
        public void AddInviteCreatedCallback(Func<SocketInvite, Task> f) => Client.InviteCreated += f; 
        public void AddInviteDeletedCallback(Func<SocketGuildChannel, string, Task> f) => Client.InviteDeleted += f; 
        public void AddJoinedGuildCallback(Func<SocketGuild, Task> f) => Client.JoinedGuild += f; 
        public void AddLatencyUpdatedCallback(Func<int, int, Task> f) => Client.LatencyUpdated += f; 
        public void AddLeftGuildCallback(Func<SocketGuild, Task> f) => Client.LeftGuild += f; 
        public void AddLogCallback(Func<LogMessage, Task> f) => Client.Log += f; 
        public void AddLoggedInCallback(Func<Task> f) => Client.LoggedIn += f; 
        public void AddLoggedOutCallback(Func<Task> f) => Client.LoggedOut += f; 
        public void AddMessageCommandExecutedCallback(Func<SocketMessageCommand, Task> f) => Client.MessageCommandExecuted += f; 
        public void AddMessageDeletedCallback(Func<Cacheable<IMessage, ulong>, Cacheable<IMessageChannel, ulong>, Task> f) => Client.MessageDeleted += f; 
        public void AddMessageReceivedCallback(Func<SocketMessage, Task> f) => Client.MessageReceived += f; 
        public void AddMessagesBulkDeletedCallback(Func<IReadOnlyCollection<Cacheable<IMessage, ulong>>, Cacheable<IMessageChannel, ulong>, Task> f) => Client.MessagesBulkDeleted += f; 
        public void AddMessageUpdatedCallback(Func<Cacheable<IMessage, ulong>, SocketMessage, ISocketMessageChannel, Task> f) => Client.MessageUpdated += f; 
        public void AddModalSubmittedCallback(Func<SocketModal, Task> f) => Client.ModalSubmitted += f; 
        public void AddPresenceUpdatedCallback(Func<SocketUser, SocketPresence, SocketPresence, Task> f) => Client.PresenceUpdated += f; 
        public void AddReactionAddedCallback(Func<Cacheable<IUserMessage, ulong>, Cacheable<IMessageChannel, ulong>, SocketReaction, Task> f) => Client.ReactionAdded += f; 
        public void AddReactionRemovedCallback(Func<Cacheable<IUserMessage, ulong>, Cacheable<IMessageChannel, ulong>, SocketReaction, Task> f) => Client.ReactionRemoved += f; 
        public void AddReactionsClearedCallback(Func<Cacheable<IUserMessage, ulong>, Cacheable<IMessageChannel, ulong>, Task> f) => Client.ReactionsCleared += f; 
        public void AddReactionsRemovedForEmoteCallback(Func<Cacheable<IUserMessage, ulong>, Cacheable<IMessageChannel, ulong>, IEmote, Task> f) => Client.ReactionsRemovedForEmote += f; 
        public void AddReadyCallback(Func<Task> f) => Client.Ready += f; 
        public void AddRecipientAddedCallback(Func<SocketGroupUser, Task> f) => Client.RecipientAdded += f; 
        public void AddRecipientRemovedCallback(Func<SocketGroupUser, Task> f) => Client.RecipientRemoved += f; 
        public void AddRequestToSpeakCallback(Func<SocketStageChannel, SocketGuildUser, Task> f) => Client.RequestToSpeak += f; 
        public void AddRoleCreatedCallback(Func<SocketRole, Task> f) => Client.RoleCreated += f; 
        public void AddRoleDeletedCallback(Func<SocketRole, Task> f) => Client.RoleDeleted += f; 
        public void AddRoleUpdatedCallback(Func<SocketRole, SocketRole, Task> f) => Client.RoleUpdated += f; 
        public void AddSelectMenuExecutedCallback(Func<SocketMessageComponent, Task> f) => Client.SelectMenuExecuted += f;
        public void AddSlashCommandExecutedCallback(Func<SocketSlashCommand, Task> f) => Client.SlashCommandExecuted += f;
        public void AddSpeakerAddedCallback(Func<SocketStageChannel, SocketGuildUser, Task> f) => Client.SpeakerAdded += f; 
        public void AddSpeakerRemovedCallback(Func<SocketStageChannel, SocketGuildUser, Task> f) => Client.SpeakerRemoved += f; 
        public void AddStageEndedCallback(Func<SocketStageChannel, Task> f) => Client.StageEnded += f; 
        public void AddStageStartedCallback(Func<SocketStageChannel, Task> f) => Client.StageStarted += f; 
        public void AddStageUpdatedCallback(Func<SocketStageChannel, SocketStageChannel, Task> f) => Client.StageUpdated += f; 
        public void AddThreadCreatedCallback(Func<SocketThreadChannel, Task> f) => Client.ThreadCreated += f; 
        public void AddThreadDeletedCallback(Func<Cacheable<SocketThreadChannel, ulong>, Task> f) => Client.ThreadDeleted += f; 
        public void AddThreadMemberJoinedCallback(Func<SocketThreadUser, Task> f) => Client.ThreadMemberJoined += f; 
        public void AddThreadMemberLeftCallback(Func<SocketThreadUser, Task> f) => Client.ThreadMemberLeft += f; 
        public void AddThreadUpdatedCallback(Func<Cacheable<SocketThreadChannel, ulong>, SocketThreadChannel, Task> f) => Client.ThreadUpdated += f; 
        public void AddUserBannedCallback(Func<SocketUser, SocketGuild, Task> f) => Client.UserBanned += f; 
        public void AddUserCommandExecutedCallback(Func<SocketUserCommand, Task> f) => Client.UserCommandExecuted += f; 
        public void AddUserIsTypingCallback(Func<Cacheable<IUser, ulong>, Cacheable<IMessageChannel, ulong>, Task> f) => Client.UserIsTyping += f; 
        public void AddUserJoinedCallback(Func<SocketGuildUser, Task> f) => Client.UserJoined += f; 
        public void AddUserLeftCallback(Func<SocketGuild, SocketUser, Task> f) => Client.UserLeft += f; 
        public void AddUserUnbannedCallback(Func<SocketUser, SocketGuild, Task> f) => Client.UserUnbanned += f; 
        public void AddUserUpdatedCallback(Func<SocketUser, SocketUser, Task> f) => Client.UserUpdated += f;
        public void AddUserVoiceStateUpdatedCallback(Func<SocketUser, SocketVoiceState, SocketVoiceState, Task> f) => Client.UserVoiceStateUpdated += f;
        public void AddVoiceServerUpdatedCallback(Func<SocketVoiceServer, Task> f) => Client.VoiceServerUpdated += f;
        public void AddWebhooksUpdatedCallback(Func<SocketGuild, SocketChannel, Task> f) => Client.WebhooksUpdated += f;
        #endregion

        ///Commands
        private static Dictionary<ulong, Func<SocketSlashCommand, Task>> Callbacks = new Dictionary<ulong, Func<SocketSlashCommand, Task>>();
        public static Dictionary<SocketSlashCommand, Interaction> Interactions = new Dictionary<SocketSlashCommand, Interaction>();
        ///Adds a command to the current guild
        public async Task AddCommand(Command _command)
        {
            try  
            { 
                Callbacks.Add((await PrivateVariables.SocketServer.CreateApplicationCommandAsync(_command.GetCommandBuilder().Build())).Id, _command.callback);
                Command.allCommands.Add(_command);
                Console.WriteLine($"[{DateTime.UtcNow}] Added Command: {_command.name}");
            }
            catch (Exception ex) { Console.WriteLine(ex); }
        }
        ///Represents a full command with confirmation support
        public class Command
        {
            public static List<Command> allCommands = new List<Command>();

            public string name = "";
            public string description = "";
            public string extendedDecription = "";
            public string parameters = "None";
            public bool modOnly = false;
            public Func<SocketSlashCommand, Task> callback = (SocketSlashCommand _command) => throw new NotImplementedException();
            public List<SlashCommandOptionBuilder> options = new List<SlashCommandOptionBuilder>();
            public SlashCommandBuilder GetCommandBuilder() =>
                new SlashCommandBuilder()
                {
                    Name = name,
                    Description = description,
                    DefaultMemberPermissions = modOnly ? GuildPermission.KickMembers : GuildPermission.ViewChannel,
                    Options = options,
                };
        }
        public class Button
        {
            private static ulong buttonsCreated = 0;
            public static Dictionary<string, Button> Buttons = new Dictionary<string, Button>();

            private string ID;
            private Func<SocketSlashCommand, Task> Callback;
            private SocketSlashCommand Command;
            private readonly ButtonBuilder ButtonBuilder;

            public Button(SocketSlashCommand command, string buttonText, ButtonStyle style, Func<SocketSlashCommand, Task> callback)
            {
                ID = (buttonsCreated++).ToString();
                Callback = callback;
                Command = command;
                Buttons.Add(ID, this);
                ButtonBuilder = new ButtonBuilder(buttonText, ID, style);
            }
            public ButtonBuilder GetButton() => ButtonBuilder;
            public Task Press() => Callback(Command);
            public void DeleteButton() => Buttons.Remove(ID);
            public static void UpdateButtons() => buttonsCreated = Buttons.Count() == 0 ? 0 : buttonsCreated;
        }
        public class Interaction
        {
            public readonly SocketSlashCommand Command;
            private DateTime LastUsed;

            public Interaction(SocketSlashCommand command) 
            { 
                Command = command; 
                LastUsed = DateTime.UtcNow; 
            }

            public async Task Respond(string? text = null, Embed[]? embeds = null, bool isTTS = false, bool ephemeral = true, AllowedMentions? allowedMentions = null, MessageComponent? components = null, Embed? embed = null, RequestOptions? options = null)
            {
                if (Command.HasResponded)
                    await Command.ModifyOriginalResponseAsync(m => { m.Content = text; m.Embeds = embeds; m.AllowedMentions = allowedMentions; m.Components = components; m.Embed = embed; }, options);
                else
                    await Command.RespondAsync(text, embeds, isTTS, ephemeral, allowedMentions, components, embed, options);
                LastUsed = DateTime.UtcNow;
            }

            public async Task DeleteInteraction()
            {
                await Command.DeleteOriginalResponseAsync();
                Interactions.Remove(Command);
            }

            public static void UpdateInteractions() => Interactions.Values.Where(i => DateTime.UtcNow.Subtract(i.LastUsed).Minutes > 5).ToList().ForEach(async i => await i.DeleteInteraction());
        }

        public static async Task RecursiveMuliPageEmbed(SocketSlashCommand _command, EmbedBuilder[] embeds, int index = 0)
        {
            if (embeds.Length == 0)
            {
                await Interactions[_command].Respond(text: "Nothing to display");
                return;
            }

            ///Make buttons
            ButtonBuilder prev = new Button(_command, "Previous", ButtonStyle.Primary, async Task (SocketSlashCommand _command) => await RecursiveMuliPageEmbed(_command, embeds, index - 1 < 0 ? embeds.Length - 1 : --index)).GetButton();
            ButtonBuilder next = new Button(_command, "Next", ButtonStyle.Primary, async Task (SocketSlashCommand _command) => await RecursiveMuliPageEmbed(_command, embeds, index + 1 >= embeds.Length ? 0 : ++index)).GetButton();

            ///Display
            await Interactions[_command].Respond(embed: embeds[index].WithFooter($"{index + 1}/{embeds.Length}").Build(), components: new ComponentBuilder().WithButton(prev).WithButton(next).Build());
        }

    }
    ///A class to be overridden to create modules
    internal abstract class Module
    {
        ///The Program this Module is attached to
        protected Program Bot;

        ///Constructor
        public Module(Program bot) { Bot = bot; InitilizeModule(); }

        ///Adds all events to the client
        public abstract Task InitilizeModule();
    }
}