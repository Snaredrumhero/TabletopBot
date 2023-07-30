global using Discord;
global using Discord.Rest;
global using Discord.WebSocket;
global using System.Text.Json;
global using static TableTopBot.Program;
global using static TableTopBot.Interaction;
using System.Reflection;

namespace TableTopBot
{
    /// <summary> The main program of a discord bot </summary>
    internal class Program
    {
        /// <summary>  Tells the program how to format all saved and loaded json </summary>
        public static readonly JsonSerializerOptions JsonOptions = new JsonSerializerOptions { WriteIndented = true, IncludeFields = true };
        
        /// <summary> Stores all data that should not be publicly accessable and needs to be loaded from a json file </summary>
        private static class PrivateVariables
        {
            /// <summary> The bot's key </summary>
            public static string Key { get; set; } = "";

            /// <summary> The server's ID </summary>
            public static ulong Server { get; set; } = 0;

            /// <summary> The server's log channel's ID </summary>
            public static ulong LogChannel { get; set; } = 0;

            /// <summary> The server </summary>
            public static SocketGuild SocketServer => Client.GetGuild(Server);

            /// <summary> The server's log channel </summary>
            public static SocketTextChannel SocketLogChannel => SocketServer.GetTextChannel(LogChannel);

            /// <summary> Loads the class on start </summary>
            /// <exception cref="NullReferenceException">Throws if the correct file can't be found</exception>
            [Start] public static void LoadVariables()
            {
                PV pv = JsonSerializer.Deserialize<PV>(File.ReadAllText("./ProgramPrivateVariables.json"), JsonOptions) ?? throw new NullReferenceException("No Private Variable File Found!");
                Key = pv.Key;
                Server = pv.Server;
                LogChannel = pv.LogChannel;
            }

            /// <summary> The structure of the class to be loaded </summary>
            private class PV
            {
                public string Key { get; set; } = "";
                public ulong Server { get; set; } = 0;
                public ulong LogChannel { get; set; } = 0;
            }
        }

        /// <summary> The server </summary>
        public static SocketGuild Server => PrivateVariables.SocketServer;

        /// <summary> Stores all information about the bot that could change based on each bot </summary>
        private static class BotInfo
        {
            /// <summary> The bot's name </summary>
            public static string Name { get; set; } = "";

            /// <summary> What the bot should display while loading </summary>
            public static string LoadingString { get; set; } = "";

            /// <summary> What the bot should display while ready </summary>
            public static string ReadyString { get; set; } = "";

            /// <summary> Loads the class on start </summary>
            /// <exception cref="NullReferenceException">Throws if the correct file can't be found</exception>
            [Start] public static void LoadVariables()
            {
                BI bi = JsonSerializer.Deserialize<BI>(File.ReadAllText("./BotInfo.json"), JsonOptions) ?? throw new NullReferenceException("No Bot Info File Found!");
                Name = bi.Name;
                LoadingString = bi.LoadingString;
                ReadyString = bi.ReadyString;
            }

            /// <summary> The format for which the class should be loaded </summary>
            private class BI
            {
                public string Name { get; set; } = "";
                public string LoadingString { get; set; } = "";
                public string ReadyString { get; set; } = "";
            }
        }

        /// <summary> The bot </summary>
        private static DiscordSocketClient Client = new DiscordSocketClient(new DiscordSocketConfig { GatewayIntents = GatewayIntents.All });

        /// <summary> Runs on start of .exe </summary>
        public static Task Main(string[] args) => VerrifyAttributes();

        /// <summary> Verrifies all atributes </summary>
        /// <exception cref="Exception">Throws if attributes are used incorrectly </exception>
        private static Task VerrifyAttributes()
        {
            ///Verrify modules
            Modules = Assembly.GetExecutingAssembly().GetTypes().Where(m => m.GetCustomAttributes(typeof(ModuleAttribute), false).Length > 0).ToList().Select(m => Activator.CreateInstance(m)!).ToArray();

            ///verrify commands
            MethodInfo[] commandAttribute = Assembly.GetExecutingAssembly().GetTypes().SelectMany(t => t.GetMethods()).Where(m => m.GetCustomAttributes(typeof(CommandAttribute), false).Length > 0).ToArray();
            foreach (MethodInfo method in commandAttribute)
            {
                if (method.ReturnParameter.ParameterType != typeof(Task))
                    throw new Exception(message: "Incorrect return type of a command, Task expected");
                if (method.GetParameters().Length != 1 || method.GetParameters()[0].ParameterType != typeof(SocketSlashCommand))
                    throw new Exception(message: "Incorrect parameters of a command, exactly one SocketSlashCommand expected");
                if (!method.IsStatic)
                    throw new Exception(message: "Incorrect qualifiers of a command, expected a static command");
            }
            CommandMethods = commandAttribute;

            ///verrify update
            MethodInfo[] updateAttribute = Assembly.GetExecutingAssembly().GetTypes().SelectMany(t => t.GetMethods()).Where(m => m.GetCustomAttributes(typeof(UpdateAttribute), false).Length > 0).ToArray();
            foreach (MethodInfo method in updateAttribute)
            {
                if (method.ReturnParameter.ParameterType != typeof(void))
                    throw new Exception(message: "Incorrect return type of a update function, Void expected");
                if (method.GetParameters().Length != 0)
                    throw new Exception(message: "Incorrect parameters of a update function, no parameters expected");
                if (!method.IsStatic)
                    throw new Exception(message: "Incorrect qualifiers of a update function, expected a static function");
            }
            UpdateMethods = updateAttribute;

            ///verrify start
            MethodInfo[] startAttribute = Assembly.GetExecutingAssembly().GetTypes().SelectMany(t => t.GetMethods()).Where(m => m.GetCustomAttributes(typeof(StartAttribute), false).Length > 0).ToArray();
            foreach (MethodInfo method in startAttribute)
            {
                if (method.ReturnParameter.ParameterType != typeof(void))
                    throw new Exception(message: "Incorrect return type of a start function, Void expected");
                if (method.GetParameters().Length != 0)
                    throw new Exception(message: "Incorrect parameters of a start function, no parameters expected");
                if (!method.IsStatic)
                    throw new Exception(message: "Incorrect qualifiers of a start function, expected a static function");
            }

            ///Run start
            foreach (MethodInfo method in startAttribute)
                method.Invoke(null, null);

            return new Program().MainAsync();
        }

        /// <summary> All methods that have the command attribute </summary>
        private static MethodInfo[] CommandMethods = new MethodInfo[0];

        /// <summary> All methods that have the update attribute </summary>
        private static MethodInfo[] UpdateMethods = new MethodInfo[0];

        /// <summary> All classes that have the module attribute </summary>
        private static object[] Modules = new object[0];

        /// <summary> The function that handles the main functionality of the bot </summary>
        private async Task MainAsync()
        {
            Console.Title = BotInfo.Name;
            bool acceptingCommands = true;
            bool end = false;

            ///Console logging
            Client.Log += (LogMessage msg) =>
            {
                SimpleLog(msg.ToString());
                return Task.CompletedTask;
            };
            ///Command logging & callbacks
            Client.SlashCommandExecuted += async (SocketSlashCommand _command) =>
            {
                ///log
                if (!Callbacks.ContainsKey(_command.CommandId))
                    return;
                string log = $"[{DateTime.UtcNow}]\nUser: {_command.User.Username}\nCommand: {_command.CommandName}\nParams: ";
                _command.Data.Options.ToList().ForEach(o => log += $"\n{o.Name}: {o.Value}");
                await PrivateVariables.SocketLogChannel.SendMessageAsync(embed: new EmbedBuilder().AddField("Command Executed", log).Build());
                ///do
                if (!acceptingCommands)
                {
                    await Respond(_command, text: "The bot is currently not accepting commands, please wait while it finishes loading.");
                    return;
                }
                try { await Callbacks[_command.CommandId](_command); }
                catch (Exception ex) { await Log($"Error: {ex.Message}", _command); }
            };
            ///Component Callbacks
            Client.ButtonExecuted += async (SocketMessageComponent _button) => await ComponentCallback(_button);
            Client.SelectMenuExecuted += async (SocketMessageComponent _selectMenu) => await ComponentCallback(_selectMenu);
            ///Fully connected
            Client.Connected += async () => await Client.SetGameAsync(BotInfo.ReadyString);

            ///Threads
            Thread update = new Thread(() => {
                while (!end)
                {
                    foreach (MethodInfo method in UpdateMethods)
                        method.Invoke(null, null);
                    Thread.Sleep(1000);
                }
            });
            Thread consoleCommands = new Thread(async () => {
                while (!end)
                {
                    switch ((Console.ReadLine() ?? "").ToLower())
                    {
                        case "help": ///displays help
                            SimpleLog("Help:\nstop: ends the program\ninit: Loads all commands\nrm: removes all commads\nclear: clears console output");
                            break;
                        case "quit" or "stop" or "halt": ///ends the program
                            end = true;
                            break;
                        case "load" or "init" or "initialize" or "start": ///loads all commands
                            acceptingCommands = false;
                            await Client.SetGameAsync(BotInfo.LoadingString);
                            foreach (MethodInfo method in CommandMethods)
                            {
                                try
                                {
                                    CommandAttribute ca = method.GetCustomAttribute<CommandAttribute>() ?? throw new Exception("Error: Could not find command attribute");
                                    string name = FormatName(method.Name);
                                    Callbacks.Add((await PrivateVariables.SocketServer.CreateApplicationCommandAsync(new SlashCommandBuilder().WithName(name).WithDescription(ca.description).WithDefaultMemberPermissions(ca.modOnly ? GuildPermission.KickMembers : GuildPermission.ViewChannel).AddOptions(method.GetCustomAttributes<OptionAttribute>().Select(o => o.option).ToArray()).Build())).Id, async (SocketSlashCommand _command) => await (Task)method.Invoke(null, new object[1] { _command })!);
                                    SimpleLog($"Added Command: {name}");
                                }
                                catch (Exception ex) { SimpleLog($"Error: {ex.Message}"); }
                            }
                            SimpleLog("Added all server commands");
                            await Client.SetGameAsync(BotInfo.ReadyString);
                            acceptingCommands = true;
                            break;
                        case "remove" or "rm": ///removes all commands
                            await PrivateVariables.SocketServer.DeleteApplicationCommandsAsync();
                            await DeleteAllInteractions();
                            Callbacks.Clear();
                            SimpleLog("Cleared server commands");
                            break;
                        case "clear" or "cls": ///clears the console
                            Console.Clear();
                            break;
                        default:
                            SimpleLog("Invalid Console Command");
                            break;
                    }
                }
            });

            ///Run bot
            await Client.LoginAsync(TokenType.Bot, PrivateVariables.Key);
            await Client.StartAsync();
            update.Start();
            consoleCommands.Start();
            while (!end) await Task.Delay(1000);
            await DeleteAllInteractions();
            await Client.LogoutAsync();
        }

        /// <summary> Holds what to do when specific commands are given </summary>
        private static Dictionary<ulong, Func<SocketSlashCommand, Task>> Callbacks = new Dictionary<ulong, Func<SocketSlashCommand, Task>>();

        #region Callback Adders
        public static void AddApplicationCommandCreatedCallback(Func<SocketApplicationCommand, Task> f) => Client.ApplicationCommandCreated += f;
        public static void AddApplicationCommandDeletedCallback(Func<SocketApplicationCommand, Task> f) => Client.ApplicationCommandDeleted += f;
        public static void AddApplicationCommandUpdatedCallback(Func<SocketApplicationCommand, Task> f) => Client.ApplicationCommandUpdated += f;
        public static void AddAutocompleteExecutedCallback(Func<SocketAutocompleteInteraction, Task> f) => Client.AutocompleteExecuted += f;
        public static void AddButtonExecutedCallback(Func<SocketMessageComponent, Task> f) => Client.ButtonExecuted += f;
        public static void AddChannelCreatedCallback(Func<SocketChannel, Task> f) => Client.ChannelCreated += f;
        public static void AddChannelDestroyedCallback(Func<SocketChannel, Task> f) => Client.ChannelDestroyed += f;
        public static void AddChannelUpdatedCallback(Func<SocketChannel, SocketChannel, Task> f) => Client.ChannelUpdated += f;
        public static void AddConnectedCallback(Func<Task> f) => Client.Connected += f;
        public static void AddCurrentUserUpdatedCallback(Func<SocketSelfUser, SocketSelfUser, Task> f) => Client.CurrentUserUpdated += f;
        public static void AddDisconnectedCallback(Func<Exception, Task> f) => Client.Disconnected += f;
        public static void AddGuildAvailableCallback(Func<SocketGuild, Task> f) => Client.GuildAvailable += f;
        public static void AddGuildJoinRequestDeletedCallback(Func<Cacheable<SocketGuildUser, ulong>, SocketGuild, Task> f) => Client.GuildJoinRequestDeleted += f;
        public static void AddGuildMembersDownloadedCallback(Func<SocketGuild, Task> f) => Client.GuildMembersDownloaded += f;
        public static void AddGuildMemberUpdatedCallback(Func<Cacheable<SocketGuildUser, ulong>, SocketGuildUser, Task> f) => Client.GuildMemberUpdated += f;
        public static void AddGuildScheduledEventCancelledCallback(Func<SocketGuildEvent, Task> f) => Client.GuildScheduledEventCancelled += f;
        public static void AddGuildScheduledEventCompletedCallback(Func<SocketGuildEvent, Task> f) => Client.GuildScheduledEventCompleted += f;
        public static void AddGuildScheduledEventCreatedCallback(Func<SocketGuildEvent, Task> f) => Client.GuildScheduledEventCreated += f;
        public static void AddGuildScheduledEventUpdatedCallback(Func<Cacheable<SocketGuildEvent, ulong>, SocketGuildEvent, Task> f) => Client.GuildScheduledEventUpdated += f;
        public static void AddGuildScheduledEventUserAddCallback(Func<Cacheable<SocketUser, RestUser, IUser, ulong>, SocketGuildEvent, Task> f) => Client.GuildScheduledEventUserAdd += f;
        public static void AddGuildScheduledEventUserRemoveCallback(Func<Cacheable<SocketUser, RestUser, IUser, ulong>, SocketGuildEvent, Task> f) => Client.GuildScheduledEventUserRemove += f;
        public static void AddGuildStickerCreatedCallback(Func<SocketCustomSticker, Task> f) => Client.GuildStickerCreated += f;
        public static void AddGuildStickerDeletedCallback(Func<SocketCustomSticker, Task> f) => Client.GuildStickerDeleted += f;
        public static void AddGuildStickerUpdatedCallback(Func<SocketCustomSticker, SocketCustomSticker, Task> f) => Client.GuildStickerUpdated += f;
        public static void AddGuildUnavailableCallback(Func<SocketGuild, Task> f) => Client.GuildUnavailable += f;
        public static void AddGuildUpdatedCallback(Func<SocketGuild, SocketGuild, Task> f) => Client.GuildUpdated += f;
        public static void AddIntegrationCreatedCallback(Func<IIntegration, Task> f) => Client.IntegrationCreated += f;
        public static void AddIntegrationDeletedCallback(Func<IGuild, ulong, Optional<ulong>, Task> f) => Client.IntegrationDeleted += f;
        public static void AddIntegrationUpdatedCallback(Func<IIntegration, Task> f) => Client.IntegrationUpdated += f;
        public static void AddInteractionCreatedCallback(Func<SocketInteraction, Task> f) => Client.InteractionCreated += f;
        public static void AddInviteCreatedCallback(Func<SocketInvite, Task> f) => Client.InviteCreated += f;
        public static void AddInviteDeletedCallback(Func<SocketGuildChannel, string, Task> f) => Client.InviteDeleted += f;
        public static void AddJoinedGuildCallback(Func<SocketGuild, Task> f) => Client.JoinedGuild += f;
        public static void AddLatencyUpdatedCallback(Func<int, int, Task> f) => Client.LatencyUpdated += f;
        public static void AddLeftGuildCallback(Func<SocketGuild, Task> f) => Client.LeftGuild += f;
        public static void AddLogCallback(Func<LogMessage, Task> f) => Client.Log += f;
        public static void AddLoggedInCallback(Func<Task> f) => Client.LoggedIn += f;
        public static void AddLoggedOutCallback(Func<Task> f) => Client.LoggedOut += f;
        public static void AddMessageCommandExecutedCallback(Func<SocketMessageCommand, Task> f) => Client.MessageCommandExecuted += f;
        public static void AddMessageDeletedCallback(Func<Cacheable<IMessage, ulong>, Cacheable<IMessageChannel, ulong>, Task> f) => Client.MessageDeleted += f;
        public static void AddMessageReceivedCallback(Func<SocketMessage, Task> f) => Client.MessageReceived += f;
        public static void AddMessagesBulkDeletedCallback(Func<IReadOnlyCollection<Cacheable<IMessage, ulong>>, Cacheable<IMessageChannel, ulong>, Task> f) => Client.MessagesBulkDeleted += f;
        public static void AddMessageUpdatedCallback(Func<Cacheable<IMessage, ulong>, SocketMessage, ISocketMessageChannel, Task> f) => Client.MessageUpdated += f;
        public static void AddModalSubmittedCallback(Func<SocketModal, Task> f) => Client.ModalSubmitted += f;
        public static void AddPresenceUpdatedCallback(Func<SocketUser, SocketPresence, SocketPresence, Task> f) => Client.PresenceUpdated += f;
        public static void AddReactionAddedCallback(Func<Cacheable<IUserMessage, ulong>, Cacheable<IMessageChannel, ulong>, SocketReaction, Task> f) => Client.ReactionAdded += f;
        public static void AddReactionRemovedCallback(Func<Cacheable<IUserMessage, ulong>, Cacheable<IMessageChannel, ulong>, SocketReaction, Task> f) => Client.ReactionRemoved += f;
        public static void AddReactionsClearedCallback(Func<Cacheable<IUserMessage, ulong>, Cacheable<IMessageChannel, ulong>, Task> f) => Client.ReactionsCleared += f;
        public static void AddReactionsRemovedForEmoteCallback(Func<Cacheable<IUserMessage, ulong>, Cacheable<IMessageChannel, ulong>, IEmote, Task> f) => Client.ReactionsRemovedForEmote += f;
        public static void AddReadyCallback(Func<Task> f) => Client.Ready += f;
        public static void AddRecipientAddedCallback(Func<SocketGroupUser, Task> f) => Client.RecipientAdded += f;
        public static void AddRecipientRemovedCallback(Func<SocketGroupUser, Task> f) => Client.RecipientRemoved += f;
        public static void AddRequestToSpeakCallback(Func<SocketStageChannel, SocketGuildUser, Task> f) => Client.RequestToSpeak += f;
        public static void AddRoleCreatedCallback(Func<SocketRole, Task> f) => Client.RoleCreated += f;
        public static void AddRoleDeletedCallback(Func<SocketRole, Task> f) => Client.RoleDeleted += f;
        public static void AddRoleUpdatedCallback(Func<SocketRole, SocketRole, Task> f) => Client.RoleUpdated += f;
        public static void AddSelectMenuExecutedCallback(Func<SocketMessageComponent, Task> f) => Client.SelectMenuExecuted += f;
        public static void AddSlashCommandExecutedCallback(Func<SocketSlashCommand, Task> f) => Client.SlashCommandExecuted += f;
        public static void AddSpeakerAddedCallback(Func<SocketStageChannel, SocketGuildUser, Task> f) => Client.SpeakerAdded += f;
        public static void AddSpeakerRemovedCallback(Func<SocketStageChannel, SocketGuildUser, Task> f) => Client.SpeakerRemoved += f;
        public static void AddStageEndedCallback(Func<SocketStageChannel, Task> f) => Client.StageEnded += f;
        public static void AddStageStartedCallback(Func<SocketStageChannel, Task> f) => Client.StageStarted += f;
        public static void AddStageUpdatedCallback(Func<SocketStageChannel, SocketStageChannel, Task> f) => Client.StageUpdated += f;
        public static void AddThreadCreatedCallback(Func<SocketThreadChannel, Task> f) => Client.ThreadCreated += f;
        public static void AddThreadDeletedCallback(Func<Cacheable<SocketThreadChannel, ulong>, Task> f) => Client.ThreadDeleted += f;
        public static void AddThreadMemberJoinedCallback(Func<SocketThreadUser, Task> f) => Client.ThreadMemberJoined += f;
        public static void AddThreadMemberLeftCallback(Func<SocketThreadUser, Task> f) => Client.ThreadMemberLeft += f;
        public static void AddThreadUpdatedCallback(Func<Cacheable<SocketThreadChannel, ulong>, SocketThreadChannel, Task> f) => Client.ThreadUpdated += f;
        public static void AddUserBannedCallback(Func<SocketUser, SocketGuild, Task> f) => Client.UserBanned += f;
        public static void AddUserCommandExecutedCallback(Func<SocketUserCommand, Task> f) => Client.UserCommandExecuted += f;
        public static void AddUserIsTypingCallback(Func<Cacheable<IUser, ulong>, Cacheable<IMessageChannel, ulong>, Task> f) => Client.UserIsTyping += f;
        public static void AddUserJoinedCallback(Func<SocketGuildUser, Task> f) => Client.UserJoined += f;
        public static void AddUserLeftCallback(Func<SocketGuild, SocketUser, Task> f) => Client.UserLeft += f;
        public static void AddUserUnbannedCallback(Func<SocketUser, SocketGuild, Task> f) => Client.UserUnbanned += f;
        public static void AddUserUpdatedCallback(Func<SocketUser, SocketUser, Task> f) => Client.UserUpdated += f;
        public static void AddUserVoiceStateUpdatedCallback(Func<SocketUser, SocketVoiceState, SocketVoiceState, Task> f) => Client.UserVoiceStateUpdated += f;
        public static void AddVoiceServerUpdatedCallback(Func<SocketVoiceServer, Task> f) => Client.VoiceServerUpdated += f;
        public static void AddWebhooksUpdatedCallback(Func<SocketGuild, SocketChannel, Task> f) => Client.WebhooksUpdated += f;
        #endregion

        /// <summary> Logs a message to the console, log channel, and to an optional command</summary>
        /// <param name="msg">The message to send</param>
        /// <param name="cmd">The command to respond to</param>
        public static async Task Log(string msg, SocketSlashCommand? cmd = null)
        {
            msg = $"[{DateTime.UtcNow}] " + msg;
            Console.WriteLine(msg);
            await PrivateVariables.SocketLogChannel.SendMessageAsync(text: msg);
            if (cmd != null)
                await Respond(cmd, text: msg, ephemeral: true);
        }

        /// <summary> Logs a message to the console </summary>
        /// <param name="msg">The message to send</param>
        public static void SimpleLog(string msg) => Console.WriteLine($"[{DateTime.UtcNow}] " + msg);

        /// <summary> Allow the user to see the list of commands and their descriptions </summary>
        [Command(description: "Displays all information about all of this bot's commands.")]
        public static async Task Help(SocketSlashCommand _command) =>
            await Respond(_command, embed: new EmbedBuilder().WithTitle("List of Commands").WithDescription("Select a command below to view their descriptions:").WithColor(Color.Orange).Build(), selectMenus: new SelectMenu[]{ new SelectMenu(async (SocketSlashCommand _command, SocketMessageComponent _component) =>
                {
                    MethodInfo method = CommandMethods.First(c => FormatName(c.Name) == string.Join(", ", _component.Data.Values));
                    string name = FormatName(method.Name);
                    CommandAttribute ca = method.GetCustomAttribute<CommandAttribute>()!;
                    var options = method.GetCustomAttributes<OptionAttribute>();
                    await Respond(_command, changeComponents: false, embed: new EmbedBuilder().WithTitle($"**{name}**").WithDescription((ca.description + (options.Count() > 0 ? "\n\n__Parameters__" : "") + string.Join(null, options.Select(o => $"\n-{o.option.Name} - {o.option.Description}")))).WithColor(ca.modOnly ? Color.Red : Color.Blue).Build());
                }, options: CommandMethods.Where(m => !m.GetCustomAttribute<CommandAttribute>()!.modOnly || Server.GetUser(_command.User.Id).GuildPermissions.KickMembers).Select(m => new SelectMenuOptionBuilder().WithLabel(FormatName(m.Name)).WithDescription(m.GetCustomAttribute<CommandAttribute>()!.description).WithValue(FormatName(m.Name))).ToList(), placeholder: "Select Command") }, ephemeral: true);
    
        /// <summary> Formats the name of a command </summary>
        /// <param name="str"> the string to format </param>
        private static string FormatName(string str) => string.Join(null, str.Select(c => char.IsLower(c) ? c.ToString() : $"-{char.ToLower(c)}")).Substring(1);
    }

    #region Attributes
    /// <summary> Runs a method on start </summary>
    [AttributeUsage(AttributeTargets.Method)] internal class StartAttribute : Attribute { }

    /// <summary> Runs a method around every second </summary>
    [AttributeUsage(AttributeTargets.Method)] internal class UpdateAttribute : Attribute { }

    /// <summary> Instantiates a class on start </summary>
    [AttributeUsage(AttributeTargets.Class)] internal class ModuleAttribute : Attribute { }

    /// <summary> Marks a class as a command </summary>
    [AttributeUsage(AttributeTargets.Method)]
    internal class CommandAttribute : Attribute
    {
        public readonly string description;
        public readonly bool modOnly;

        public CommandAttribute(string description, bool modOnly = false)
        {
            this.description = description;
            this.modOnly = modOnly;
        }
    }

    /// <summary> Adds an option to a command </summary>
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
    internal class OptionAttribute : Attribute
    {
        public readonly SlashCommandOptionBuilder option;

        public OptionAttribute(string name, ApplicationCommandOptionType type, string description, ChannelType[] channelTypes, string[] choiceKeys, string[] choiceValues, bool isRequired = true, bool isDefault = false, bool isAutocomplete = false, double minValue = 0, double maxValue = 6000, /*List<SlashCommandOptionBuilder>? options = null, IDictionary<string, string>? nameLocalizations = null, IDictionary<string, string>? descriptionLocalizations = null,*/ int minLength = 0, int maxLength = 6000)
        {
            if (choiceKeys.Length != choiceValues.Length)
                throw new Exception("Error: Option choice lengths don't match");
            option = new SlashCommandOptionBuilder()
            {
                Name = name,
                Type = type,
                Description = description,
                IsRequired = isRequired,
                IsDefault = isDefault,
                IsAutocomplete = isAutocomplete,
                MinValue = minValue,
                MaxValue = maxValue,
                Options = null,//options,
                ChannelTypes = channelTypes.ToList(),
                MinLength = minLength,
                MaxLength = maxLength,
            };//.WithNameLocalizations(nameLocalizations).WithDescriptionLocalizations(descriptionLocalizations);
            for (int i = 0; i < choiceKeys.Length; i++)
                option.AddChoice(choiceKeys[i], choiceValues[i]);
        }

        public OptionAttribute(string name, ApplicationCommandOptionType type, string description, bool isRequired = true, bool isDefault = false, bool isAutocomplete = false, double minValue = 0, double maxValue = 6000, /*List<SlashCommandOptionBuilder>? options = null, IDictionary<string, string>? nameLocalizations = null, IDictionary<string, string>? descriptionLocalizations = null,*/ int minLength = 0, int maxLength = 6000) : this(name, type, description, new ChannelType[0], new string[0], new string[0], isRequired, isDefault, isAutocomplete, minValue, maxValue, minLength, maxLength) { }
    }
    #endregion

    /// <summary> Handles interactions between a user and the bot </summary>
    internal class Interaction
    {
        /// <summary> The list of all interactions </summary>
        private static Dictionary<SocketSlashCommand, Interaction> Interactions = new Dictionary<SocketSlashCommand, Interaction>();

        /// <summary> The command that this interaction is in response to </summary>
        private readonly SocketSlashCommand Command;

        private Interaction(SocketSlashCommand command)
        {
            Command = command;
            LastUsed = DateTime.UtcNow;
        }

        /// <summary> Responds to the command </summary>
        /// <param name="_command"></param>
        /// <param name="changeText"></param>
        /// <param name="text"></param>
        /// <param name="changeEmbeds"></param>
        /// <param name="embeds"></param>
        /// <param name="embed"></param>
        /// <param name="isTTS"></param>
        /// <param name="ephemeral"></param>
        /// <param name="changeAM"></param>
        /// <param name="allowedMentions"></param>
        /// <param name="changeComponents"></param>
        /// <param name="components"></param>
        /// <param name="buttons"></param>
        /// <param name="selectMenus"></param>
        /// <param name="options"></param>
        public static async Task Respond(SocketSlashCommand _command, bool changeText = true, string? text = null,
                                  bool changeEmbeds = true, Embed[]? embeds = null, Embed? embed = null, ///changes both
                                  bool isTTS = false, bool ephemeral = true, ///can't be changed
                                  bool changeAM = true, AllowedMentions? allowedMentions = null, 
                                  bool changeComponents = true, ComponentBuilder? components = null, Button[]? buttons = null, SelectMenu[]? selectMenus = null, /// changes all
                                  RequestOptions? options = null) =>
            await (!_command.HasResponded ? new Interaction(_command) : Interactions[_command]).Respond(changeText, text, changeEmbeds, embeds, embed, isTTS, ephemeral, changeAM, allowedMentions, changeComponents, components, buttons, selectMenus, options);

        /// <summary> Responds to the command </summary>
        /// <param name="_component"></param>
        /// <param name="changeText"></param>
        /// <param name="text"></param>
        /// <param name="changeEmbeds"></param>
        /// <param name="embeds"></param>
        /// <param name="embed"></param>
        /// <param name="isTTS"></param>
        /// <param name="ephemeral"></param>
        /// <param name="changeAM"></param>
        /// <param name="allowedMentions"></param>
        /// <param name="changeComponents"></param>
        /// <param name="components"></param>
        /// <param name="buttons"></param>
        /// <param name="selectMenus"></param>
        /// <param name="options"></param>
        /// <returns></returns>
        public static async Task Respond(SocketMessageComponent _component, bool changeText = true, string? text = null,
                                  bool changeEmbeds = true, Embed[]? embeds = null, Embed? embed = null, ///changes both
                                  bool isTTS = false, bool ephemeral = true, ///can't be changed
                                  bool changeAM = true, AllowedMentions? allowedMentions = null,
                                  bool changeComponents = true, ComponentBuilder? components = null, Button[]? buttons = null, SelectMenu[]? selectMenus = null, /// changes all
                                  RequestOptions? options = null) => 
            await Interactions[Components[ulong.Parse(_component.Data.CustomId)]].Respond(changeText, text, changeEmbeds, embeds, embed, isTTS, ephemeral, changeAM, allowedMentions, changeComponents, components, buttons, selectMenus, options);
        
        private async Task Respond(bool changeText, string? text, bool changeEmbeds, Embed[]? embeds, Embed? embed, bool isTTS, bool ephemeral, bool changeAM, AllowedMentions? allowedMentions, bool changeComponents, ComponentBuilder? components, Button[]? buttons, SelectMenu[]? selectMenus, RequestOptions? options)
        {
            ///Get all components
            ComponentBuilder fullComponent = components ?? new ComponentBuilder();
            buttons?.ToList().ForEach(b => fullComponent = fullComponent.WithButton(b.GetBuilder(this)));
            selectMenus?.ToList().ForEach(sm => fullComponent = fullComponent.WithSelectMenu(sm.GetBuilder(this)));
            ///Respond
            if (!Command.HasResponded)
            {
                await Command.RespondAsync(text, embeds, isTTS, ephemeral, allowedMentions, fullComponent.Build(), embed, options);
                Interactions.Add(Command, this);
            }
            else
                await Command.ModifyOriginalResponseAsync(m => {
                    if (changeText) m.Content = text;
                    if (changeEmbeds)
                    {
                        m.Embeds = embeds;
                        m.Embed = embed;
                    }
                    if (changeAM) m.AllowedMentions = allowedMentions;
                    if (changeComponents) m.Components = fullComponent.Build();
                }, options);
            LastUsed = DateTime.UtcNow;
        }

        #region Components
        /// <summary> Gets the next available id </summary>
        /// <returns> The ID as a ulong </returns>
        /// <exception cref="IndexOutOfRangeException">Throws if there are more buttons than ulong.max</exception>
        private static ulong GetNextID()
        {
            ulong i = 0;
            while (i < ulong.MaxValue && Components.ContainsKey(i)) i++;
            if (i == ulong.MaxValue)
                throw new IndexOutOfRangeException(message: "Too many components were created!");
            return i;
        }

        /// <summary> A button as part of an interaction </summary>
        public class Button
        {
            /// <summary> The builder for the button </summary>
            private readonly ButtonBuilder Builder;

            /// <summary> The callback when the button is pressed </summary>
            private readonly Func<SocketSlashCommand, SocketMessageComponent, Task> Callback;

            public Button(Func<SocketSlashCommand, SocketMessageComponent, Task> callback, string? label = null, ButtonStyle style = ButtonStyle.Primary, string? url = null, IEmote? emote = null, bool isDisabled = false)
            {
                Builder = new ButtonBuilder(label, null, style, url, emote, isDisabled);
                Callback = callback;
            }

            /// <summary> Gets the button builder and adds the button to the interaction </summary>
            /// <param name="i">The interaction that the button is a part of</param>
            /// <returns> The button builder </returns>
            public ButtonBuilder GetBuilder(Interaction i)
            {
                ulong id = GetNextID();
                Components.Add(id, i.Command);
                i.ComponentCallbacks.Add(id, Callback);
                return Builder.WithCustomId(id.ToString());
            }
        }

        /// <summary> A select menu as part of an interaction </summary>
        public class SelectMenu
        {
            /// <summary> The builder for the select menu </summary>
            private readonly SelectMenuBuilder Builder;

            /// <summary> The callback when the select menu is pressed </summary>
            private readonly Func<SocketSlashCommand, SocketMessageComponent, Task> Callback;

            public SelectMenu(Func<SocketSlashCommand, SocketMessageComponent, Task> callback, List<SelectMenuOptionBuilder>? options = null, string? placeholder = null, int maxValues = 1, int minValues = 1, bool isDisabled = false, ComponentType type = ComponentType.SelectMenu, List<ChannelType>? channelTypes = null)
            {
                Builder = new SelectMenuBuilder(null, options, placeholder, maxValues, minValues, isDisabled, type, channelTypes);
                Callback = callback;
            }

            /// <summary> Gets the select menu builder and adds the select menu to the interaction </summary>
            /// <param name="i">The interaction that the select menu is a part of</param>
            /// <returns> The select menu builder </returns>
            public SelectMenuBuilder GetBuilder(Interaction i)
            {
                ulong id = GetNextID();
                Components.Add(id, i.Command);
                i.ComponentCallbacks.Add(id, Callback);
                return Builder.WithCustomId(id.ToString());
            }
        }

        /// <summary> The list of all components </summary>
        private static Dictionary<ulong, SocketSlashCommand> Components = new Dictionary<ulong, SocketSlashCommand>();

        /// <summary> The list of all components that are part of the interaction and what to do with them </summary>
        private Dictionary<ulong, Func<SocketSlashCommand, SocketMessageComponent, Task>> ComponentCallbacks = new Dictionary<ulong, Func<SocketSlashCommand, SocketMessageComponent, Task>>();
        
        /// <summary> Invokes the callback of a specific component </summary>
        /// <param name="component">The component</param>
        public static async Task ComponentCallback(SocketMessageComponent component)
        {
            Interaction inter = Interactions[Components[ulong.Parse(component.Data.CustomId)]];
            try
            {
                await component.DeferAsync();
                await inter.ComponentCallbacks[ulong.Parse(component.Data.CustomId)](inter.Command, component);
            }
            catch (Exception ex) { await Log($"Error: {ex.Message}", inter.Command); }
        }
        #endregion

        #region Interaction Cleanup
        /// <summary> Stores the time that the component was last used </summary>
        private DateTime LastUsed;

        /// <summary> Delets the interaction </summary>
        private async Task DeleteInteraction()
        {
            Interactions.Remove(Command);
            await Command.DeleteOriginalResponseAsync();
            Components.Where(c => c.Value == Command).ToList().ForEach(c => Components.Remove(c.Key));
        }

        /// <summary> Deletes all interactions </summary>
        public static async Task DeleteAllInteractions()
        {
            foreach (Interaction i in Interactions.Values) 
                await i.DeleteInteraction();
        }

        /// <summary> Checks if any inteactions are over 5 minutes old and deletes them </summary>
        [Update] public static void UpdateInteractions() => Interactions.Values.Where(i => DateTime.UtcNow.Subtract(i.LastUsed).Minutes > 5).ToList().ForEach(async i => await i.DeleteInteraction());
        #endregion

        #region Multi Page Embeds
        /// <summary> Creates a recursive multipage embed </summary>
        /// <param name="_command">The command to respond to</param>
        /// <param name="embeds">The list of embeds</param>
        /// <param name="title">The title of embeds</param>
        /// <param name="index">The specific embed to access</param>
        /// <param name="color">The color of the embed</param>
        public static async Task CreateRecursiveMuliPageEmbed(SocketSlashCommand _command, EmbedBuilder[] embeds, string title, int index = 0, Color? color = null)
        {
            ///Checks if the embed is valid
            if (embeds.Length == 0)
                await Respond(_command, text: "Nothing to display");
            else
                await RecursiveMuliPageEmbed(_command, embeds, title, index, color);
        }

        private static async Task RecursiveMuliPageEmbed(SocketSlashCommand _command, EmbedBuilder[] embeds, string title, int index = 0, Color? color = null)
        {
            ///Make buttons
            Button prev = new Button(async (SocketSlashCommand _command, SocketMessageComponent _component) => await RecursiveMuliPageEmbed(_command, embeds, title, index - 1 < 0 ? embeds.Length - 1 : --index, color), "Previous", ButtonStyle.Primary, isDisabled: embeds.Count() == 1);
            Button next = new Button(async (SocketSlashCommand _command, SocketMessageComponent _component) => await RecursiveMuliPageEmbed(_command, embeds, title, index + 1 >= embeds.Length ? 0 : ++index, color)   , "Next"    , ButtonStyle.Primary, isDisabled: embeds.Count() == 1);

            ///Display
            await Interaction.Respond(_command, embed: embeds[index].WithTitle(title).WithFooter($"Page {index + 1}/{embeds.Length}").WithColor(color == null ? Color.Blue : (Color)color).Build(), buttons: new Button[] { prev, next });
        }
        #endregion
    }
}