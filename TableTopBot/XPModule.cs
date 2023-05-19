using Discord;
using Discord.WebSocket;

namespace TableTopBot
{
    internal class XPModule : Module
    {
        public SocketTextChannel AnnouncementChannel() => Bot.Server().GetTextChannel(1106217661194571806);
        public SocketTextChannel CommandChannel() => Bot.Server().GetTextChannel(1104487160226258964);

        private XpStorage xpSystem = new XpStorage();
        public XPModule(Program _bot) : base(_bot) { }

        public override Task InitilizeModule()
        {
            Bot.AddConnectedCallback(async () =>
            {
                //
                await Bot.AddCommand(new Program.Command()
                {
                    name = "start",
                    description = "starts the all-day event.",
                    callback = (SocketSlashCommand _command) =>
                    {
                        CommandChannel().AddPermissionOverwriteAsync(Bot.Server().EveryoneRole, OverwritePermissions.DenyAll(CommandChannel()).Modify(viewChannel: PermValue.Allow, useApplicationCommands: PermValue.Allow, sendMessages: PermValue.Allow));
                        xpSystem.Clear();
                        return Task.CompletedTask;
                    },
                    modOnly = true,
                    requiresConfirmation = true,
                });
                //
                await Bot.AddCommand(new Program.Command()
                {
                    name = "end",
                    description = "ends the all-day event.",
                    callback = (SocketSlashCommand _command) =>
                    {
                        //displays the top 3 users to the all-day announcements channel for prizes
                        //could display overall statistics for the all-day as well
                        CommandChannel().AddPermissionOverwriteAsync(Bot.Server().EveryoneRole, OverwritePermissions.DenyAll(CommandChannel()));
                        return Task.CompletedTask;
                    },
                    modOnly = true,
                    requiresConfirmation = true,
                });
                //
                await Bot.AddCommand(new Program.Command()
                {
                    name = "draw-raffle",
                    description = "draws a raffle ticket",
                    callback = async (SocketSlashCommand _command) =>
                    {
                        await AnnouncementChannel().SendMessageAsync(xpSystem.DrawRaffle());
                    },
                    modOnly = true,
                    requiresConfirmation = true,
                });
                //
                await Bot.AddCommand(new Program.Command()
                {
                    name = "see-player",
                    description = "view a player's profile",
                    callback = async (SocketSlashCommand _command) =>
                    {
                        await _command.RespondAsync(embed: (new EmbedBuilder().AddField("Player Data", xpSystem.GetUser(((SocketGuildUser)_command.Data.Options.First().Value).Id).ToString())).Build(), ephemeral: true);
                    },
                    modOnly = true,
                    options = new List<SlashCommandOptionBuilder>() {
                        new SlashCommandOptionBuilder(){
                            Name = "player",
                            Type = ApplicationCommandOptionType.User,
                            Description = "the user to see",
                            IsRequired = true,
                        },
                    }
                });
                //
                await Bot.AddCommand(new Program.Command()
                {
                    name = "show-x-users",
                    description = "shows a leaderboard",
                    callback = (SocketSlashCommand _command) =>
                    {
                        //shows the entire profile of the top x users
                        return Task.CompletedTask;
                    },
                    modOnly = true,
                    options = new List<SlashCommandOptionBuilder>() {
                        new SlashCommandOptionBuilder(){
                            Name = "x",
                            Type = ApplicationCommandOptionType.Integer,
                            Description = "the number of users to see",
                            IsRequired = true,
                        },
                    }
                });
                //
                await Bot.AddCommand(new Program.Command()
                {
                    name = "remove-player-game",
                    description = "removes a game from a player's profile",
                    callback = (SocketSlashCommand _command) =>
                    {
                        //removes a game from a player
                        return Task.CompletedTask;
                    },
                    modOnly = true,
                    requiresConfirmation = true,
                    options = new List<SlashCommandOptionBuilder>() {
                        new SlashCommandOptionBuilder(){
                            Name = "player",
                            Type = ApplicationCommandOptionType.User,
                            Description = "the user",
                            IsRequired = true,
                        },
                        new SlashCommandOptionBuilder(){
                            Name = "id",
                            Type = ApplicationCommandOptionType.Integer,
                            Description = "the game's id",
                            IsRequired = true,
                        },
                    },
                });
                //
                await Bot.AddCommand(new Program.Command()
                {
                    name = "remove-player-achievement",
                    description = "removes an achievement from a player's profile",
                    callback = (SocketSlashCommand _command) =>
                    {
                        //removes an achievement from a player
                        return Task.CompletedTask;
                    },
                    modOnly = true,
                    requiresConfirmation = true,
                    options = new List<SlashCommandOptionBuilder>() {
                        new SlashCommandOptionBuilder() {
                            Name = "user",
                            Type = ApplicationCommandOptionType.User,
                            Description = "the user",
                            IsRequired = true,
                        },
                        new SlashCommandOptionBuilder(){
                            Name = "id",
                            Type = ApplicationCommandOptionType.String,
                            Description = "the achievement's name",
                            IsRequired = true,
                        },
                    },
                });
                //
                await Bot.AddCommand(new Program.Command()
                {
                    name = "remove-player",
                    description = "removes a player's profile",
                    callback = (SocketSlashCommand _command) =>
                    {
                        //removes a player from the event
                        return Task.CompletedTask;
                    },
                    modOnly = true,
                    requiresConfirmation = true,
                    options = new List<SlashCommandOptionBuilder>(){
                        new SlashCommandOptionBuilder(){
                            Name = "player",
                            Type = ApplicationCommandOptionType.User,
                            Description = "the user to remove",
                            IsRequired = true,
                        },
                    },
                });
                //
                await Bot.AddCommand(new Program.Command()
                {
                    name = "join-event",
                    description = "registers you for the current event",
                    callback = (SocketSlashCommand _command) =>
                    {
                        string PID = (string)_command.Data.Options.First().Value;
                        if (PID[0] != 'P' || PID.Length != 10)
                            throw new InvalidDataException(message: "Invalid PID.");
                        for (int i = 1; i < 10; i++)
                            if (PID[i] < '0' || PID[i] > '9')
                                throw new InvalidDataException(message: "Invalid PID.");

                        xpSystem.AddNewUser(_command.User.Id, PID);
                        return Task.CompletedTask;
                    },
                    options = new List<SlashCommandOptionBuilder>() {
                        new SlashCommandOptionBuilder(){
                            Name = "PID",
                            Type = ApplicationCommandOptionType.String,
                            Description = "your PID",
                            IsRequired = true,
                        },
                    },
                });
                //
                await Bot.AddCommand(new Program.Command()
                {
                    name = "leave-event",
                    description = "unregisters you from the current event",
                    callback = (SocketSlashCommand _command) =>
                    {
                        xpSystem.RemoveUser(xpSystem.GetUser(_command.User.Id));
                        return Task.CompletedTask;
                    },
                    requiresConfirmation = true,
                });
                //
                await Bot.AddCommand(new Program.Command()
                {
                    name = "see-self",
                    description = "shows you your stats",
                    callback = async (SocketSlashCommand _command) =>
                    {
                        await _command.RespondAsync(embed: new EmbedBuilder().AddField("Your Data", xpSystem.GetUser(_command.User.Id).ToString()).Build(), ephemeral: true);
                    },
                });
                //
                await Bot.AddCommand(new Program.Command()
                {
                    name = "add-game",
                    description = "adds a game to your profile",
                    callback = (SocketSlashCommand _command) =>
                    {
                        //Adds a game to the caller's profile
                        return Task.CompletedTask;
                    },
                    options = new List<SlashCommandOptionBuilder>() {
                        new SlashCommandOptionBuilder(){
                            Name = "type",
                            Type = ApplicationCommandOptionType.String,
                            Description = "one of: ranked/coop/teams/party",
                            IsRequired = true,
                        },
                        new SlashCommandOptionBuilder(){
                            Name = "player-count",
                            Type = ApplicationCommandOptionType.Integer,
                            Description = "the number of players/teams in the game",
                            IsRequired = true,
                        },
                        new SlashCommandOptionBuilder(){
                            Name = "rank",
                            Type = ApplicationCommandOptionType.Integer,
                            Description = "where you/your team ranked",
                            IsRequired = true,
                        },
                        new SlashCommandOptionBuilder(){
                            Name = "time",
                            Type = ApplicationCommandOptionType.Integer,
                            Description = "game length in minutes",
                            IsRequired = true,
                        },
                    },
                });
                //
                await Bot.AddCommand(new Program.Command()
                {
                    name = "remove-game",
                    description = "removes a game from a your profile",
                    callback = (SocketSlashCommand _command) =>
                    {
                        //Removes a game from the caller's profile
                        return Task.CompletedTask;
                    },
                    requiresConfirmation = true,
                    options = new List<SlashCommandOptionBuilder>() {
                        new SlashCommandOptionBuilder(){
                            Name = "id",
                            Type = ApplicationCommandOptionType.Integer,
                            Description = "the game's id",
                            IsRequired = true,
                        },
                    },
                });
                //
                await Bot.AddCommand(new Program.Command()
                {
                    name = "add-achievement",
                    description = "",
                    callback = (SocketSlashCommand _command) =>
                    {
                        //Adds an achivement to the caller's profile
                        return Task.CompletedTask;
                    },
                    options = new List<SlashCommandOptionBuilder>() {
                        new SlashCommandOptionBuilder(){
                            Name = "id",
                            Type = ApplicationCommandOptionType.String,
                            Description = "the achievement's name",
                            IsRequired = true,
                        },
                    },
                });
                //
                await Bot.AddCommand(new Program.Command()
                {
                    name = "remove-achievement",
                    description = "removes an achievement from your profile",
                    callback = (SocketSlashCommand _command) =>
                    {
                        //Removes an achivement from the caller's profile
                        return Task.CompletedTask;
                    },
                    requiresConfirmation = true,
                    options = new List<SlashCommandOptionBuilder>() {
                        new SlashCommandOptionBuilder(){
                            Name = "id",
                            Type = ApplicationCommandOptionType.String,
                            Description = "the achievement's name",
                            IsRequired = true,
                        },
                    },
                });
                Console.WriteLine("Commands Initalized");
            });
            return Task.CompletedTask;
        }
    }
}