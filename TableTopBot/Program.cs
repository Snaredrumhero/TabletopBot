using Discord;
using Discord.Rest;
using Discord.WebSocket;
using System.Diagnostics;

namespace TableTopBot
{
    internal class Program
    {
        //The bot's client
        private DiscordSocketClient _client = new DiscordSocketClient(new DiscordSocketConfig { GatewayIntents = GatewayIntents.All });
        //Channels that slash commands can be executed in
        private readonly ulong[] _allowedCommandChannels = { 1104487160226258964 };
        //All slash commands
        private List<Func<SocketSlashCommand, Task>> _slashCommandCallbacks = new List<Func<SocketSlashCommand, Task>>();
        
        public static Task Main(string[] args) => new Program().MainAsync();

        private async Task MainAsync()
        {
            //Required Lambdas
            _client.Log += (LogMessage msg) =>
            {
                Console.WriteLine(msg.ToString());
                return Task.CompletedTask;
            };
            _client.SlashCommandExecuted += ClientSlashCommandExecuted;
            
            //Init all moduels
            new PingPong(this);

            //run bot
            await _client.LoginAsync(TokenType.Bot, "PrivateVariables.KEY");
            await _client.StartAsync();
            await Task.Delay(Timeout.Infinite);
        }

        private async Task ClientSlashCommandExecuted(SocketSlashCommand command)
        {
            //Guard clause: Only execute in approved channels
            if (_allowedCommandChannels.All(z => z != command.ChannelId))
            {
                return;
            }

            //Execute each callback in SlashCommandCallbacks
            foreach (Func<SocketSlashCommand, Task> callback in _slashCommandCallbacks) 
                await callback(command);
        }

        //all possible callback events that can be used
        #region Callback Adders
        public void AddApplicationCommandCreatedCallback(Func<SocketApplicationCommand, Task> f) =>
            _client.ApplicationCommandCreated += f; 
        
        public void AddApplicationCommandDeletedCallback(Func<SocketApplicationCommand, Task> f) =>
            _client.ApplicationCommandDeleted += f; 
        
        public void AddApplicationCommandUpdatedCallback(Func<SocketApplicationCommand, Task> f) =>
            _client.ApplicationCommandUpdated += f; 
        
        public void AddAutocompleteExecutedCallback(Func<SocketAutocompleteInteraction, Task> f) =>
            _client.AutocompleteExecuted += f; 
        
        public void AddButtonExecutedCallback(Func<SocketMessageComponent, Task> f) =>
            _client.ButtonExecuted += f; 
        
        public void AddChannelCreatedCallback(Func<SocketChannel, Task> f) =>
            _client.ChannelCreated += f; 
        
        public void AddChannelDestroyedCallback(Func<SocketChannel, Task> f) =>
            _client.ChannelDestroyed += f; 
        
        public void AddChannelUpdatedCallback(Func<SocketChannel, SocketChannel, Task> f) =>
            _client.ChannelUpdated += f; 
        
        public void AddConnectedCallback(Func<Task> f) =>
            _client.Connected += f; 
        
        public void AddCurrentUserUpdatedCallback(Func<SocketSelfUser, SocketSelfUser, Task> f) =>
            _client.CurrentUserUpdated += f; 
        
        public void AddDisconnectedCallback(Func<Exception, Task> f) =>
            _client.Disconnected += f; 
        
        public void AddGuildAvailableCallback(Func<SocketGuild, Task> f) =>
            _client.GuildAvailable += f; 
        
        public void AddGuildJoinRequestDeletedCallback(Func<Cacheable<SocketGuildUser, ulong>, SocketGuild, Task> f) =>
            _client.GuildJoinRequestDeleted += f; 
        
        public void AddGuildMembersDownloadedCallback(Func<SocketGuild, Task> f) =>
            _client.GuildMembersDownloaded += f; 
        
        public void AddGuildMemberUpdatedCallback(Func<Cacheable<SocketGuildUser, ulong>, SocketGuildUser, Task> f) =>
            _client.GuildMemberUpdated += f; 
        
        public void AddGuildScheduledEventCancelledCallback(Func<SocketGuildEvent, Task> f) =>
            _client.GuildScheduledEventCancelled += f; 
        
        public void AddGuildScheduledEventCompletedCallback(Func<SocketGuildEvent, Task> f) =>
            _client.GuildScheduledEventCompleted += f; 
        
        public void AddGuildScheduledEventCreatedCallback(Func<SocketGuildEvent, Task> f) =>
            _client.GuildScheduledEventCreated += f; 
        
        public void AddGuildScheduledEventUpdatedCallback(Func<Cacheable<SocketGuildEvent, ulong>, SocketGuildEvent, Task> f) =>
            _client.GuildScheduledEventUpdated += f; 
        
        public void AddGuildScheduledEventUserAddCallback(Func<Cacheable<SocketUser, RestUser, IUser, ulong>, SocketGuildEvent, Task> f) =>
            _client.GuildScheduledEventUserAdd += f; 
        
        public void AddGuildScheduledEventUserRemoveCallback(Func<Cacheable<SocketUser, RestUser, IUser, ulong>, SocketGuildEvent, Task> f) =>
            _client.GuildScheduledEventUserRemove += f; 
        
        public void AddGuildStickerCreatedCallback(Func<SocketCustomSticker, Task> f) =>
            _client.GuildStickerCreated += f; 
        
        public void AddGuildStickerDeletedCallback(Func<SocketCustomSticker, Task> f) =>
            _client.GuildStickerDeleted += f; 
        
        public void AddGuildStickerUpdatedCallback(Func<SocketCustomSticker, SocketCustomSticker, Task> f) =>
            _client.GuildStickerUpdated += f; 
        
        public void AddGuildUnavailableCallback(Func<SocketGuild, Task> f) =>
            _client.GuildUnavailable += f; 
        
        public void AddGuildUpdatedCallback(Func<SocketGuild, SocketGuild, Task> f) =>
            _client.GuildUpdated += f; 
        
        public void AddIntegrationCreatedCallback(Func<IIntegration, Task> f) =>
            _client.IntegrationCreated += f; 
        
        public void AddIntegrationDeletedCallback(Func<IGuild, ulong, Optional<ulong>, Task> f) =>
            _client.IntegrationDeleted += f; 
        
        public void AddIntegrationUpdatedCallback(Func<IIntegration, Task> f) =>
            _client.IntegrationUpdated += f; 
        
        public void AddInteractionCreatedCallback(Func<SocketInteraction, Task> f) =>
            _client.InteractionCreated += f; 
        
        public void AddInviteCreatedCallback(Func<SocketInvite, Task> f) =>
            _client.InviteCreated += f; 
        
        public void AddInviteDeletedCallback(Func<SocketGuildChannel, string, Task> f) =>
            _client.InviteDeleted += f; 
        
        public void AddJoinedGuildCallback(Func<SocketGuild, Task> f) =>
            _client.JoinedGuild += f; 
        
        public void AddLatencyUpdatedCallback(Func<int, int, Task> f) =>
            _client.LatencyUpdated += f; 
        
        public void AddLeftGuildCallback(Func<SocketGuild, Task> f) =>
            _client.LeftGuild += f; 
        
        public void AddLogCallback(Func<LogMessage, Task> f) =>
            _client.Log += f; 
        
        public void AddLoggedInCallback(Func<Task> f) =>
            _client.LoggedIn += f; 
        
        public void AddLoggedOutCallback(Func<Task> f) =>
            _client.LoggedOut += f; 
        
        public void AddMessageCommandExecutedCallback(Func<SocketMessageCommand, Task> f) =>
            _client.MessageCommandExecuted += f; 
        
        public void AddMessageDeletedCallback(Func<Cacheable<IMessage, ulong>, Cacheable<IMessageChannel, ulong>, Task> f) =>
            _client.MessageDeleted += f; 
        
        public void AddMessageReceivedCallback(Func<SocketMessage, Task> f) =>
            _client.MessageReceived += f; 
        
        public void AddMessagesBulkDeletedCallback(Func<IReadOnlyCollection<Cacheable<IMessage, ulong>>, Cacheable<IMessageChannel, ulong>, Task> f) =>
            _client.MessagesBulkDeleted += f; 
        
        public void AddMessageUpdatedCallback(Func<Cacheable<IMessage, ulong>, SocketMessage, ISocketMessageChannel, Task> f) =>
            _client.MessageUpdated += f; 
        
        public void AddModalSubmittedCallback(Func<SocketModal, Task> f) =>
            _client.ModalSubmitted += f; 
        
        public void AddPresenceUpdatedCallback(Func<SocketUser, SocketPresence, SocketPresence, Task> f) =>
            _client.PresenceUpdated += f; 
        
        public void AddReactionAddedCallback(Func<Cacheable<IUserMessage, ulong>, Cacheable<IMessageChannel, ulong>, SocketReaction, Task> f) =>
            _client.ReactionAdded += f; 
        
        public void AddReactionRemovedCallback(Func<Cacheable<IUserMessage, ulong>, Cacheable<IMessageChannel, ulong>, SocketReaction, Task> f) =>
            _client.ReactionRemoved += f; 
        
        public void AddReactionsClearedCallback(Func<Cacheable<IUserMessage, ulong>, Cacheable<IMessageChannel, ulong>, Task> f) =>
            _client.ReactionsCleared += f; 
        
        public void AddReactionsRemovedForEmoteCallback(Func<Cacheable<IUserMessage, ulong>, Cacheable<IMessageChannel, ulong>, IEmote, Task> f) =>
            _client.ReactionsRemovedForEmote += f; 
        
        public void AddReadyCallback(Func<Task> f) =>
            _client.Ready += f; 
        
        public void AddRecipientAddedCallback(Func<SocketGroupUser, Task> f) =>
            _client.RecipientAdded += f; 
        
        public void AddRecipientRemovedCallback(Func<SocketGroupUser, Task> f) =>
            _client.RecipientRemoved += f; 
        
        public void AddRequestToSpeakCallback(Func<SocketStageChannel, SocketGuildUser, Task> f) =>
            _client.RequestToSpeak += f; 
        
        public void AddRoleCreatedCallback(Func<SocketRole, Task> f) =>
            _client.RoleCreated += f; 
        
        public void AddRoleDeletedCallback(Func<SocketRole, Task> f) =>
            _client.RoleDeleted += f; 
        
        public void AddRoleUpdatedCallback(Func<SocketRole, SocketRole, Task> f) =>
            _client.RoleUpdated += f; 
        
        public void AddSelectMenuExecutedCallback(Func<SocketMessageComponent, Task> f) =>
            _client.SelectMenuExecuted += f; 
        
        public void AddSpeakerAddedCallback(Func<SocketStageChannel, SocketGuildUser, Task> f) =>
            _client.SpeakerAdded += f; 
        
        public void AddSpeakerRemovedCallback(Func<SocketStageChannel, SocketGuildUser, Task> f) =>
            _client.SpeakerRemoved += f; 
        
        public void AddStageEndedCallback(Func<SocketStageChannel, Task> f) =>
            _client.StageEnded += f; 
        
        public void AddStageStartedCallback(Func<SocketStageChannel, Task> f) =>
            _client.StageStarted += f; 
        
        public void AddStageUpdatedCallback(Func<SocketStageChannel, SocketStageChannel, Task> f) =>
            _client.StageUpdated += f; 
        
        public void AddThreadCreatedCallback(Func<SocketThreadChannel, Task> f) =>
            _client.ThreadCreated += f; 
        
        public void AddThreadDeletedCallback(Func<Cacheable<SocketThreadChannel, ulong>, Task> f) =>
            _client.ThreadDeleted += f; 
        
        public void AddThreadMemberJoinedCallback(Func<SocketThreadUser, Task> f) =>
            _client.ThreadMemberJoined += f; 
        
        public void AddThreadMemberLeftCallback(Func<SocketThreadUser, Task> f) =>
            _client.ThreadMemberLeft += f; 
        
        public void AddThreadUpdatedCallback(Func<Cacheable<SocketThreadChannel, ulong>, SocketThreadChannel, Task> f) =>
            _client.ThreadUpdated += f; 
        
        public void AddUserBannedCallback(Func<SocketUser, SocketGuild, Task> f) =>
            _client.UserBanned += f; 
        
        public void AddUserCommandExecutedCallback(Func<SocketUserCommand, Task> f) =>
            _client.UserCommandExecuted += f; 
        
        public void AddUserIsTypingCallback(Func<Cacheable<IUser, ulong>, Cacheable<IMessageChannel, ulong>, Task> f) =>
            _client.UserIsTyping += f; 
        
        public void AddUserJoinedCallback(Func<SocketGuildUser, Task> f) =>
            _client.UserJoined += f; 
        
        public void AddUserLeftCallback(Func<SocketGuild, SocketUser, Task> f) =>
            _client.UserLeft += f; 
        
        public void AddUserUnbannedCallback(Func<SocketUser, SocketGuild, Task> f) =>
            _client.UserUnbanned += f; 
        
        public void AddUserUpdatedCallback(Func<SocketUser, SocketUser, Task> f) =>
            _client.UserUpdated += f;
        
        public void AddUserVoiceStateUpdatedCallback(Func<SocketUser, SocketVoiceState, SocketVoiceState, Task> f) =>
            _client.UserVoiceStateUpdated += f;
        
        public void AddVoiceServerUpdatedCallback(Func<SocketVoiceServer, Task> f) =>
            _client.VoiceServerUpdated += f;
        
        public void AddWebhooksUpdatedCallback(Func<SocketGuild, SocketChannel, Task> f) =>
            _client.WebhooksUpdated += f;
        #endregion

        //Changed for channel checking
        public void AddSlashCommandExecutedCallback(Func<SocketSlashCommand, Task> f) =>
            _slashCommandCallbacks.Add(f);

        public async void AddGuildCommand(SlashCommandBuilder builder)
        {
            try { await _client.GetGuild(1047337930965909646).CreateApplicationCommandAsync(builder.Build()); }
            catch (Exception ex) { Debug.WriteLine(ex); }
        }
    }
    //A class to be overridden to create modules
    internal abstract class Module
    {
        //The Program this Module is attached to
        protected Program Bot;

        //Constructor
        public Module(Program bot)
        {
            Bot = bot;
            InitilizeModule();
        }

        //Adds all events to the client
        public abstract void InitilizeModule();
    }
}