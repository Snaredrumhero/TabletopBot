using Discord;
using Discord.WebSocket;

namespace TableTopBot
{
    internal class XPModule : Module
    {
        public SocketTextChannel AnnouncementChannel() => Bot.Server().GetTextChannel(1108244408027066468);
        public SocketTextChannel CommandChannel() => Bot.Server().GetTextChannel(1108244408027066468);

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
                    callback = async (SocketSlashCommand _command) =>
                    {
                        await CommandChannel().AddPermissionOverwriteAsync(Bot.Server().EveryoneRole, OverwritePermissions.DenyAll(CommandChannel()).Modify(viewChannel: PermValue.Allow, useApplicationCommands: PermValue.Allow, sendMessages: PermValue.Allow));
                        xpSystem.Clear();
                        //return Task.CompletedTask;
                        // await _command.RespondAsync(embed: new EmbedBuilder().AddField("Starting", 
                        // "Game").Build(), ephemeral: true);
                    },
                    modOnly = true,
                    requiresConfirmation = true,
                });
                //
                await Bot.AddCommand(new Program.Command()
                {
                    name = "end",
                    description = "ends the all-day event.",
                    callback = async (SocketSlashCommand _command) =>
                    {
                        //displays the top 3 users to the all-day announcements channel for prizes
                        //could display overall statistics for the all-day as well
                        await CommandChannel().AddPermissionOverwriteAsync(Bot.Server().EveryoneRole, OverwritePermissions.DenyAll(CommandChannel()));
                        //return Task.CompletedTask;
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
                    {   try{
                            await AnnouncementChannel().SendMessageAsync(xpSystem.DrawRaffle());
                        }
                        catch
                        { 
                            throw;
                        }
                    
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
                    callback = async (SocketSlashCommand _command) =>
                    {
                        try
                        {
                            await AnnouncementChannel().SendMessageAsync(xpSystem.DisplayTopXUsers(Convert.ToInt32(_command.Data.Options.First().Value)));
                        //shows the entire profile of the top x users
                        }
                        catch
                        {
                            throw;
                        }
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
                    callback = async (SocketSlashCommand _command) =>
                    {
                        try
                        {    
                        //removes a game from a player
                            
                            xpSystem.GetUser(((SocketUser) _command.Data.Options.First().Value).Id)
                            .RemoveGame(Convert.ToInt32(_command.Data.Options.ElementAt(1).Value));
                            await Task.CompletedTask;
                            //return Task.CompletedTask;
                        }
                        catch
                        {
                            throw;
                        }
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
                    callback = async (SocketSlashCommand _command) =>
                    {
                        //removes an achievement from a player
                        await Task.CompletedTask;
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
                    callback = async (SocketSlashCommand _command) =>
                    {
                        try{
                            xpSystem.RemoveUser(xpSystem.GetUser(((SocketUser) _command.Data.Options.First().Value).Id));
                        //removes a player from the event
                            await Task.CompletedTask;
                        }
                        catch{
                            throw;
                        }
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
                    callback = async (SocketSlashCommand _command) =>
                    {
                        
                        try{
                            
                            string PID = ((string)_command.Data.Options.First().Value).ToUpper();
                            if (PID[0] != 'P' || PID.Length != 10 || !int.TryParse(PID[1..9],out int value))
                                throw new InvalidDataException(message: "Invalid PID.");
                            for (int i = 1; i < 10; i++)
                                if (PID[i] < '0' || PID[i] > '9')
                                    throw new InvalidDataException(message: "Invalid PID.");
                            
                            xpSystem.AddNewUser(_command.User, PID);
                            await Task.CompletedTask;
                        }
                        catch{
                            throw;
                        }
                    },
                    options = new List<SlashCommandOptionBuilder>() {
                        new SlashCommandOptionBuilder(){
                            Name = "pid",
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
                    callback = async (SocketSlashCommand _command) =>
                    {
                        try
                        {
                            xpSystem.RemoveUser(xpSystem.GetUser(_command.User.Id));
                            await Task.CompletedTask;
                        }
                        catch
                        {
                            throw;
                        }
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
                        try
                        {
                            await _command.RespondAsync(embed: new EmbedBuilder().AddField("Your Data", xpSystem.GetUser(_command.User.Id).ToString()).Build(), ephemeral: true);
                        }
                        catch
                        {
                            throw;
                        }
                    },
                });
                //
                await Bot.AddCommand(new Program.Command()
                {
                    //Adds a game to the caller's profile
                    name = "add-game",
                    description = "adds a game to your profile",
                    callback = async (SocketSlashCommand _command) =>
                    {
                        try
                        {
                            Index game_index = 0;
                            Index player_index = 1;
                            Index type_index = 2;
                            Index rank_index = 3;
                            Index length_index = 4;
                            GameType game_type;
                            
                            switch( ((string)_command.Data.Options.ElementAt(type_index).Value).ToLower()){
                                case "ranked":
                                    game_type = GameType.Ranked;
                                    break;
                                case "coop":
                                    game_type = GameType.CoOp;
                                    break;
                                case "teams":
                                    game_type = GameType.Teams;
                                    break;
                                case "party":
                                    game_type = GameType.Party;
                                    break;
                                default:
                                    throw new InvalidDataException(message: "Invalid Game Type.");
                                    
                            }
                            XpStorage.User user = xpSystem.GetUser(_command.User.Id);
                            user.AddGame(
                                _command.Data.Options.ElementAt(game_index).Value.ToString()!,
                                Convert.ToUInt32(_command.Data.Options.ElementAt(player_index).Value),
                                game_type,
                                Convert.ToUInt32(_command.Data.Options.ElementAt(rank_index).Value), 
                                Convert.ToUInt32(_command.Data.Options.ElementAt(length_index).Value));
                                
                            await _command.RespondAsync(embed: new EmbedBuilder().AddField("Your Games", 
                                user.ShowGames(Convert.ToInt32(user.NumberGamesPlayed-1))).Build(), ephemeral: true);
                            //return Task.CompletedTask;
                        }
                        catch
                        {
                            throw;
                        }
                    },
                    options = new List<SlashCommandOptionBuilder>() {
                        new SlashCommandOptionBuilder(){
                            Name = "name",
                            Type = ApplicationCommandOptionType.String,
                            Description = "the name of the game played",
                            IsRequired = true,
                        },
                        new SlashCommandOptionBuilder(){
                            Name = "player-count",
                            Type = ApplicationCommandOptionType.Integer,
                            Description = "the number of players/teams in the game",
                            IsRequired = true,
                        },
                        new SlashCommandOptionBuilder(){
                            Name = "type",
                            Type = ApplicationCommandOptionType.String,
                            Description = "one of: ranked/coop/teams/party",
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
                    description = "removes a game from your profile",
                    callback = async (SocketSlashCommand _command) =>
                    {
                        try
                        {
                            xpSystem.GetUser(_command.User.Id).RemoveGame(Convert.ToInt32(_command.Data.Options.First().Value));
                            //Removes a game from the caller's profile
                            await Task.CompletedTask;
                        }
                        catch
                        {
                            throw;
                        }
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
                    description = "adds an achievement to your profile",
                    callback = async (SocketSlashCommand _command) =>
                    {
                        try
                        {
                           xpSystem.GetUser(_command.User.Id).ClaimAchievement((string) _command.Data.Options.First().Value); 
                            await Task.CompletedTask;
                        }
                        catch{
                            throw;
                        }
                        //Adds an achivement to the caller's profile
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
                    callback = async (SocketSlashCommand _command) =>
                    {
                        try
                        {
                            xpSystem.GetUser(_command.User.Id).UnclaimAchievement((string) _command.Data.Options.First().Value);
                            //Removes an achivement from the caller's profile
                            await Task.CompletedTask;
                        
                        }
                        catch
                        {
                            throw;
                        }
                    
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
                
                await Bot.AddCommand(new Program.Command()
                {
                    name = "show-games",
                    description = "shows your completed games",
                    callback = async (SocketSlashCommand _command) =>
                    {
                        try
                        {
                            await _command.RespondAsync(embed: new EmbedBuilder().AddField("Your Games", 
                                xpSystem.GetUser(_command.User.Id).ShowGames()).Build(), ephemeral: true);
                        
                        }
                        catch
                        {
                            throw;
                        }
                    },
                    
                });
                await Bot.AddCommand(new Program.Command()
                {
                    name = "show-achievements",
                    description = "shows achievements you have completed or all available achievements",
                    callback = async (SocketSlashCommand _command) =>
                    {
                        try
                        {
                            await _command.RespondAsync(embed: new EmbedBuilder().AddField("Your Games", 
                            xpSystem.GetUser(_command.User.Id).ShowAchievements((Boolean) _command.Data.Options.First().Value)).Build(), ephemeral: true);
                        }
                        catch
                        {
                            throw;
                        }
                    },
                    options = new List<SlashCommandOptionBuilder>() {
                        new SlashCommandOptionBuilder(){
                            Name = "show-all",
                            Type = ApplicationCommandOptionType.Boolean,
                            Description = "True: shows all achievements | False: shows your completed achievements",
                        },
                    },
                });
                
                Console.WriteLine("Commands Initalized");
            });
            return Task.CompletedTask;
        }
    }
}
