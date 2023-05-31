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
                        await _command.RespondAsync(embed: (new EmbedBuilder().AddField("Player Data", xpSystem.GetUser(((SocketUser)_command.Data.Options.First().Value).Id).ToString())).Build(), ephemeral: true);
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
                            int numberOfUsers = Convert.ToInt32(_command.Data.Options.First().Value);
                            
                            await AnnouncementChannel().SendMessageAsync(embed: (new EmbedBuilder().AddField($"Top {numberOfUsers} Users",
                                xpSystem.DisplayTopXUsers(numberOfUsers)).Build()));
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
                            int gameId = Convert.ToInt32(_command.Data.Options.ElementAt(1).Value);
                            XpStorage.User user = xpSystem.GetUser(((SocketUser) _command.Data.Options.First().Value).Id);
                            
                            string gameName = user.ShowGames(gameId);
                            user.RemoveGame(gameId);
                                
                            await _command.FollowupAsync(embed: (new EmbedBuilder().AddField("Game Removed", 
                                user.DiscordUser.ToString() + "\n" + 
                                gameName)).Build(), ephemeral: true);
                                
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
                        string achievementName = (string) _command.Data.Options.ElementAt(1).Value;
                        XpStorage.User user = xpSystem.GetUser(((SocketUser) _command.Data.Options.First().Value).Id);
                            
                        
                        user.UnclaimAchievement(achievementName);
                        //removes an achievement from a player
                         
                        await _command.FollowupAsync(embed: (new EmbedBuilder().AddField("Achievement Removed", 
                            user.DiscordUser.ToString() + "\n" + achievementName)).Build(), ephemeral: true);
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
                            Name = "name",
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
                            XpStorage.User user = xpSystem.GetUser(((SocketUser) _command.Data.Options.First().Value).Id);
                            
                            xpSystem.RemoveUser(user);
                        //removes a player from the event
                            await _command.FollowupAsync(embed: (new EmbedBuilder().AddField("User Removed", 
                                user.DiscordUser.ToString())).Build(), ephemeral: true);
                        }
                        catch{
                            await _command.FollowupAsync(embed: (new EmbedBuilder().AddField("User Not Found", 
                                ((SocketUser)_command.Data.Options.First().Value).Username)).Build(), ephemeral: true);
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
                            
                            String name = xpSystem.GetUser(_command.User.Id).DiscordUser.ToString();
                            
                            xpSystem.RemoveUser(xpSystem.GetUser(_command.User.Id));
                            await _command.FollowupAsync(embed: new EmbedBuilder().AddField("Successfully left event", name).Build(), ephemeral: true);
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
                            XpStorage.User user = xpSystem.GetUser(_command.User.Id);
                            string gameName = (string) _command.Data.Options.First().Value;
                            uint playerCount = Convert.ToUInt32(_command.Data.Options.ElementAt(1).Value);
                            GameType type = (_command.Data.Options.ElementAt(2).Value as string) switch {
                                "ranked" => GameType.Ranked, "coop" => GameType.CoOp, 
                                "teams" => GameType.Teams, "party" => GameType.Party, _ =>
                                throw new InvalidDataException(message: "Invalid Game Type.")
                            };
                            uint rank = Convert.ToUInt32(_command.Data.Options.ElementAt(3).Value);
                            uint time = Convert.ToUInt32(_command.Data.Options.ElementAt(4).Value);
                            
                            user.AddGame(gameName, playerCount, type, rank, time);
                                
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
                            XpStorage.User user = xpSystem.GetUser(_command.User.Id);
                            int id = Convert.ToInt32(_command.Data.Options.First().Value);
                            string gameName = user.ShowGames(id);
                           
                            user.RemoveGame(id);
                            //Removes a game from the caller's profile
                            await _command.FollowupAsync(embed: new EmbedBuilder().AddField("Removed Game From List", 
                                gameName).Build(), ephemeral: true);
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
                    //Adds an achivement to the caller's profile
                    name = "add-achievement",
                    description = "adds an achievement to your profile",
                    callback = async (SocketSlashCommand _command) =>
                    {
                        try
                        {
                            string achievementName = (string) _command.Data.Options.First().Value;
                            XpStorage.User user = xpSystem.GetUser(_command.User.Id);
                            
                            
                            user.ClaimAchievement(achievementName); 
                            await _command.RespondAsync(embed: new EmbedBuilder().AddField("Added achievement to profile", 
                                user.ShowAchievements(name: achievementName)).Build(), ephemeral: true);
                        }
                        catch{
                            throw;
                        }
                        
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
                    //Unclaims a user's achievement and take away the respective points from the user
                    name = "remove-achievement",
                    description = "removes an achievement from your profile",
                    callback = async (SocketSlashCommand _command) =>
                    {
                        try
                        {
                            string achievementName = (string) _command.Data.Options.First().Value;
                            XpStorage.User user = xpSystem.GetUser(_command.User.Id);
                            
                            user.UnclaimAchievement(achievementName);
                            //Removes an achivement from the caller's profile
                            await _command.FollowupAsync(embed: new EmbedBuilder().AddField("Achievement Removed", 
                                achievementName).Build(), ephemeral: true);
                        
                        }
                        catch
                        {
                            throw;
                        }
                    
                    },
                    requiresConfirmation = true,
                    options = new List<SlashCommandOptionBuilder>() {
                        new SlashCommandOptionBuilder(){
                            Name = "name",
                            Type = ApplicationCommandOptionType.String,
                            Description = "the achievement's name",
                            IsRequired = true,
                        },
                    },
                });
                
                await Bot.AddCommand(new Program.Command()
                {
                    //displays either all achievements or user's completed achievements 
                    name = "show-achievements",
                    description = "shows achievements you have completed or all available achievements",
                    callback = async (SocketSlashCommand _command) =>
                    {
                        try
                        {      
                            bool isShowAll = false;
                            string? achievementName = null;
                            XpStorage.User user = xpSystem.GetUser(_command.User.Id); 

                            foreach (SocketSlashCommandDataOption? option in _command.Data.Options)
                            {
                                if (option is null)
                                {
                                    continue;
                                }
                                switch (option.Name)
                                {
                                    //true: will show all possible achievements | false: will show completed achievements
                                    case "show-all":
                                        isShowAll = (bool)option.Value;
                                        break;
                                    //if name is specified and show-all is false, then will show a specified achievement
                                    case "name":
                                        achievementName = (string)option.Value;
                                        break;
                                }
                            }
                            await _command.RespondAsync(embed: new EmbedBuilder().AddField("Your Achievements", 
                                user.ShowAchievements(showAll: isShowAll, name: achievementName)).Build(), ephemeral: true);
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
                        new SlashCommandOptionBuilder(){
                          Name = "name",
                          Type = ApplicationCommandOptionType.String,
                          Description = "name of achievement",  
                        },
                    },
                    
                    
                });
                
                Console.WriteLine("Commands Initalized");
            });
            return Task.CompletedTask;
        }
    }
}
