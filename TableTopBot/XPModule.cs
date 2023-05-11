using Discord;

namespace TableTopBot
{
    internal class XPModule : Module
    {
        public XPModule(Program _bot) : base(_bot) { }

        public override void InitilizeModule()
        {
            Bot.AddConnectedCallback(() =>
            {
                //officer only
                //start
                SlashCommandBuilder command = new SlashCommandBuilder()
                {
                    Name = "start",
                    Description = "starts the all-day event.",
                    DefaultMemberPermissions = GuildPermission.KickMembers,
                };
                Bot.AddGuildCommand(command);
                //end
                command = new SlashCommandBuilder()
                {
                    Name = "end",
                    Description = "ends the all-day event.",
                    DefaultMemberPermissions = GuildPermission.KickMembers,
                };
                Bot.AddGuildCommand(command);
                //draw raffle
                command = new SlashCommandBuilder()
                {
                    Name = "draw-raffle",
                    Description = "draws a raffle ticket",
                    DefaultMemberPermissions = GuildPermission.KickMembers,
                };
                Bot.AddGuildCommand(command);
                //see player
                command = new SlashCommandBuilder()
                {
                    Name = "see-player",
                    Description = "view a player's profile",
                    DefaultMemberPermissions = GuildPermission.KickMembers,
                    Options = { 
                        new SlashCommandOptionBuilder(){
                            Name = "player",
                            Type = ApplicationCommandOptionType.User,
                            Description = "the user to see",
                            IsRequired = true,
                        },
                    },
                };
                Bot.AddGuildCommand(command);
                //show top x users
                command = new SlashCommandBuilder()
                {
                    Name = "showXusers",
                    Description = "shows a leaderboard",
                    DefaultMemberPermissions = GuildPermission.KickMembers,
                    Options = { 
                        new SlashCommandOptionBuilder(){
                            Name = "x",
                            Type = ApplicationCommandOptionType.Integer,
                            Description = "the number of users to see",
                            IsRequired = true,
                        },
                    },
                };
                Bot.AddGuildCommand(command);
                //remove player game
                command = new SlashCommandBuilder()
                {
                    Name = "remove-player-game",
                    Description = "removes a game from a player's profile",
                    DefaultMemberPermissions = GuildPermission.KickMembers,
                    Options = {
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
                };
                Bot.AddGuildCommand(command);
                //remove player achievement
                command = new SlashCommandBuilder()
                {
                    Name = "remove-player-achievement",
                    Description = "removes an achievement from a player's profile",
                    DefaultMemberPermissions = GuildPermission.KickMembers,
                    Options = {
                        new SlashCommandOptionBuilder(){
                            Name = "player",
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
                };
                Bot.AddGuildCommand(command);
                //remover user
                command = new SlashCommandBuilder()
                {
                    Name = "remove-player",
                    Description = "removes a player's profile",
                    DefaultMemberPermissions = GuildPermission.KickMembers,
                    Options = {
                        new SlashCommandOptionBuilder(){
                            Name = "player",
                            Type = ApplicationCommandOptionType.User,
                            Description = "the user to remove",
                            IsRequired = true,
                        },
                    },
                };
                Bot.AddGuildCommand(command);

                //anyone
                //init user
                command = new SlashCommandBuilder()
                {
                    Name = "join-event",
                    Description = "registers you for the current event",
                };
                //withdraw
                command = new SlashCommandBuilder()
                {
                    Name = "leave-event",
                    Description = "unregisters you from the current event",
                };
                //see self
                command = new SlashCommandBuilder()
                {
                    Name = "see-self",
                    Description = "shows you your stats",
                };
                //add game
                command = new SlashCommandBuilder()
                {
                    Name = "add-game",
                    Description = "adds a game to your profile",
                    Options = {
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
                };
                //remove game
                command = new SlashCommandBuilder()
                {
                    Name = "remove-game",
                    Description = "removes a game from a your profile",
                    Options = {
                        new SlashCommandOptionBuilder(){
                            Name = "id",
                            Type = ApplicationCommandOptionType.Integer,
                            Description = "the game's id",
                            IsRequired = true,
                        },
                    },
                };
                //add achivement
                command = new SlashCommandBuilder()
                {
                    Name = "add-achievement",
                    Description = "adds an achievement to your profile",
                    Options = {
                        new SlashCommandOptionBuilder(){
                            Name = "id",
                            Type = ApplicationCommandOptionType.String,
                            Description = "the achievement's name",
                            IsRequired = true,
                        },
                    },
                };
                //remove achievement
                command = new SlashCommandBuilder()
                {
                    Name = "remove-achievement",
                    Description = "removes an achievement from your profile",
                    Options = {
                        new SlashCommandOptionBuilder(){
                            Name = "id",
                            Type = ApplicationCommandOptionType.String,
                            Description = "the achievement's name",
                            IsRequired = true,
                        },
                    },
                };
                return Task.CompletedTask;
            });
        }

        //Listeners
    }
}