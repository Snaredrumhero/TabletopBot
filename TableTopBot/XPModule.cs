using Discord;
using Discord.WebSocket;

namespace TableTopBot
{
    internal class XPModule : Module
    {
        private XpStorage xpSystem = new XpStorage();
        public XPModule(Program _bot) : base(_bot) { }

        public override void InitilizeModule()
        {
            Bot.AddConnectedCallback(async () =>
            {
                //officer only
                //start
                SlashCommandBuilder command = new SlashCommandBuilder()
                {
                    Name = "start",
                    Description = "starts the all-day event.",
                    DefaultMemberPermissions = GuildPermission.KickMembers,
                };
                await Bot.AddGuildCommand(command);
                //end
                command = new SlashCommandBuilder()
                {
                    Name = "end",
                    Description = "ends the all-day event.",
                    DefaultMemberPermissions = GuildPermission.KickMembers,
                };
                await Bot.AddGuildCommand(command);
                //draw raffle
                command = new SlashCommandBuilder()
                {
                    Name = "draw-raffle",
                    Description = "draws a raffle ticket",
                    DefaultMemberPermissions = GuildPermission.KickMembers,
                };
                await Bot.AddGuildCommand(command);
                //see player
                command = new SlashCommandBuilder()
                {
                    Name = "see-player",
                    Description = "view a player's profile",
                    DefaultMemberPermissions = GuildPermission.KickMembers,
                    Options = new List<SlashCommandOptionBuilder>() { 
                        new SlashCommandOptionBuilder(){
                            Name = "player",
                            Type = ApplicationCommandOptionType.User,
                            Description = "the user to see",
                            IsRequired = true,
                        },
                    },
                };
                await Bot.AddGuildCommand(command);
                //show top x users
                command = new SlashCommandBuilder()
                {
                    Name = "show-x-users",
                    Description = "shows a leaderboard",
                    DefaultMemberPermissions = GuildPermission.KickMembers,
                    Options = new List<SlashCommandOptionBuilder>() { 
                        new SlashCommandOptionBuilder(){
                            Name = "x",
                            Type = ApplicationCommandOptionType.Integer,
                            Description = "the number of users to see",
                            IsRequired = true,
                        },
                    },
                };
                await Bot.AddGuildCommand(command);
                //remove player game
                command = new SlashCommandBuilder()
                {
                    Name = "remove-player-game",
                    Description = "removes a game from a player's profile",
                    DefaultMemberPermissions = GuildPermission.KickMembers,
                    Options = new List<SlashCommandOptionBuilder>() {
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
                await Bot.AddGuildCommand(command);
                //remove player achievement
                command = new SlashCommandBuilder()
                {
                    Name = "remove-player-achievement",
                    Description = "removes an achievement from a player's profile",
                    DefaultMemberPermissions = GuildPermission.KickMembers,
                    Options = new List<SlashCommandOptionBuilder>() {
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
                await Bot.AddGuildCommand(command);
                //remove user
                command = new SlashCommandBuilder()
                {
                    Name = "remove-player",
                    Description = "removes a player's profile",
                    DefaultMemberPermissions = GuildPermission.KickMembers,
                    Options = new List<SlashCommandOptionBuilder>(){
                        new SlashCommandOptionBuilder(){
                            Name = "player",
                            Type = ApplicationCommandOptionType.User,
                            Description = "the user to remove",
                            IsRequired = true,
                        },
                    },
                };
                await Bot.AddGuildCommand(command);

                //anyone
                //init user
                command = new SlashCommandBuilder()
                {
                    Name = "join-event",
                    Description = "registers you for the current event",
                    Options = new List<SlashCommandOptionBuilder>() {
                        new SlashCommandOptionBuilder(){
                            Name = "PID",
                            Type = ApplicationCommandOptionType.String,
                            Description = "your PID",
                            IsRequired = true,
                        },
                    },
                };
                await Bot.AddGuildCommand(command);
                //withdraw
                command = new SlashCommandBuilder()
                {
                    Name = "leave-event",
                    Description = "unregisters you from the current event",
                };
                await Bot.AddGuildCommand(command);
                //see self
                command = new SlashCommandBuilder()
                {
                    Name = "see-self",
                    Description = "shows you your stats",
                };
                await Bot.AddGuildCommand(command);
                //add game
                command = new SlashCommandBuilder()
                {
                    Name = "add-game",
                    Description = "adds a game to your profile",
                    Options = new List<SlashCommandOptionBuilder>() {
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
                await Bot.AddGuildCommand(command);
                //remove game
                command = new SlashCommandBuilder()
                {
                    Name = "remove-game",
                    Description = "removes a game from a your profile",
                    Options = new List<SlashCommandOptionBuilder>() {
                        new SlashCommandOptionBuilder(){
                            Name = "id",
                            Type = ApplicationCommandOptionType.Integer,
                            Description = "the game's id",
                            IsRequired = true,
                        },
                    },
                };
                await Bot.AddGuildCommand(command);
                //add achivement
                command = new SlashCommandBuilder()
                {
                    Name = "add-achievement",
                    Description = "adds an achievement to your profile",
                    Options = new List<SlashCommandOptionBuilder>() {
                        new SlashCommandOptionBuilder(){
                            Name = "id",
                            Type = ApplicationCommandOptionType.String,
                            Description = "the achievement's name",
                            IsRequired = true,
                        },
                    },
                };
                await Bot.AddGuildCommand(command);
                //remove achievement
                command = new SlashCommandBuilder()
                {
                    Name = "remove-achievement",
                    Description = "removes an achievement from your profile",
                    Options = new List<SlashCommandOptionBuilder>() {
                        new SlashCommandOptionBuilder(){
                            Name = "id",
                            Type = ApplicationCommandOptionType.String,
                            Description = "the achievement's name",
                            IsRequired = true,
                        },
                    },
                };
                await Bot.AddGuildCommand(command);
                Console.WriteLine("Commands Initalized");
            });
            Bot.AddSlashCommandExecutedCallback(SlashCallbacks);
            Bot.AddButtonExecutedCallback(ButtonListener);
        }

        //Listeners
        public async Task SlashCallbacks(SocketSlashCommand _command)
        {
            //most commands should have ephemerial responses
            //add logging to all commands
            List<SocketSlashCommandDataOption> options = _command.Data.Options.ToList();
            EmbedBuilder embed = new EmbedBuilder();
            switch (_command.CommandName)
            {
                case "start":
                    await GetConfirmation(_command, () =>
                    {
                        //opens the all day command channel to all
                        xpSystem.Clear();
                        return Task.CompletedTask;
                    });
                    break;
                case "end":
                    await GetConfirmation(_command, () =>
                    {
                        //displays the top 3 users to the all-day announcements channel for prizes
                        //could display overall statistics for the all-day as well
                        //closes the all day command channel from all
                        return Task.CompletedTask;
                    });
                    break;
                case "draw-raffle":
                    await GetConfirmation(_command, async () =>
                    {
                        await Bot.AnnouncementChannel().SendMessageAsync(xpSystem.DrawRaffle());
                    });
                    break;
                case "see-player":
                    embed.AddField("Player Data", xpSystem.GetUser(((SocketGuildUser)options[0].Value).Id).ToString());
                    await _command.RespondAsync(embed: embed.Build(), ephemeral: true);
                    break;
                case "show-x-users":
                    //shows the entire profile of the top x users
                    break;
                case "remove-player-game":
                    await GetConfirmation(_command, () =>
                    {
                        //removes a game from a player
                        return Task.CompletedTask;
                    });
                    break;
                case "remove-player-achievement":
                    await GetConfirmation(_command, () =>
                    {
                        //removes an achievement from a player
                        return Task.CompletedTask;
                    });
                    break;
                case "remove-player":
                    await GetConfirmation(_command, () =>
                    {
                        //removes a player from the event
                        return Task.CompletedTask;
                    });
                    break;
                case "join-event":
                    string PID = (string)options[0].Value;
                    if (PID[0] != 'P' || PID.Length != 10) //Check if the last 9 are numbers
                        throw new InvalidDataException(message: "Invalid PID.");
                    xpSystem.AddNewUser(_command.User.Id, PID);
                    break;
                case "leave-event":
                    await GetConfirmation(_command, () => {
                        xpSystem.RemoveUser(xpSystem.GetUser(_command.User.Id));
                        return Task.CompletedTask;
                    });
                    break;
                case "see-self":
                    embed.AddField("Your Data", xpSystem.GetUser(_command.User.Id).ToString());
                    await _command.RespondAsync(embed: embed.Build(), ephemeral: true);
                    break;
                case "add-game":
                    //Adds a game to the caller's profile
                    break;
                case "remove-game":
                    await GetConfirmation(_command, () =>
                    {
                        //Removes a game from the caller's profile
                        return Task.CompletedTask;
                    });
                    break;
                case "add-achievement":
                    //Adds an achivement to the caller's profile
                    break;
                case "remove-achievement":
                    await GetConfirmation(_command, () =>
                    {
                        //Removes an achivement from the caller's profile
                        return Task.CompletedTask;
                    });
                    break;
                default:
                    throw new MissingMethodException(message: $"No definition for commad: {_command.CommandName}");
            }
        }

        //Confirmation stuff
        private Dictionary<string, Func<Task>> Buttons = new Dictionary<string, Func<Task>>();
        static ulong buttonsCreated = 0;
        private async Task GetConfirmation(SocketSlashCommand _command, Func<Task> _task)
        {
            ComponentBuilder cb = new ComponentBuilder();
            cb.WithButton("Confirm", buttonsCreated.ToString(), ButtonStyle.Danger);
            Buttons.Add(buttonsCreated.ToString(), _task);
            buttonsCreated++;
            await _command.RespondAsync(ephemeral: true, components: cb.Build());
        }

        public async Task ButtonListener(SocketMessageComponent _button)
        {
            if (Buttons.ContainsKey(_button.Data.CustomId))
            {
                await _button.DeferAsync();
                await _button.DeleteOriginalResponseAsync();
                await Buttons[_button.Data.CustomId]();
                Buttons.Remove(_button.Data.CustomId);
            }
        }
    }
}