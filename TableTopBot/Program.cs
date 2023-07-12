using System.Text.Json;
using Discord;
using Discord.Interactions;
using Discord.Rest;
using Discord.WebSocket;
using System.Diagnostics;
namespace TableTopBot
{
    internal class Program
    {
        ///The bot's client
        private static PrivateVariable? PrivateVariables = JsonSerializer.Deserialize<PrivateVariable>(File.ReadAllText("./PrivateVariables.json"));
        private static DiscordSocketClient Client = new DiscordSocketClient(new DiscordSocketConfig { GatewayIntents = GatewayIntents.All });
        ///Channels
        public static SocketGuild Server() => Client.GetGuild(PrivateVariables!.Server) ?? throw new ArgumentNullException("Error: Server not specified.");
        public static SocketTextChannel LogChannel() => Server().GetTextChannel(PrivateVariables!.LogChannel) ?? throw new ArgumentNullException("Error: Log channel not specified.");
        
        public static Task Main(string[] args) => new Program().MainAsync();

        private async Task MainAsync()
        {
            XPModule xPModule = new XPModule(this);
            await xPModule.InitilizeModule();

            Console.Title = "TabletopBot";
            bool acceptingCommands = false;
            InteractionService interactionService = new InteractionService(Client.Rest);
            ///Required Lambdas
            Client.Log += (LogMessage msg) => ///Console logging
            {
                Console.WriteLine(msg.ToString());
                return Task.CompletedTask;
            };
            Client.SlashCommandExecuted += async (SocketSlashCommand _command) => ///Command logging
            {
                string log = $"User: {_command.User.Username}\nCommand: {_command.CommandName}\nParams: ";
                foreach (SocketSlashCommandDataOption o in _command.Data.Options.ToList())
                    log += $"\n{o.Name}: {o.Value}";
                await LogChannel().SendMessageAsync(log);
            };
            Client.SlashCommandExecuted += async (SocketSlashCommand _command) => ///Command calls
            {
                if (Callbacks.ContainsKey(_command.CommandId))
                    try { await Callbacks[_command.CommandId](_command); }
                    catch (Exception ex) 
                    {
                        string error = $"Error: {ex.Message}";
                        Console.WriteLine(error);
                        await _command.RespondAsync(text: error, ephemeral: true); 
                    }
            };
            Client.ButtonExecuted += async (SocketMessageComponent _button) =>    ///Confirmation
            { 
                if (Buttons.ContainsKey(_button.Data.CustomId))
                {
                    await _button.DeferAsync();
                    await Buttons[_button.Data.CustomId].Item1(Buttons[_button.Data.CustomId].Item2);
                    Buttons.Remove(_button.Data.CustomId);
                }
                else
                {
                    if(xPModule.xpSystem == null){
                        throw new Exception("");
                    }
                    XPModule.XpStorage.User user = xPModule.xpSystem.GetUser(_button.User.Id) ;
                    switch(_button.Data.CustomId)
                    {
                        case "next-button":
                           
                            await user.PageEmbed!.NextPage();
                            await _button.DeferAsync();
                            break;
                        
                        case "back-button":
                            await user.PageEmbed!.PreviousPage();
                            await _button.DeferAsync();
                            break;
                            
                        default:
                            break;
                            
                    }
                }
            };

            ///Init all moduels
            
            ///Fully connected
            Client.Connected += async () =>
            {
                await Client.SetGameAsync("Board Games");
                acceptingCommands = true;
            };

            ///run bot
            await Client.LoginAsync(TokenType.Bot, PrivateVariables!.KEY);
            await Client.StartAsync();
            await Client.SetGameAsync("Getting The Cart Out");
            while (!acceptingCommands) 
                await Task.Delay(1000);
            await AwaitConsoleCommands();
            await Server().DeleteApplicationCommandsAsync();
            await Client.LogoutAsync();
            
        }

        ///all possible callback events that can be used
        #region Callback Adders
        public void AddApplicationCommandCreatedCallback(Func<SocketApplicationCommand, Task> f) =>
            Client.ApplicationCommandCreated += f; 
        
        public void AddApplicationCommandDeletedCallback(Func<SocketApplicationCommand, Task> f) =>
            Client.ApplicationCommandDeleted += f; 
        
        public void AddApplicationCommandUpdatedCallback(Func<SocketApplicationCommand, Task> f) =>
            Client.ApplicationCommandUpdated += f; 
        
        public void AddAutocompleteExecutedCallback(Func<SocketAutocompleteInteraction, Task> f) =>
            Client.AutocompleteExecuted += f; 
        
        public void AddButtonExecutedCallback(Func<SocketMessageComponent, Task> f) =>
            Client.ButtonExecuted += f; 
        
        public void AddChannelCreatedCallback(Func<SocketChannel, Task> f) =>
            Client.ChannelCreated += f; 
        
        public void AddChannelDestroyedCallback(Func<SocketChannel, Task> f) =>
            Client.ChannelDestroyed += f; 
        
        public void AddChannelUpdatedCallback(Func<SocketChannel, SocketChannel, Task> f) =>
            Client.ChannelUpdated += f; 
        
        public void AddConnectedCallback(Func<Task> f) =>
            Client.Connected += f; 
        
        public void AddCurrentUserUpdatedCallback(Func<SocketSelfUser, SocketSelfUser, Task> f) =>
            Client.CurrentUserUpdated += f; 
        
        public void AddDisconnectedCallback(Func<Exception, Task> f) =>
            Client.Disconnected += f; 
        
        public void AddGuildAvailableCallback(Func<SocketGuild, Task> f) =>
            Client.GuildAvailable += f; 
        
        public void AddGuildJoinRequestDeletedCallback(Func<Cacheable<SocketGuildUser, ulong>, SocketGuild, Task> f) =>
            Client.GuildJoinRequestDeleted += f; 
        
        public void AddGuildMembersDownloadedCallback(Func<SocketGuild, Task> f) =>
            Client.GuildMembersDownloaded += f; 
        
        public void AddGuildMemberUpdatedCallback(Func<Cacheable<SocketGuildUser, ulong>, SocketGuildUser, Task> f) =>
            Client.GuildMemberUpdated += f; 
        
        public void AddGuildScheduledEventCancelledCallback(Func<SocketGuildEvent, Task> f) =>
            Client.GuildScheduledEventCancelled += f; 
        
        public void AddGuildScheduledEventCompletedCallback(Func<SocketGuildEvent, Task> f) =>
            Client.GuildScheduledEventCompleted += f; 
        
        public void AddGuildScheduledEventCreatedCallback(Func<SocketGuildEvent, Task> f) =>
            Client.GuildScheduledEventCreated += f; 
        
        public void AddGuildScheduledEventUpdatedCallback(Func<Cacheable<SocketGuildEvent, ulong>, SocketGuildEvent, Task> f) =>
            Client.GuildScheduledEventUpdated += f; 
        
        public void AddGuildScheduledEventUserAddCallback(Func<Cacheable<SocketUser, RestUser, IUser, ulong>, SocketGuildEvent, Task> f) =>
            Client.GuildScheduledEventUserAdd += f; 
        
        public void AddGuildScheduledEventUserRemoveCallback(Func<Cacheable<SocketUser, RestUser, IUser, ulong>, SocketGuildEvent, Task> f) =>
            Client.GuildScheduledEventUserRemove += f; 
        
        public void AddGuildStickerCreatedCallback(Func<SocketCustomSticker, Task> f) =>
            Client.GuildStickerCreated += f; 
        
        public void AddGuildStickerDeletedCallback(Func<SocketCustomSticker, Task> f) =>
            Client.GuildStickerDeleted += f; 
        
        public void AddGuildStickerUpdatedCallback(Func<SocketCustomSticker, SocketCustomSticker, Task> f) =>
            Client.GuildStickerUpdated += f; 
        
        public void AddGuildUnavailableCallback(Func<SocketGuild, Task> f) =>
            Client.GuildUnavailable += f; 
        
        public void AddGuildUpdatedCallback(Func<SocketGuild, SocketGuild, Task> f) =>
            Client.GuildUpdated += f; 
        
        public void AddIntegrationCreatedCallback(Func<IIntegration, Task> f) =>
            Client.IntegrationCreated += f; 
        
        public void AddIntegrationDeletedCallback(Func<IGuild, ulong, Optional<ulong>, Task> f) =>
            Client.IntegrationDeleted += f; 
        
        public void AddIntegrationUpdatedCallback(Func<IIntegration, Task> f) =>
            Client.IntegrationUpdated += f; 
        
        public void AddInteractionCreatedCallback(Func<SocketInteraction, Task> f) =>
            Client.InteractionCreated += f; 
        
        public void AddInviteCreatedCallback(Func<SocketInvite, Task> f) =>
            Client.InviteCreated += f; 
        
        public void AddInviteDeletedCallback(Func<SocketGuildChannel, string, Task> f) =>
            Client.InviteDeleted += f; 
        
        public void AddJoinedGuildCallback(Func<SocketGuild, Task> f) =>
            Client.JoinedGuild += f; 
        
        public void AddLatencyUpdatedCallback(Func<int, int, Task> f) =>
            Client.LatencyUpdated += f; 
        
        public void AddLeftGuildCallback(Func<SocketGuild, Task> f) =>
            Client.LeftGuild += f; 
        
        public void AddLogCallback(Func<LogMessage, Task> f) =>
            Client.Log += f; 
        
        public void AddLoggedInCallback(Func<Task> f) =>
            Client.LoggedIn += f; 
        
        public void AddLoggedOutCallback(Func<Task> f) =>
            Client.LoggedOut += f; 
        
        public void AddMessageCommandExecutedCallback(Func<SocketMessageCommand, Task> f) =>
            Client.MessageCommandExecuted += f; 
        
        public void AddMessageDeletedCallback(Func<Cacheable<IMessage, ulong>, Cacheable<IMessageChannel, ulong>, Task> f) =>
            Client.MessageDeleted += f; 
        
        public void AddMessageReceivedCallback(Func<SocketMessage, Task> f) =>
            Client.MessageReceived += f; 
        
        public void AddMessagesBulkDeletedCallback(Func<IReadOnlyCollection<Cacheable<IMessage, ulong>>, Cacheable<IMessageChannel, ulong>, Task> f) =>
            Client.MessagesBulkDeleted += f; 
        
        public void AddMessageUpdatedCallback(Func<Cacheable<IMessage, ulong>, SocketMessage, ISocketMessageChannel, Task> f) =>
            Client.MessageUpdated += f; 
        
        public void AddModalSubmittedCallback(Func<SocketModal, Task> f) =>
            Client.ModalSubmitted += f; 
        
        public void AddPresenceUpdatedCallback(Func<SocketUser, SocketPresence, SocketPresence, Task> f) =>
            Client.PresenceUpdated += f; 
        
        public void AddReactionAddedCallback(Func<Cacheable<IUserMessage, ulong>, Cacheable<IMessageChannel, ulong>, SocketReaction, Task> f) =>
            Client.ReactionAdded += f; 
        
        public void AddReactionRemovedCallback(Func<Cacheable<IUserMessage, ulong>, Cacheable<IMessageChannel, ulong>, SocketReaction, Task> f) =>
            Client.ReactionRemoved += f; 
        
        public void AddReactionsClearedCallback(Func<Cacheable<IUserMessage, ulong>, Cacheable<IMessageChannel, ulong>, Task> f) =>
            Client.ReactionsCleared += f; 
        
        public void AddReactionsRemovedForEmoteCallback(Func<Cacheable<IUserMessage, ulong>, Cacheable<IMessageChannel, ulong>, IEmote, Task> f) =>
            Client.ReactionsRemovedForEmote += f; 
        
        public void AddReadyCallback(Func<Task> f) =>
            Client.Ready += f; 
        
        public void AddRecipientAddedCallback(Func<SocketGroupUser, Task> f) =>
            Client.RecipientAdded += f; 
        
        public void AddRecipientRemovedCallback(Func<SocketGroupUser, Task> f) =>
            Client.RecipientRemoved += f; 
        
        public void AddRequestToSpeakCallback(Func<SocketStageChannel, SocketGuildUser, Task> f) =>
            Client.RequestToSpeak += f; 
        
        public void AddRoleCreatedCallback(Func<SocketRole, Task> f) =>
            Client.RoleCreated += f; 
        
        public void AddRoleDeletedCallback(Func<SocketRole, Task> f) =>
            Client.RoleDeleted += f; 
        
        public void AddRoleUpdatedCallback(Func<SocketRole, SocketRole, Task> f) =>
            Client.RoleUpdated += f; 
        
        public void AddSelectMenuExecutedCallback(Func<SocketMessageComponent, Task> f) =>
            Client.SelectMenuExecuted += f;

        public void AddSlashCommandExecutedCallback(Func<SocketSlashCommand, Task> f) =>
            Client.SlashCommandExecuted += f;

        public void AddSpeakerAddedCallback(Func<SocketStageChannel, SocketGuildUser, Task> f) =>
            Client.SpeakerAdded += f; 
        
        public void AddSpeakerRemovedCallback(Func<SocketStageChannel, SocketGuildUser, Task> f) =>
            Client.SpeakerRemoved += f; 
        
        public void AddStageEndedCallback(Func<SocketStageChannel, Task> f) =>
            Client.StageEnded += f; 
        
        public void AddStageStartedCallback(Func<SocketStageChannel, Task> f) =>
            Client.StageStarted += f; 
        
        public void AddStageUpdatedCallback(Func<SocketStageChannel, SocketStageChannel, Task> f) =>
            Client.StageUpdated += f; 
        
        public void AddThreadCreatedCallback(Func<SocketThreadChannel, Task> f) =>
            Client.ThreadCreated += f; 
        
        public void AddThreadDeletedCallback(Func<Cacheable<SocketThreadChannel, ulong>, Task> f) =>
            Client.ThreadDeleted += f; 
        
        public void AddThreadMemberJoinedCallback(Func<SocketThreadUser, Task> f) =>
            Client.ThreadMemberJoined += f; 
        
        public void AddThreadMemberLeftCallback(Func<SocketThreadUser, Task> f) =>
            Client.ThreadMemberLeft += f; 
        
        public void AddThreadUpdatedCallback(Func<Cacheable<SocketThreadChannel, ulong>, SocketThreadChannel, Task> f) =>
            Client.ThreadUpdated += f; 
        
        public void AddUserBannedCallback(Func<SocketUser, SocketGuild, Task> f) =>
            Client.UserBanned += f; 
        
        public void AddUserCommandExecutedCallback(Func<SocketUserCommand, Task> f) =>
            Client.UserCommandExecuted += f; 
        
        public void AddUserIsTypingCallback(Func<Cacheable<IUser, ulong>, Cacheable<IMessageChannel, ulong>, Task> f) =>
            Client.UserIsTyping += f; 
        
        public void AddUserJoinedCallback(Func<SocketGuildUser, Task> f) =>
            Client.UserJoined += f; 
        
        public void AddUserLeftCallback(Func<SocketGuild, SocketUser, Task> f) =>
            Client.UserLeft += f; 
        
        public void AddUserUnbannedCallback(Func<SocketUser, SocketGuild, Task> f) =>
            Client.UserUnbanned += f; 
        
        public void AddUserUpdatedCallback(Func<SocketUser, SocketUser, Task> f) =>
            Client.UserUpdated += f;
        
        public void AddUserVoiceStateUpdatedCallback(Func<SocketUser, SocketVoiceState, SocketVoiceState, Task> f) =>
            Client.UserVoiceStateUpdated += f;
        
        public void AddVoiceServerUpdatedCallback(Func<SocketVoiceServer, Task> f) =>
            Client.VoiceServerUpdated += f;
        
        public void AddWebhooksUpdatedCallback(Func<SocketGuild, SocketChannel, Task> f) =>
            Client.WebhooksUpdated += f;
        #endregion
        ///Awaits a list of console commands (currently only "Quit")
        private Task AwaitConsoleCommands()
        {
            Console.Write(">");
            string? input = Console.ReadLine();
            while(input == null || input.ToLower() != "quit") {
                Console.Write("Command not recognized\n>");
                input = Console.ReadLine(); 
            }
            return Task.CompletedTask;
        }

        ///Commands
        private static Dictionary<ulong, Func<SocketSlashCommand, Task>> Callbacks = new Dictionary<ulong, Func<SocketSlashCommand, Task>>();
        private static Dictionary<string, Tuple<Func<SocketSlashCommand, Task>, SocketSlashCommand>> Buttons = new Dictionary<string, Tuple<Func<SocketSlashCommand, Task>, SocketSlashCommand>>();
        ///Adds a command to the current guild
        public async Task AddCommand(Command _command)
        {
            try  { Callbacks.Add((await Client.GetGuild(PrivateVariables!.Server).CreateApplicationCommandAsync(_command.GetCommandBuilder().Build())).Id, _command.GetCallback()); }
            catch (Exception ex) { Debug.WriteLine(ex); }
        }
        ///Represents a full command with confirmation support
        public class Command
        {
            private static ulong buttonsCreated = 0;
            public Command() { }
            public string name = "";
            public string description = "";
            public bool modOnly = false;
            public bool requiresConfirmation = false;
            public Func<SocketSlashCommand, Task> callback = (SocketSlashCommand _command) => { throw new NotImplementedException(); };
            public List<SlashCommandOptionBuilder> options = new List<SlashCommandOptionBuilder>();
            public SlashCommandBuilder GetCommandBuilder() 
            {
                return new SlashCommandBuilder()
                {
                    Name = name,
                    Description = description,
                    DefaultMemberPermissions = modOnly ? GuildPermission.KickMembers : GuildPermission.ViewChannel,
                    Options = options,
                };
            }
            public Func<SocketSlashCommand, Task> GetCallback()
            {
                return !requiresConfirmation ? callback : (async (_command) =>
                {
                    ComponentBuilder cb = new ComponentBuilder().WithButton("Confirm", buttonsCreated.ToString(), ButtonStyle.Danger);
                    Buttons.Add(buttonsCreated.ToString(), new Tuple<Func<SocketSlashCommand, Task>, SocketSlashCommand>(callback, _command));
                    buttonsCreated++;
                    await _command.RespondAsync(ephemeral: true, components: cb.Build());
                });
            }
        }
    }
    ///A class to be overridden to create modules
    internal abstract class Module
    {
        ///The Program this Module is attached to
        protected Program Bot;

        ///Constructor
        public Module(Program bot) { Bot = bot; }

        ///Adds all events to the client
        public abstract Task InitilizeModule();
    }
    
    internal class PrivateVariable 
    {
        public string? KEY {get;set;}   
        public ulong Server {get;set;}
        public ulong LogChannel {get;set;}
        public ulong AnnouncementChannel {get;set;}
        public ulong CommandChannel {get;set;}
    }
}