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
                    callback = async (SocketSlashCommand _command) =>
                    {
                        await CommandChannel().AddPermissionOverwriteAsync(Bot.Server().EveryoneRole, OverwritePermissions.DenyAll(CommandChannel()).Modify(viewChannel: PermValue.Allow, useApplicationCommands: PermValue.Allow, sendMessages: PermValue.Allow));
                        
                        xpSystem.Clear();
                        xpSystem.EventName = (string) _command.Data.Options.First().Value;
                         
                        await AnnouncementChannel().SendMessageAsync(embed: new EmbedBuilder().AddField("The Event has Started!", 
                            xpSystem.EventName).Build());

                    },
                    modOnly = true,
                    requiresConfirmation = true,
                    options = new List<SlashCommandOptionBuilder>() {
                        new SlashCommandOptionBuilder(){
                            Name = "event-name",
                            Type = ApplicationCommandOptionType.String,
                            Description = "name of the event",
                            IsRequired = true,
                        },
                    }
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
                        
                        await AnnouncementChannel().SendMessageAsync(xpSystem.DisplayTopXUsers(3));
                        await AnnouncementChannel().SendMessageAsync(embed: new EmbedBuilder().AddField("Thank you for participating in the event.", 
                            xpSystem.EventName).Build());
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
                            await AnnouncementChannel().SendMessageAsync(embed: new EmbedBuilder().AddField("Raffle Winner", xpSystem.DrawRaffle()).Build());
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
                            await AnnouncementChannel().SendMessageAsync( embed: (new EmbedBuilder().AddField("Top " + (string) _command.Data.Options.First().Value + " Users",
                                xpSystem.DisplayTopXUsers(Convert.ToInt32(_command.Data.Options.First().Value))).Build()));
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
                            string name = xpSystem.GetUser(((SocketGuildUser) _command.Data.Options.First().Value).Id).ShowGames(Convert.ToInt32(_command.Data.Options.ElementAt(1).Value));
                            
                            xpSystem.GetUser(((SocketGuildUser) _command.Data.Options.First().Value).Id)
                                .RemoveGame(Convert.ToInt32(_command.Data.Options.ElementAt(1).Value));
                                
                            await _command.FollowupAsync(embed: (new EmbedBuilder().AddField("Game Removed", 
                                xpSystem.GetUser(((SocketGuildUser)_command.Data.Options.First().Value).Id).DiscordUser.ToString() + "\n" + 
                                name)).Build(), ephemeral: true);
                                
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
                        xpSystem.GetUser(((SocketGuildUser) _command.Data.Options.First().Value).Id)
                            .UnclaimAchievement((string) (_command.Data.Options.ElementAt(1).Value));
                        //removes an achievement from a player
                         
                        await _command.FollowupAsync(embed: (new EmbedBuilder().AddField("Achievement Removed", 
                            xpSystem.GetUser(((SocketGuildUser)_command.Data.Options.First().Value).Id).DiscordUser.ToString() + "\n" + 
                            (string) _command.Data.Options.ElementAt(1).Value)).Build(), ephemeral: true);
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
                            string user = xpSystem.GetUser(((SocketGuildUser) _command.Data.Options.First().Value).Id).DiscordUser.Username;
                            
                            xpSystem.RemoveUser(xpSystem.GetUser(((SocketGuildUser) _command.Data.Options.First().Value).Id));
                        //removes a player from the event
                            await _command.FollowupAsync(embed: (new EmbedBuilder().AddField("User Removed", 
                                user)).Build(), ephemeral: true);
                        }
                        catch{
                            await _command.FollowupAsync(embed: (new EmbedBuilder().AddField("User Not Found", 
                                ((SocketGuildUser)_command.Data.Options.First().Value).Username)).Build(), ephemeral: true);
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
                            
                            await _command.RespondAsync(embed: new EmbedBuilder().AddField("Added to Event\nYour Data", xpSystem.GetUser(_command.User.Id).ToString()).Build(), ephemeral: true);
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
                            String name = xpSystem.GetUser(_command.User.Id).DiscordUser.Username;
                            xpSystem.RemoveUser(xpSystem.GetUser(_command.User.Id));
                            await _command.RespondAsync(embed: new EmbedBuilder().AddField("Successfully left event", name).Build(), ephemeral: true);
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
                                
                            await _command.RespondAsync(embed: new EmbedBuilder().AddField("Added game to profile", 
                                user.ShowGames(Convert.ToInt32(user.NumberGamesPlayed-1))).Build(), ephemeral: true);
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
                            
                            
                        }
                            .AddChoice("Ranked", "ranked")
                            .AddChoice("Co-op", "coop")
                            .AddChoice("Teams", "teams")
                            .AddChoice("Party", "party")
                        ,
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
                            string name = xpSystem.GetUser(_command.User.Id).ShowGames(Convert.ToInt32(_command.Data.Options.First().Value));
                            xpSystem.GetUser(_command.User.Id).RemoveGame(Convert.ToInt32(_command.Data.Options.First().Value));
                            //Removes a game from the caller's profile
                            await _command.FollowupAsync(embed: new EmbedBuilder().AddField("Removed Game From List", 
                                name).Build(), ephemeral: true);
                            //return Task.CompletedTask;
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
                    name = "add-achievement",
                    description = "adds an achievement to your profile",
                    callback = async (SocketSlashCommand _command) =>
                    {
                        try
                        {
                           xpSystem.GetUser(_command.User.Id).ClaimAchievement((string) _command.Data.Options.First().Value); 
                            await _command.RespondAsync(embed: new EmbedBuilder().AddField("Added achievement to profile", 
                                xpSystem.GetUser(_command.User.Id).ShowAchievements(id: (string) _command.Data.Options.First().Value)).Build(), ephemeral: true);
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
                            await _command.FollowupAsync(embed: new EmbedBuilder().AddField("Achievement Removed", 
                            _command.Data.Options.First().Value).Build(), ephemeral: true);
                        
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
                    name = "show-achievements",
                    description = "shows achievements you have completed or all available achievements",
                    callback = async (SocketSlashCommand _command) =>
                    {
                        try
                        {   
                            await _command.RespondAsync(embed: new EmbedBuilder().AddField("Your Achievements", 
                                xpSystem.GetUser(_command.User.Id).ShowAchievements(showAll: (Boolean) _command.Data.Options.First().Value)).Build(), ephemeral: true);
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
