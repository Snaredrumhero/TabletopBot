using Discord;
using Discord.Rest;
using Discord.WebSocket;
using System.Diagnostics;

namespace TableTopBot
{
    internal class Program
    {
        //The bot's client
        private DiscordSocketClient Client = new DiscordSocketClient(new DiscordSocketConfig { GatewayIntents = GatewayIntents.All });
        //Channels that slash commands can be executed in
        private readonly ulong[] AllowedCommandChannels = { 1104487160226258964 };
        //All slash commands
        private List<Func<SocketSlashCommand, Task>> SlashCommandCallbacks = new List<Func<SocketSlashCommand, Task>>();
        //All created commands (helps deallocate on turn off)
        private List<SocketApplicationCommand> CreatedCommands = new List<SocketApplicationCommand>();

        public static Task Main(string[] args) => new Program().MainAsync();

        private async Task MainAsync()
        {
            Console.Title = "TabletopBot";
            //Required Lambdas
            Client.Log += (LogMessage msg) =>
            {
                Console.WriteLine(msg.ToString());
                return Task.CompletedTask;
            };
            Client.SlashCommandExecuted += ClientSlashCommandExecuted;
            
            //Init all moduels
            new PingPong(this);
            new XPModule(this);

            //run bot
            await Client.LoginAsync(TokenType.Bot, PrivateVariables.KEY);
            await Client.StartAsync();
            await AwaitConsoleCommands();
            foreach (SocketApplicationCommand c in CreatedCommands)
                await c.DeleteAsync();
            await Client.LogoutAsync();
            
        }

        private async Task ClientSlashCommandExecuted(SocketSlashCommand command)
        {
            //Guard clause: Only execute in approved channels
            if (AllowedCommandChannels.All(z => z != command.ChannelId))
                return;
            //Execute each callback in SlashCommandCallbacks
            foreach (Func<SocketSlashCommand, Task> callback in SlashCommandCallbacks)
            {
                try { await callback(command); }
                catch (Exception _ex)
                {
                    EmbedBuilder embed = new EmbedBuilder();
                    embed.AddField("Error", _ex.Message);
                    await command.RespondAsync(embed: embed.Build(), ephemeral: true);
                }
            }
        }

        //all possible callback events that can be used
        #region Callback Adders
        public void AddApplicationCommandCreatedCallback(Func<SocketApplicationCommand, Task> _f) =>
            Client.ApplicationCommandCreated += _f; 
        
        public void AddApplicationCommandDeletedCallback(Func<SocketApplicationCommand, Task> _f) =>
            Client.ApplicationCommandDeleted += _f; 
        
        public void AddApplicationCommandUpdatedCallback(Func<SocketApplicationCommand, Task> _f) =>
            Client.ApplicationCommandUpdated += _f; 
        
        public void AddAutocompleteExecutedCallback(Func<SocketAutocompleteInteraction, Task> _f) =>
            Client.AutocompleteExecuted += _f; 
        
        public void AddButtonExecutedCallback(Func<SocketMessageComponent, Task> _f) =>
            Client.ButtonExecuted += _f; 
        
        public void AddChannelCreatedCallback(Func<SocketChannel, Task> _f) =>
            Client.ChannelCreated += _f; 
        
        public void AddChannelDestroyedCallback(Func<SocketChannel, Task> _f) =>
            Client.ChannelDestroyed += _f; 
        
        public void AddChannelUpdatedCallback(Func<SocketChannel, SocketChannel, Task> _f) =>
            Client.ChannelUpdated += _f; 
        
        public void AddConnectedCallback(Func<Task> _f) =>
            Client.Connected += _f; 
        
        public void AddCurrentUserUpdatedCallback(Func<SocketSelfUser, SocketSelfUser, Task> _f) =>
            Client.CurrentUserUpdated += _f; 
        
        public void AddDisconnectedCallback(Func<Exception, Task> _f) =>
            Client.Disconnected += _f; 
        
        public void AddGuildAvailableCallback(Func<SocketGuild, Task> _f) =>
            Client.GuildAvailable += _f; 
        
        public void AddGuildJoinRequestDeletedCallback(Func<Cacheable<SocketGuildUser, ulong>, SocketGuild, Task> _f) =>
            Client.GuildJoinRequestDeleted += _f; 
        
        public void AddGuildMembersDownloadedCallback(Func<SocketGuild, Task> _f) =>
            Client.GuildMembersDownloaded += _f; 
        
        public void AddGuildMemberUpdatedCallback(Func<Cacheable<SocketGuildUser, ulong>, SocketGuildUser, Task> _f) =>
            Client.GuildMemberUpdated += _f; 
        
        public void AddGuildScheduledEventCancelledCallback(Func<SocketGuildEvent, Task> _f) =>
            Client.GuildScheduledEventCancelled += _f; 
        
        public void AddGuildScheduledEventCompletedCallback(Func<SocketGuildEvent, Task> _f) =>
            Client.GuildScheduledEventCompleted += _f; 
        
        public void AddGuildScheduledEventCreatedCallback(Func<SocketGuildEvent, Task> _f) =>
            Client.GuildScheduledEventCreated += _f; 
        
        public void AddGuildScheduledEventUpdatedCallback(Func<Cacheable<SocketGuildEvent, ulong>, SocketGuildEvent, Task> _f) =>
            Client.GuildScheduledEventUpdated += _f; 
        
        public void AddGuildScheduledEventUserAddCallback(Func<Cacheable<SocketUser, RestUser, IUser, ulong>, SocketGuildEvent, Task> _f) =>
            Client.GuildScheduledEventUserAdd += _f; 
        
        public void AddGuildScheduledEventUserRemoveCallback(Func<Cacheable<SocketUser, RestUser, IUser, ulong>, SocketGuildEvent, Task> _f) =>
            Client.GuildScheduledEventUserRemove += _f; 
        
        public void AddGuildStickerCreatedCallback(Func<SocketCustomSticker, Task> _f) =>
            Client.GuildStickerCreated += _f; 
        
        public void AddGuildStickerDeletedCallback(Func<SocketCustomSticker, Task> _f) =>
            Client.GuildStickerDeleted += _f; 
        
        public void AddGuildStickerUpdatedCallback(Func<SocketCustomSticker, SocketCustomSticker, Task> _f) =>
            Client.GuildStickerUpdated += _f; 
        
        public void AddGuildUnavailableCallback(Func<SocketGuild, Task> _f) =>
            Client.GuildUnavailable += _f; 
        
        public void AddGuildUpdatedCallback(Func<SocketGuild, SocketGuild, Task> _f) =>
            Client.GuildUpdated += _f; 
        
        public void AddIntegrationCreatedCallback(Func<IIntegration, Task> _f) =>
            Client.IntegrationCreated += _f; 
        
        public void AddIntegrationDeletedCallback(Func<IGuild, ulong, Optional<ulong>, Task> _f) =>
            Client.IntegrationDeleted += _f; 
        
        public void AddIntegrationUpdatedCallback(Func<IIntegration, Task> _f) =>
            Client.IntegrationUpdated += _f; 
        
        public void AddInteractionCreatedCallback(Func<SocketInteraction, Task> _f) =>
            Client.InteractionCreated += _f; 
        
        public void AddInviteCreatedCallback(Func<SocketInvite, Task> _f) =>
            Client.InviteCreated += _f; 
        
        public void AddInviteDeletedCallback(Func<SocketGuildChannel, string, Task> _f) =>
            Client.InviteDeleted += _f; 
        
        public void AddJoinedGuildCallback(Func<SocketGuild, Task> _f) =>
            Client.JoinedGuild += _f; 
        
        public void AddLatencyUpdatedCallback(Func<int, int, Task> _f) =>
            Client.LatencyUpdated += _f; 
        
        public void AddLeftGuildCallback(Func<SocketGuild, Task> _f) =>
            Client.LeftGuild += _f; 
        
        public void AddLogCallback(Func<LogMessage, Task> _f) =>
            Client.Log += _f; 
        
        public void AddLoggedInCallback(Func<Task> _f) =>
            Client.LoggedIn += _f; 
        
        public void AddLoggedOutCallback(Func<Task> _f) =>
            Client.LoggedOut += _f; 
        
        public void AddMessageCommandExecutedCallback(Func<SocketMessageCommand, Task> _f) =>
            Client.MessageCommandExecuted += _f; 
        
        public void AddMessageDeletedCallback(Func<Cacheable<IMessage, ulong>, Cacheable<IMessageChannel, ulong>, Task> _f) =>
            Client.MessageDeleted += _f; 
        
        public void AddMessageReceivedCallback(Func<SocketMessage, Task> _f) =>
            Client.MessageReceived += _f; 
        
        public void AddMessagesBulkDeletedCallback(Func<IReadOnlyCollection<Cacheable<IMessage, ulong>>, Cacheable<IMessageChannel, ulong>, Task> _f) =>
            Client.MessagesBulkDeleted += _f; 
        
        public void AddMessageUpdatedCallback(Func<Cacheable<IMessage, ulong>, SocketMessage, ISocketMessageChannel, Task> _f) =>
            Client.MessageUpdated += _f; 
        
        public void AddModalSubmittedCallback(Func<SocketModal, Task> _f) =>
            Client.ModalSubmitted += _f; 
        
        public void AddPresenceUpdatedCallback(Func<SocketUser, SocketPresence, SocketPresence, Task> _f) =>
            Client.PresenceUpdated += _f; 
        
        public void AddReactionAddedCallback(Func<Cacheable<IUserMessage, ulong>, Cacheable<IMessageChannel, ulong>, SocketReaction, Task> _f) =>
            Client.ReactionAdded += _f; 
        
        public void AddReactionRemovedCallback(Func<Cacheable<IUserMessage, ulong>, Cacheable<IMessageChannel, ulong>, SocketReaction, Task> _f) =>
            Client.ReactionRemoved += _f; 
        
        public void AddReactionsClearedCallback(Func<Cacheable<IUserMessage, ulong>, Cacheable<IMessageChannel, ulong>, Task> _f) =>
            Client.ReactionsCleared += _f; 
        
        public void AddReactionsRemovedForEmoteCallback(Func<Cacheable<IUserMessage, ulong>, Cacheable<IMessageChannel, ulong>, IEmote, Task> _f) =>
            Client.ReactionsRemovedForEmote += _f; 
        
        public void AddReadyCallback(Func<Task> _f) =>
            Client.Ready += _f; 
        
        public void AddRecipientAddedCallback(Func<SocketGroupUser, Task> _f) =>
            Client.RecipientAdded += _f; 
        
        public void AddRecipientRemovedCallback(Func<SocketGroupUser, Task> _f) =>
            Client.RecipientRemoved += _f; 
        
        public void AddRequestToSpeakCallback(Func<SocketStageChannel, SocketGuildUser, Task> _f) =>
            Client.RequestToSpeak += _f; 
        
        public void AddRoleCreatedCallback(Func<SocketRole, Task> _f) =>
            Client.RoleCreated += _f; 
        
        public void AddRoleDeletedCallback(Func<SocketRole, Task> _f) =>
            Client.RoleDeleted += _f; 
        
        public void AddRoleUpdatedCallback(Func<SocketRole, SocketRole, Task> _f) =>
            Client.RoleUpdated += _f; 
        
        public void AddSelectMenuExecutedCallback(Func<SocketMessageComponent, Task> _f) =>
            Client.SelectMenuExecuted += _f; 
        
        public void AddSpeakerAddedCallback(Func<SocketStageChannel, SocketGuildUser, Task> _f) =>
            Client.SpeakerAdded += _f; 
        
        public void AddSpeakerRemovedCallback(Func<SocketStageChannel, SocketGuildUser, Task> _f) =>
            Client.SpeakerRemoved += _f; 
        
        public void AddStageEndedCallback(Func<SocketStageChannel, Task> _f) =>
            Client.StageEnded += _f; 
        
        public void AddStageStartedCallback(Func<SocketStageChannel, Task> _f) =>
            Client.StageStarted += _f; 
        
        public void AddStageUpdatedCallback(Func<SocketStageChannel, SocketStageChannel, Task> _f) =>
            Client.StageUpdated += _f; 
        
        public void AddThreadCreatedCallback(Func<SocketThreadChannel, Task> _f) =>
            Client.ThreadCreated += _f; 
        
        public void AddThreadDeletedCallback(Func<Cacheable<SocketThreadChannel, ulong>, Task> _f) =>
            Client.ThreadDeleted += _f; 
        
        public void AddThreadMemberJoinedCallback(Func<SocketThreadUser, Task> _f) =>
            Client.ThreadMemberJoined += _f; 
        
        public void AddThreadMemberLeftCallback(Func<SocketThreadUser, Task> _f) =>
            Client.ThreadMemberLeft += _f; 
        
        public void AddThreadUpdatedCallback(Func<Cacheable<SocketThreadChannel, ulong>, SocketThreadChannel, Task> _f) =>
            Client.ThreadUpdated += _f; 
        
        public void AddUserBannedCallback(Func<SocketUser, SocketGuild, Task> _f) =>
            Client.UserBanned += _f; 
        
        public void AddUserCommandExecutedCallback(Func<SocketUserCommand, Task> _f) =>
            Client.UserCommandExecuted += _f; 
        
        public void AddUserIsTypingCallback(Func<Cacheable<IUser, ulong>, Cacheable<IMessageChannel, ulong>, Task> _f) =>
            Client.UserIsTyping += _f; 
        
        public void AddUserJoinedCallback(Func<SocketGuildUser, Task> _f) =>
            Client.UserJoined += _f; 
        
        public void AddUserLeftCallback(Func<SocketGuild, SocketUser, Task> _f) =>
            Client.UserLeft += _f; 
        
        public void AddUserUnbannedCallback(Func<SocketUser, SocketGuild, Task> _f) =>
            Client.UserUnbanned += _f; 
        
        public void AddUserUpdatedCallback(Func<SocketUser, SocketUser, Task> _f) =>
            Client.UserUpdated += _f;
        
        public void AddUserVoiceStateUpdatedCallback(Func<SocketUser, SocketVoiceState, SocketVoiceState, Task> _f) =>
            Client.UserVoiceStateUpdated += _f;
        
        public void AddVoiceServerUpdatedCallback(Func<SocketVoiceServer, Task> _f) =>
            Client.VoiceServerUpdated += _f;
        
        public void AddWebhooksUpdatedCallback(Func<SocketGuild, SocketChannel, Task> _f) =>
            Client.WebhooksUpdated += _f;
        #endregion

        //Changed for channel checking
        public void AddSlashCommandExecutedCallback(Func<SocketSlashCommand, Task> _f) =>
            SlashCommandCallbacks.Add(_f);

        public async void AddGuildCommand(SlashCommandBuilder _builder)
        {
            try { CreatedCommands.Add(await Client.GetGuild(1047337930965909646).CreateApplicationCommandAsync(_builder.Build())); }
            catch (Exception ex) { Debug.WriteLine(ex); }
        }
        private Task AwaitConsoleCommands()
        {
            Console.Write("> ");
            Console.Out.Flush(); //why no flush?
            string? input = Console.ReadLine();
            while(input == null || input != "Quit") {
                Console.Write("Command not recognized\n> ");
                input = Console.ReadLine(); 
            }
            return Task.CompletedTask;
        }
    }
    //A class to be overridden to create moduels
    internal abstract class Module
    {
        //The Program this Module is attached to
        protected Program Bot;

        //Constructor
        public Module(Program _bot)
        {
            Bot = _bot;
            InitilizeModule();
        }

        //Adds all events to the client
        public abstract void InitilizeModule();
    }
}