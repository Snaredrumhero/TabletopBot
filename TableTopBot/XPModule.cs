using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using System.Text.Json;

namespace TableTopBot
{
    internal class XPModule : Module
    {
        ///Sub Classes
        //should be saved every time someone adds/removes a game/achievement, when someone enters/drops, when a raffle is drawn
        //*to do* Kyle do better documentation of this class
        class XpStorage
        {
            ///Sub Classes
            public class User
            {
                ///Variables
                public SocketUser DiscordUser;
                public string Pid;
                private readonly List<Game> _gamesPlayed = new List<Game>();
                public bool IsRaffleWinner = false;
                private readonly List<Achievement> _allAchievements = new List<Achievement>();//DefaultAchievements.Select(achievement => achievement).ToList();
                public List<Achievement> Achievements => _allAchievements.Where(achievement => achievement.IsClaimed).ToList();
                public uint NumberGamesPlayed = 0; ///only used for tracking game ids

                ///Operators
                public static bool operator >(User a, User b) => a.TotalPoints() > b.TotalPoints();
                public static bool operator <(User a, User b) => a.TotalPoints() < b.TotalPoints();

                ///Constructor
                public User(SocketUser user, string pid)
                {
                    DiscordUser = user;
                    Pid = pid;
                }

                ///Mutators
                ///Rank = 1 for win 2 for loss
                public void AddGame(string name, uint playerCount, GameType type, uint rank, uint length)
                {
                    _gamesPlayed.Add(new Game(name, NumberGamesPlayed, type, playerCount, rank, length));
                    ++NumberGamesPlayed;
                }
                public void RemoveGame(int id) => _gamesPlayed.RemoveAll(game => game.Id == id);
                public void ClaimAchievement(string achievementName)
                {
                    Achievement achievement = _allAchievements.FirstOrDefault(achievement => achievement.Name == achievementName) ?? throw new ArgumentException("Achievement Not Found");
                    achievement.IsClaimed = true;
                }
                public void UnclaimAchievement(string achievementName)
                {
                    Achievement achievement = _allAchievements.FirstOrDefault(achievement => achievement.Name == achievementName) ?? throw new ArgumentException("Achievement Not Found");
                    achievement.IsClaimed = false;
                }

                ///Accessors
                public int TotalPoints() { return _gamesPlayed.Select(game => game.ComputeXp()).Sum() + Achievements.Select(achievement => achievement.XpValue).Sum(); }
                public override string ToString() { return $"{DiscordUser.Username}\nPID: {Pid}\nPoints: {TotalPoints()}\nClaimed Raffle: {IsRaffleWinner}"; }
                public List<string> ShowGames(int list_games = Int32.MinValue)
                {
                    List<string> embedlist = new List<string>();
                    string gamelist = "";

                    if (list_games == Int32.MinValue)
                    {
                        for (int i = 0; i < _gamesPlayed.Count(); ++i)
                        {
                            //gamelist += (_gamesPlayed[i].GameAttributes() + "\n\n");
                            gamelist += (_gamesPlayed[i].GameAttributes() + "\n\n");
                            if ((i + 1) % 5 == 0)
                            {
                                embedlist.Add(gamelist);
                                gamelist = "";
                            }
                        }
                        if (!string.IsNullOrEmpty(gamelist))
                        {
                            embedlist.Add(gamelist);
                        }
                    }
                    else
                    {
                        gamelist += _gamesPlayed.FirstOrDefault(game => game.Id == list_games)!.GameAttributes();
                        embedlist.Add(gamelist);
                    }
                    //return gamelist;
                    return embedlist;
                }
                public List<string> ShowAchievements(bool showAll = false, string? name = null)
                {
                    List<string> embedlist = new List<string>();
                    string achievementlist = "";

                    if (showAll)
                    {
                        for (int i = 0; i < _allAchievements.Count; ++i)
                        {
                            achievementlist += (_allAchievements[i].AchievementAttributes() + "\n\n");
                            if ((i + 1) % 5 == 0)
                            {
                                embedlist.Add(achievementlist);
                                achievementlist = "";
                            }
                        }
                        if (!string.IsNullOrEmpty(achievementlist))
                        {
                            embedlist.Add(achievementlist);
                        }

                    }
                    else if (string.IsNullOrEmpty(name))
                    {
                        for (int i = 0; i < Achievements.Count; ++i)
                        {
                            achievementlist += (Achievements[i].AchievementAttributes() + "\n\n");
                            if ((i + 1) % 5 == 0)
                            {
                                embedlist.Add(achievementlist);
                                achievementlist = "";
                            }
                        }
                        if (!string.IsNullOrEmpty(achievementlist))
                        {
                            embedlist.Add(achievementlist);
                        }
                    }
                    else
                    {
                        achievementlist += Achievements.FirstOrDefault(achievement => achievement.Name == name)!.AchievementAttributes() ?? throw new ArgumentException("Achievement Not Found");
                        embedlist.Add(achievementlist);
                    }
                    return embedlist;
                }
            }
            public class Game
            {
                ///Variables
                public readonly string Name;
                public readonly uint Id;
                public readonly GameType Type;
                public readonly uint PlayerCount;
                public readonly uint Rank;
                public readonly uint GameLengthInMinutes;
                private const double PointsScale = 1.5;
                private const double XtraScale = PointsScale / 0.5;
                private const int RankedPositions = 5;

                public Game(string name, uint id, GameType type, uint playerCount, uint rank, uint gameLengthInMinutes)
                {
                    if (rank == 0)
                        throw new ArgumentException(message: "rank can not be 0", paramName: nameof(rank));
                    if (playerCount < rank)
                        throw new ArgumentException(paramName: nameof(rank), message: "rank cannot be greater than playerCount");
                    Name = name;
                    Id = id;
                    Type = type;
                    PlayerCount = playerCount;
                    Rank = rank;
                    GameLengthInMinutes = gameLengthInMinutes;
                }

                ///Accessors
                public string GameAttributes()
                {
                    List<string> gameAttributes = new List<string>(){
                    $"Name: {Name}",
                    $"ID: {Id}",
                    $"Game Type: {Type}",
                    $"Player Count: {PlayerCount}",
                    $"Ranking: {Rank}",
                    $"Game Length: {GameLengthInMinutes} minutes",
                    $"Points: {ComputeXp()}"
                };
                    return String.Join("\n", gameAttributes);
                }
                public int ComputeXp()
                {
                    ///For ranked 5 and up
                    ///1st gets 24/24
                    ///2nd gets 22/24
                    ///3rd gets 20/24
                    ///4th gets 18/24
                    ///5th gets 16/24

                    double points = PointsScale * GameLengthInMinutes;
                    double xtraPoints = XtraScale * GameLengthInMinutes;
                    if (Type == GameType.Ranked)
                    {
                        if (PlayerCount >= RankedPositions)
                        {
                            if (Rank <= RankedPositions)
                                return (int)(points + (xtraPoints) / Rank);
                            else
                                return (int)points;
                        }
                        else
                        {
                            xtraPoints = (double)((XtraScale * PlayerCount) / (double)RankedPositions) * GameLengthInMinutes;
                            return (int)(points + xtraPoints / (((Rank - 1) * RankedPositions / (PlayerCount)) + 1));
                        }
                    }
                    else
                        return (int)(points + (Rank == 1 ? (xtraPoints / 2) : 0));
                }
            }
            public class Achievement
            {
                ///Variables
                public string? Name { get; set; }
                public string? Description { get; set; }
                public int XpValue { get; set; }
                public bool IsClaimed = false;

                ///Accessors
                public string AchievementAttributes()
                {
                    try
                    {
                        List<string> achievementAttributes = new List<string>(){
                        $"Name: {Name}",
                        $"Description: {Description}",
                        $"Points: {XpValue}",
                        $"Claimed: {IsClaimed}",
                    };
                        return String.Join("\n", achievementAttributes);
                    }
                    catch { throw; }
                }
            }

            ///Psudo Const
            private static readonly int[] TicketThresholds = new int[]
            {
                0, //need to add more ticket values
            };
            private static readonly List<Achievement> DefaultAchievements = JsonSerializer.Deserialize<List<Achievement>>(File.ReadAllText("Achievement.json"))!;

            ///Data
            private readonly List<User> Users = new();
            public string EventName = "";

            ///Construction
            public XpStorage(string eventName)
            {
                //attempt file load
                EventName = eventName;
            }
            public static bool CanLoad(string eventName) //returns true if there is a file with the event name else false
            {
                return false;
            }

            ///Accessors
            public User GetUser(ulong discordId)
            {
                User? u = Users.FirstOrDefault(user => user.DiscordUser.Id == discordId);
                if (u == null)
                    throw new NullReferenceException(message: "User not found in system.");
                return u;
            }
            public List<User> GetTopXUsers(int x)
            {
                if (x > Users.Count)
                    throw new ArgumentOutOfRangeException(nameof(x), "There aren't enough users to populate the list");
                Users.Sort();
                return Users.Take(x).ToList();
            }

            ///Mutators
            public void AddNewUser(SocketUser addedUser, string pid)
            {
                try
                {
                    if (Users.Any(user => user.DiscordUser.Id == addedUser.Id))
                        throw new InvalidDataException(message: "User is already registered.");
                    Users.Add(new User(addedUser, pid));
                }
                catch { throw; }
            }
            public void RemoveUser(User user) => Users.Remove(user);
            ///returns the user that won
            public SocketUser DrawRaffle()
            {
                try
                {
                    List<User> raffleEntries = new();
                    foreach (User user in Users.Where(user => !user.IsRaffleWinner))
                        raffleEntries.AddRange(TicketThresholds.Where(points => user.TotalPoints() > points).Select(_ => user));

                    if (raffleEntries.Count() <= 0)
                        throw new IndexOutOfRangeException("No valid users for raffle.");
                    Random r = new(DateTime.Now.Minute + DateTime.Now.Second + DateTime.Now.Millisecond);
                    User winner = raffleEntries[r.Next(raffleEntries.Count)];
                    winner.IsRaffleWinner = true;

                    return winner.DiscordUser;
                }
                catch (IndexOutOfRangeException) { throw; }
                catch { throw new Exception("Error in processing raffle"); }

            }

            ///Private Functions
            private void Save()
            {

            }
        }

        ///Variables
        enum GameType { Ranked = 1, CoOp = 2, Teams = 3, Party = 4 }
        public SocketTextChannel AnnouncementChannel() => Bot.Server().GetTextChannel(1106217661194571806);
        public SocketTextChannel CommandChannel() => Bot.Server().GetTextChannel(1104487160226258964);
        private XpStorage? xpSystem;
        public XPModule(Program _bot) : base(_bot) { }

        ///Constructor
        public override Task InitilizeModule()
        {
            Bot.AddConnectedCallback(async () =>
            {
                /**
                 * start
                 * Starts the event after confirmation, takes in 
                 * an event name and will refrence the directory to 
                 * see if the event needs loaded from a crash state.
                 */
                await Bot.AddCommand(new Program.Command()
                {
                    name = "start",
                    description = "starts the all-day event.",
                    callback = async (SocketSlashCommand _command) =>
                    {
                        ///Checks if the event has not started
                        if (xpSystem != null)
                            throw new Exception("Error: No event currently running.");

                        ///Adds permissions for everyone to input commands
                        //*note* uncomment next line when publishing
                        //await CommandChannel().AddPermissionOverwriteAsync(Bot.Server().EveryoneRole, OverwritePermissions.DenyAll(CommandChannel()).Modify(viewChannel: PermValue.Allow, useApplicationCommands: PermValue.Allow, sendMessages: PermValue.Allow));

                        ///Parses the event name
                        string eventName = (string)_command.Data.Options.First().Value;

                        ///Creates the event
                        xpSystem = new XpStorage(eventName);

                        ///Checks if the event was loaded and gives appropriate response
                        if (XpStorage.CanLoad(eventName))
                            await AnnouncementChannel().SendMessageAsync(text: $"@everyone Thank you for your patience, the event is back up and running!");
                        else
                            //*note* does not actually insert channel refrence
                            await AnnouncementChannel().SendMessageAsync(text: $"@everyone Welcome to {xpSystem.EventName}!\nLook to this channel for future updates and visit the #all-day-commands channel to register youself to this event! (/join-event)\n**Disclaimer**: you need to be a current student at Ohio University and be at the event to recieve any prizes");

                        ///User Feedback
                        await _command.ModifyOriginalResponseAsync(m => { m.Components = null; m.Content = "Successfully started the event."; });
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
                /**
                 * end
                 * Ends the current event after confirmation and
                 * displays information about the event in the 
                 * announcements channel *note* does not clear 
                 * event so admins can still call functions
                 */
                await Bot.AddCommand(new Program.Command()
                {
                    name = "end",
                    description = "ends the all-day event.",
                    callback = async (SocketSlashCommand _command) =>
                    {
                        ///Checks if the event has started
                        if (xpSystem == null)
                            throw new Exception("Error: No event currently running.");

                        ///Sets user permissions for the command channel back to default
                        await CommandChannel().AddPermissionOverwriteAsync(Bot.Server().EveryoneRole, OverwritePermissions.DenyAll(CommandChannel()));

                        ///Get Data
                        List<XpStorage.User> top = xpSystem.GetTopXUsers(3);

                        ///Logs the end of all day message
                        //*note* could display overall statistics for the all-day as well
                        await AnnouncementChannel().SendMessageAsync(text: $"@everyone Thank you all for participating in {xpSystem.EventName}!\nWe hope you all had fun, here are the results: \n1: {top[0].DiscordUser.Username} - {top[0].TotalPoints()}\n2: {top[0].DiscordUser.Username} - {top[0].TotalPoints()}\n3: {top[0].DiscordUser.Username} - {top[0].TotalPoints()}\nOnce again thank you all for showing up and we hope to see you at our next event!");

                        ///User Feedback
                        await _command.ModifyOriginalResponseAsync(m => { m.Components = null; m.Content = "Successfully ended the event."; });
                    },
                    modOnly = true,
                    requiresConfirmation = true,
                });
                /**
                 * draw-raffle
                 * Draws a raffle ticket after confirmation
                 */
                await Bot.AddCommand(new Program.Command()
                {
                    name = "draw-raffle",
                    description = "draws a raffle ticket",
                    callback = async (SocketSlashCommand _command) =>
                    {
                        ///Checks if the event has started
                        if (xpSystem == null)
                            throw new Exception("Error: No event currently running.");

                        try
                        {
                            ///Displays a message notifying everyone of a raffle being drawn
                            //*note* Doesn't notify officers before announcing the raffle (need to ask if this needs changed)
                            await AnnouncementChannel().SendMessageAsync(text: $"@everyone Congratulations to {xpSystem.DrawRaffle().Mention} for winning the raffle!\nMake sure to contact an officer to redeem your prize.");
                        }
                        catch { throw; }

                        ///User Feedback
                        await _command.ModifyOriginalResponseAsync(m => { m.Components = null; m.Content = "Successfully drew a raffle."; });
                    },
                    modOnly = true,
                    requiresConfirmation = true,
                });
                /**
                 * see-player
                 * Shows all data of a player including private info
                 */
                await Bot.AddCommand(new Program.Command()
                {
                    name = "see-player",
                    description = "view a player's profile",
                    callback = async (SocketSlashCommand _command) =>
                    {
                        ///Checks if the event has started
                        if (xpSystem == null)
                            throw new Exception("Error: No event currently running.");

                        ///Responds with an ephemeral embed of the info
                        //*note* this will be ephemeral, and will show private data
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
                /**
                 * show-x-users
                 * shows an ephemeral leaderboard of the top x 
                 * users and displays private information
                 */
                await Bot.AddCommand(new Program.Command()
                {
                    name = "show-x-users",
                    description = "shows a leaderboard to you",
                    callback = async (SocketSlashCommand _command) =>
                    {
                        ///Checks if the event has started
                        if (xpSystem == null)
                            throw new Exception("Error: No event currently running.");

                        try
                        {
                            ///Gets X
                            int numberOfUsers = Convert.ToInt32(_command.Data.Options.First().Value);

                            ///Format the string
                            List<XpStorage.User> top = xpSystem.GetTopXUsers(numberOfUsers);
                            string output = "";
                            for (int i = 0; i < numberOfUsers; i++)
                                output = string.Join(output, $"{numberOfUsers}: {top[i].DiscordUser.Username} - {top[i].TotalPoints()}");

                            ///Displays the leaderboard
                            await _command.RespondAsync(embed: new EmbedBuilder().AddField($"Top {numberOfUsers} Users", output).Build(), ephemeral: true);
                        }
                        catch { throw; }
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
                /**
                 * remove-player-game
                 * Removes a game from a specific player after
                 * confirmation *warnning* only do this if a 
                 * descrepancy is found
                 */
                await Bot.AddCommand(new Program.Command()
                {
                    name = "remove-player-game",
                    description = "removes a game from a player's profile",
                    callback = async (SocketSlashCommand _command) =>
                    {
                        ///Checks if the event has started
                        if (xpSystem == null)
                            throw new Exception("Error: No event currently running.");

                        try
                        {    
                            ///Get the information
                            int gameId = Convert.ToInt32(_command.Data.Options.ElementAt(1).Value);
                            XpStorage.User user = xpSystem.GetUser(((SocketUser) _command.Data.Options.First().Value).Id);

                            ///Remove the game
                            user.RemoveGame(gameId);
                        }
                        catch { throw; }

                        ///User Feedback
                        await _command.ModifyOriginalResponseAsync(m => { m.Components = null; m.Content = "Successfully removed game."; });
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
                /**
                 * remove-player-achievement
                 * Removes an achievement from a specific player
                 * after confirmation *warning* only do this if
                 * a descrepancy is found
                 */
                await Bot.AddCommand(new Program.Command()
                {
                    name = "remove-player-achievement",
                    description = "removes an achievement from a player's profile",
                    callback = async (SocketSlashCommand _command) =>
                    {
                        ///Checks if the event has started
                        if (xpSystem == null)
                            throw new Exception("Error: No event currently running.");

                        try
                        {
                            ///Get the information
                            string achievementName = (string)_command.Data.Options.ElementAt(1).Value;
                            XpStorage.User user = xpSystem.GetUser(((SocketUser)_command.Data.Options.First().Value).Id);

                            ///remove achievement
                            user.UnclaimAchievement(achievementName);
                        }
                        catch { throw; }

                        ///User Feedback
                        await _command.ModifyOriginalResponseAsync(m => { m.Components = null; m.Content = "Successfully removed achievement."; });
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
                /**
                 * remove-player
                 * Removes a player from the event *warning* only 
                 * do this if a descrepancy is found
                 */
                await Bot.AddCommand(new Program.Command()
                {
                    name = "remove-player",
                    description = "removes a player's profile",
                    callback = async (SocketSlashCommand _command) =>
                    {
                        ///Checks if the event has started
                        if (xpSystem == null)
                            throw new Exception("Error: No event currently running.");

                        try
                        {
                            ///Get user
                            XpStorage.User user = xpSystem.GetUser(((SocketUser) _command.Data.Options.First().Value).Id);
                            
                            ///Remove user
                            xpSystem.RemoveUser(user);
                        }
                        catch
                        {
                            ///Throw error if user is not found
                            throw new NullReferenceException($"User {((SocketUser) _command.Data.Options.First().Value).Username} not found in the system.");
                        }

                        ///User Feedback
                        await _command.ModifyOriginalResponseAsync(m => { m.Components = null; m.Content = "Successfully removed player."; });
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
                /**
                 * join-event
                 * Adds the caller to the event, requires a valid
                 * PID. *note* must be a current Ohio University
                 * student and at the in person event to recieve 
                 * prizes
                 */
                await Bot.AddCommand(new Program.Command()
                {
                    name = "join-event",
                    description = "registers you for the current event",
                    callback = async (SocketSlashCommand _command) =>
                    {
                        ///Checks if the event has started
                        if (xpSystem == null)
                            throw new Exception("Error: No event currently running.");

                        try
                        {
                            ///Gets PID
                            string PID = ((string)_command.Data.Options.First().Value).ToUpper();

                            ///Validates PID
                            if (PID[0] != 'P' || PID.Length != 10 || !int.TryParse(PID[1..9],out int value))
                                throw new InvalidDataException(message: "Invalid PID.");
                            for (int i = 1; i < 10; i++)
                                if (PID[i] < '0' || PID[i] > '9')
                                    throw new InvalidDataException(message: "Invalid PID.");
                            
                            ///Adds the user
                            xpSystem.AddNewUser(_command.User, PID);
                        }
                        catch { throw; }

                        ///User Feedback
                        await _command.RespondAsync(text: "Successfully joined event.", ephemeral: true);
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
                /**
                 * leave-event
                 * Unregisters the caller from the current event 
                 * after confirmation
                 */
                await Bot.AddCommand(new Program.Command()
                {
                    name = "leave-event",
                    description = "unregisters you from the current event",
                    callback = async (SocketSlashCommand _command) =>
                    {
                        ///Checks if the event has started
                        if (xpSystem == null)
                            throw new Exception("Error: No event currently running.");

                        try
                        {
                            ///Removes user
                            xpSystem.RemoveUser(xpSystem.GetUser(_command.User.Id));
                        }
                        catch { throw; }

                        ///User Feedback
                        await _command.ModifyOriginalResponseAsync(m => { m.Components = null; m.Content = "Successfully left event."; });
                    },
                    requiresConfirmation = true,
                });
                /**
                 * see-self
                 * Shows the caller's private information
                 */
                await Bot.AddCommand(new Program.Command()
                {
                    name = "see-self",
                    description = "shows you your stats",
                    callback = async (SocketSlashCommand _command) =>
                    {
                        ///Checks if the event has started
                        if (xpSystem == null)
                            throw new Exception("Error: No event currently running.");

                        try
                        {
                            ///Shows the caller's information
                            await _command.RespondAsync(embed: new EmbedBuilder().AddField("Your Data", xpSystem.GetUser(_command.User.Id).ToString()).Build(), ephemeral: true);
                        }
                        catch { throw; }
                    },
                });
                /**
                 * add-game
                 * Adds a game to the caller's profile
                 */
                await Bot.AddCommand(new Program.Command()
                {
                    name = "add-game",
                    description = "adds a game to your profile",
                    callback = async (SocketSlashCommand _command) =>
                    {
                        ///Checks if the event has started
                        if (xpSystem == null)
                            throw new Exception("Error: No event currently running.");

                        try
                        {
                            ///Get game data
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
                            
                            ///Adds the game
                            user.AddGame(gameName, playerCount, type, rank, time);
                        }
                        catch { throw; }

                        ///User Feedback
                        await _command.RespondAsync(text:"Successfully added game.", ephemeral: true);
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
                /**
                 * remove-game
                 * Removes a game from the caller's profile
                 * after confirmation
                 */
                await Bot.AddCommand(new Program.Command()
                {
                    name = "remove-game",
                    description = "removes a game from your profile",
                    callback = async (SocketSlashCommand _command) =>
                    {
                        ///Checks if the event has started
                        if (xpSystem == null)
                            throw new Exception("Error: No event currently running.");

                        try
                        {
                            ///Gets data
                            XpStorage.User user = xpSystem.GetUser(_command.User.Id);
                            int id = Convert.ToInt32(_command.Data.Options.First().Value);
                           
                            ///Removes the game
                            user.RemoveGame(id);
                        }
                        catch { throw; }

                        ///User Feedback
                        await _command.ModifyOriginalResponseAsync(m => { m.Components = null; m.Content = "Successfully removed game."; });
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
                /**
                 * show-games
                 * Show's the caller's games played
                 */
                await Bot.AddCommand(new Program.Command()
                {
                    name = "show-games",
                    description = "shows your completed games",
                    callback = async (SocketSlashCommand _command) =>
                    {
                        ///Checks if the event has started
                        if (xpSystem == null)
                            throw new Exception("Error: No event currently running.");

                        try
                        {
                            ///Gets data
                            List<string> gamelist = xpSystem.GetUser(_command.User.Id).ShowGames();

                            ///Creates embed
                            EmbedBuilder embed = new EmbedBuilder();
                            for(int i = 0; i < gamelist.Count; ++i)
                                embed.AddField($"Game {i}", gamelist[i]);

                            ///Display's games
                            await _command.RespondAsync(embed: embed.Build(), ephemeral: true);
                        }
                        catch { throw; }
                    },
                });
                /**
                 * add-achievement
                 * Adds an achievement to the user's profile
                 */
                await Bot.AddCommand(new Program.Command()
                {
                    name = "add-achievement",
                    description = "adds an achievement to your profile",
                    callback = async (SocketSlashCommand _command) =>
                    {
                        ///Checks if the event has started
                        if (xpSystem == null)
                            throw new Exception("Error: No event currently running.");

                        try
                        {
                            ///Gets data
                            string achievementName = (string) _command.Data.Options.First().Value;
                            XpStorage.User user = xpSystem.GetUser(_command.User.Id);
                            
                            ///Adds the achievement
                            user.ClaimAchievement(achievementName); 
                        }
                        catch { throw; }

                        ///User Feedback
                        await _command.ModifyOriginalResponseAsync(m => { m.Components = null; m.Content = "Successfully added achievement."; });
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
                /**
                 * remove-achievement
                 * Removes an achievement from the user's profile
                 * after confirmation
                 */
                await Bot.AddCommand(new Program.Command()
                {
                    name = "remove-achievement",
                    description = "removes an achievement from your profile",
                    callback = async (SocketSlashCommand _command) =>
                    {
                        ///Checks if the event has started
                        if (xpSystem == null)
                            throw new Exception("Error: No event currently running.");

                        try
                        {
                            ///Get data
                            string achievementName = (string) _command.Data.Options.First().Value;
                            XpStorage.User user = xpSystem.GetUser(_command.User.Id);
                            
                            ///Remove achievement
                            user.UnclaimAchievement(achievementName);
                        }
                        catch { throw; }

                        ///User Feedback
                        await _command.ModifyOriginalResponseAsync(m => { m.Components = null; m.Content = "Successfully removed achievement."; });
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
                /**
                 * show-achievements
                 * Shows a multi-page-embed of the caller's achievements
                 */
                await Bot.AddCommand(new Program.Command()
                {
                    name = "show-achievements",
                    description = "shows achievements you have completed or all available achievements",
                    callback = async (SocketSlashCommand _command) =>
                    {
                        ///Checks if the event has started
                        if (xpSystem == null)
                            throw new Exception("Error: No event currently running.");

                        try
                        {      
                            ///Gets data
                            XpStorage.User user = xpSystem.GetUser(_command.User.Id); 
                            List<string> achievementlist = new List<string>();
                            bool isShowAll = false;
                            string? achievementName = null;
                            
                            ///Get selected achievements
                            foreach (SocketSlashCommandDataOption? option in _command.Data.Options)
                            {
                                if (option is null)
                                    continue;
                                switch (option.Name)
                                {
                                    ///true: will show all possible achievements | false: will show completed achievements
                                    case "show-all":
                                        isShowAll = (bool)option.Value;
                                        break;
                                    ///if name is specified and show-all is false, then will show a specified achievement
                                    case "name":
                                        achievementName = (string)option.Value;
                                        break;
                                }
                            }
                            achievementlist = user.ShowAchievements(showAll: isShowAll, name: achievementName);

                            ///Create embed
                            EmbedBuilder embed = new EmbedBuilder();
                            for(int i = 0; i < achievementlist.Count; ++i){
                                embed.AddField($"Achievement {i}", achievementlist[i]);
                            }
                            
                            ///Display achievements
                            await _command.RespondAsync(embed: embed.Build(), ephemeral: true);
                        }
                        catch { throw; }
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

                ///Lets the user know when the bot is running
                Console.WriteLine("Commands Initalized");
            });
            return Task.CompletedTask;
        }
    }
}
