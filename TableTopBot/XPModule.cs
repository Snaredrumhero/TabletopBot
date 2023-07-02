using Discord;
using Discord.WebSocket;
using System.Text.Json;

namespace TableTopBot
{
    internal class XPModule : Module
    {
        public enum GameType { Ranked = 1, CoOp = 2, Teams = 3, Party = 4 } ///Represents the types of games

        ///Sub Classes
        /**
         * XpStorage
         * Stores an all day event for Bobcat Tabletop
         */
        public class XpStorage
        {
            ///Sub Classes
            /**
             * User
             * Stores an individual user in the event
             */
            public class User
            {
                ///Variables
                private readonly List<string> _achievementsClaimed; ///The list of all possible achievements a user can get
                private readonly List<Game> _gamesPlayed;           ///The games the user has played in

                public SocketUser DiscordUser;                      ///The user's discord
                public readonly string Pid;                         ///The user's Ohio University ID
                public bool IsRaffleWinner;                         ///If the user has won a raffle prize
                private ushort NumberGamesPlayed;                   ///only used for tracking game ids
                public MultiPageEmbed? pageEmbed;                   ///Used to create embed pages for user's games and achievements
                public int BoughtTickets;                           ///Tickets that are bought with the user's points
                
                ///Tells the current points a user has by using the maximum points and the tickets bought by the user
                public int CurrentPoints() { return TotalPoints() - (BoughtTickets * TicketPrice); } 
            
                ///Constructor
                public User(SocketUser user, string pid, List<Game>? gamesPlayed = null, bool isRaffleWinner = false, ushort numberGamesPlayed = 0, List<string>? achievements = null, int tickets = 0)
                {
                    DiscordUser = user;
                    Pid = pid;
                    _gamesPlayed = gamesPlayed == null ? new List<Game>() : gamesPlayed;
                    IsRaffleWinner = isRaffleWinner;
                    NumberGamesPlayed = numberGamesPlayed;
                    _achievementsClaimed = achievements ?? new List<string>();
                    BoughtTickets = tickets;
                }
                private User(ulong id, string pid, List<Game>? gamesPlayed = null, bool isRaffleWinner = false, ushort numberGamesPlayed = 0, List<string>? achievements = null, int tickets = 0) : this(Program.Server().GetUser(id), pid, gamesPlayed, isRaffleWinner, numberGamesPlayed, achievements, tickets) { }

                ///Mutators
                ///Rank = 1 for win 2 for loss in an unranked game
                public void AddGame(string name, uint playerCount, GameType type, uint rank, uint length) => _gamesPlayed.Add(new Game(name, NumberGamesPlayed++, type, playerCount, rank, length));
                public void RemoveGame(int id) => _gamesPlayed.RemoveAll(game => game.Data.Id == id);
                public void ClaimAchievement(string achievementName)
                {
                    if (!DefaultAchievements.Select(a => a.Data.Name).Contains(achievementName))
                        throw new ArgumentException("Achievement Not Found");
                    else if(_achievementsClaimed.Select(a => a).Contains(achievementName))
                    {
                        throw new ArgumentException("Already Claimed Achievement");
                    }
                    _achievementsClaimed.Add(achievementName);
                }
                public void UnclaimAchievement(string achievementName) => _achievementsClaimed.Remove(achievementName);

                ///Accessors
                public List<Game> GamesPlayed { get { return _gamesPlayed; } }
                public List<Achievement> Achievements => DefaultAchievements.Where(achievement => _achievementsClaimed.Contains(achievement.Data.Name)).ToList();
                public int TotalPoints() { return _gamesPlayed.Select(game => game.XpValue).Sum() + Achievements.Select(achievement => achievement.Data.XpValue).Sum(); }
                public override string ToString() { return $"{DiscordUser.Username}\nPID: {Pid}\nPoints: {CurrentPoints()}\nClaimed Raffle: {IsRaffleWinner}\nBought Tickets: {BoughtTickets}"; }
                public List<EmbedBuilder> ShowGames(int list_games = Int32.MinValue)
                {
                    List<EmbedBuilder> embedlist = new List<EmbedBuilder>();
                    string gamelist = "";

                    if (list_games == Int32.MinValue)
                    {
                        for (int i = 0; i < _gamesPlayed.Count(); ++i)
                        {
                            gamelist += (_gamesPlayed[i].ToString() + "\n\n");
                            if ((i + 1) % 5 == 0)
                            {
                                embedlist.Add(new EmbedBuilder().AddField("Your Games",gamelist));
                                gamelist = "";
                            }
                        }
                        if (!string.IsNullOrEmpty(gamelist))
                            embedlist.Add(new EmbedBuilder().AddField("Your Games",gamelist));
                    }
                    else
                    {
                        gamelist += _gamesPlayed.FirstOrDefault(game => game.Data.Id == list_games)!.ToString() ?? throw new Exception("Cannot find game");
                        embedlist.Add(new EmbedBuilder().AddField("Found Game",gamelist));
                    }
                    //return gamelist;
                    return embedlist;
                }
                public List<EmbedBuilder> ShowAchievements(bool showAll = false, string? name = null)
                {
                    List<EmbedBuilder> embedlist = new List<EmbedBuilder>();
                    string achievementlist = "";

                    if (showAll)
                    {
                        for (int i = 0; i < DefaultAchievements.Count; ++i)
                        {
                            achievementlist += $"{DefaultAchievements[i]}\nClaimed: {_achievementsClaimed.Contains(DefaultAchievements[i].Data.Name)}\n\n";
                            if ((i + 1) % 5 == 0)
                            {
                                embedlist.Add((new EmbedBuilder().AddField("List of Achievements", achievementlist)));
                                achievementlist = "";
                            }
                        }
                        if (!string.IsNullOrEmpty(achievementlist))
                            embedlist.Add(new EmbedBuilder().AddField("List of Achievements",achievementlist));

                    }
                    else if (string.IsNullOrEmpty(name))
                    {
                        for (int i = 0; i < Achievements.Count; ++i)
                        {
                            achievementlist += $"{Achievements[i]}\n\n";
                            if ((i + 1) % 5 == 0)
                            {
                                embedlist.Add((new EmbedBuilder().AddField("Your Achievements", achievementlist)));
                                achievementlist = "";
                            }
                        }
                        if (!string.IsNullOrEmpty(achievementlist))
                            embedlist.Add((new EmbedBuilder().AddField("Your Achievements", achievementlist)));
                    }
                    else
                    {
                        achievementlist += Achievements.FirstOrDefault(achievement => achievement.Data.Name == name)!.ToString() ?? throw new ArgumentException("Achievement Not Found");
                        embedlist.Add((new EmbedBuilder().AddField("Achievement Found", achievementlist)));
                    }
                    return embedlist;
                }

                ///Operators
                public static bool operator >(User a, User b) => a.TotalPoints() > b.TotalPoints();
                public static bool operator <(User a, User b) => a.TotalPoints() < b.TotalPoints();
                public static implicit operator UserData(User u) => new UserData(u.DiscordUser.Id, u.Pid, u.GamesPlayed.Select(g => (GameData)g).ToArray(), u.IsRaffleWinner, u._achievementsClaimed.ToArray(), u.NumberGamesPlayed, u.BoughtTickets);
                public static implicit operator User(UserData u) => new User(u.DiscordId, u.PID, u.GamesPlayed.Select(g => (Game)g).ToList(), u.WonRaffle, u.NumberGamesPlayed, u.AchievementsClaimed.ToList());
            }
            public struct UserData
            {
                public ulong DiscordId;
                public string PID;
                public GameData[] GamesPlayed;
                public bool WonRaffle;
                public String[] AchievementsClaimed;
                public ushort NumberGamesPlayed;
                public int Tickets;

                public UserData(ulong discordId, string pID, GameData[] gamesPlayed, bool wonRaffle, string[] achievementsClaimed, ushort numberGamesPlayed, int tickets)
                {
                    DiscordId = discordId;
                    PID = pID;
                    GamesPlayed = gamesPlayed;
                    WonRaffle = wonRaffle;
                    AchievementsClaimed = achievementsClaimed;
                    NumberGamesPlayed = numberGamesPlayed;
                    Tickets = tickets;
                }
            }

            /**
             * Game
             * Stores a game for a single user
             */
            public class Game
            {
                ///Static
                private static readonly double PointsScale = 1.5;             ///The scale of points per minute
                private static readonly double XtraScale = PointsScale / 0.5; ///The scale of extra points awarded
                private static readonly int RankedPositions = 5;              ///The amount of ranked positions

                ///Variables
                public readonly GameData Data; ///Stores the data for a game
                public readonly int XpValue;

                ///Constructor
                public Game(string name, uint id, GameType type, uint playerCount, uint rank, uint gameLengthInMinutes) : this(new GameData(name, id, type, playerCount, rank, gameLengthInMinutes))
                {
                    if (rank == 0)
                        throw new ArgumentException(message: "rank can not be 0", paramName: nameof(rank));
                    if (playerCount < rank)
                        throw new ArgumentException(paramName: nameof(rank), message: "rank cannot be greater than playerCount");
                }
                private Game(GameData data) 
                { 
                    Data = data;

                    ///XP Computation
                    ///For ranked 5 and up
                    ///1st gets 24/24
                    ///2nd gets 22/24
                    ///3rd gets 20/24
                    ///4th gets 18/24
                    ///5th gets 16/24

                    double points = PointsScale * Data.GameLength;
                    double xtraPoints = XtraScale * Data.GameLength;
                    if (Data.Type == GameType.Ranked)
                    {
                        if (Data.PlayerCount >= RankedPositions)
                        {
                            if (Data.Rank <= RankedPositions)
                                XpValue = (int)(points + (xtraPoints) / Data.Rank);
                            else
                                XpValue = (int)points;
                        }
                        else
                        {
                            xtraPoints = (double)((XtraScale * Data.PlayerCount) / (double)RankedPositions) * Data.GameLength;
                            XpValue = (int)(points + xtraPoints / (((Data.Rank - 1) * RankedPositions / (Data.PlayerCount)) + 1));
                        }
                    }
                    else
                        XpValue = (int)(points + (Data.Rank == 1 ? (xtraPoints / 2) : 0));
                }

                ///Accessors
                public override string ToString() => $"Name: {Data.Name}\nID: {Data.Id}\nGame Type: {Data.Type}\nPlayer Count: {Data.PlayerCount}\nRanking: {Data.Rank}\nGame Length: {Data.GameLength} min\nPoints: {XpValue}\n";

                ///Operators
                public static implicit operator GameData(Game g) => g.Data;
                public static implicit operator Game(GameData g) => new Game(g);
            }
            public struct GameData
            {
                public string Name;      ///The name of the game
                public uint Id;          ///The id of the game
                public GameType Type;    ///The type of game
                public uint PlayerCount; ///The amount of players in the game
                public uint Rank;        ///The user's rank
                public uint GameLength;  ///The length of the game in minutes

                public GameData(string name, uint id, GameType type, uint playerCount, uint rank, uint gameLength)
                {
                    Name = name;
                    Id = id;
                    Type = type;
                    PlayerCount = playerCount;
                    Rank = rank;
                    GameLength = gameLength;
                }
            }

            /**
             * Achievement
             * Stores an achivement for a single user
             */
            public class Achievement
            {
                ///Variables
                public readonly AchievementData Data; ///Stores the data for an achievement

                ///Constructor
                private Achievement(AchievementData data) { Data = data; }

                ///Accessors
                public override string ToString() { return $"Name: {Data.Name}\nDescription: {Data.Description}\nPoints: {Data.XpValue}"; }

                ///Operators
                public static implicit operator Achievement(AchievementData a) => new Achievement(a);
            }
            public struct AchievementData
            {
                public string Name{get;set;}        ///The name of the achievement
                public string Description{get;set;} ///The requirements to get it
                public int XpValue{get;set;}        ///The reward amount

                public AchievementData(string name, string description, int xpValue)
                {
                    Name = name;
                    Description = description;
                    XpValue = xpValue;
                }
            }

            ///Psudo Const
            public static readonly int[] TicketThresholds = new int[] { 0, 75, 300, 675, 1250 };
            private static readonly List<Achievement> DefaultAchievements = (JsonSerializer.Deserialize<AchievementData[]>(File.ReadAllText("./Achievement.json")) ?? throw new NullReferenceException("No Data In Achievement File")).Select(a => (Achievement)a).ToList();
            private static readonly JsonSerializerOptions JsonOptions = new JsonSerializerOptions { WriteIndented = true, IncludeFields = true };

            ///Data
            private readonly List<User> Users;
            public readonly string EventName;
            
            public static readonly int TicketPrice = 1000;

            ///Construction
            public XpStorage(string eventName)
            {
                EventName = eventName;
                if (File.Exists(Path))
                {
                    FileStream stream = new FileStream(Path, FileMode.Open);
                    Users = (JsonSerializer.Deserialize<UserData[]>(stream, JsonOptions) ?? new UserData[0]).Select(u => (User)u).ToList();
                    stream.Close();
                }
                else Users = new List<User>();
            }
            public static bool CanLoad(string eventName) { return File.Exists($"./{eventName}.json"); }

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
                    x = Users.Count;
                    //throw new ArgumentOutOfRangeException(nameof(x), "There aren't enough users to populate the list");
                Users.Sort();
                return Users.Take(x).ToList();
            }
            private string Path { get { return $"./{EventName}.json"; } }

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
            public SocketUser DrawRaffle()
            {
                try
                {
                    List<User> raffleEntries = new();
                    foreach (User user in Users.Where(user => !user.IsRaffleWinner)){
                        raffleEntries.AddRange(TicketThresholds.Where(points => user.TotalPoints() > points).Select(_ => user));
                        for(int i = 0; i < user.BoughtTickets; ++i)
                            raffleEntries.Add(user);
                    }

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
            public async Task Save() => await File.WriteAllTextAsync(Path, JsonSerializer.Serialize(Users.Select(u => (UserData)u), JsonOptions));
        }

        ///Variables
        private static PrivateVariable? PrivateVariables = JsonSerializer.Deserialize<PrivateVariable>(File.ReadAllText("./PrivateVariables.json"));
        public SocketTextChannel AnnouncementChannel() => Program.Server().GetTextChannel(PrivateVariables!.AnnouncementChannel);
        public SocketTextChannel CommandChannel() => Program.Server().GetTextChannel(PrivateVariables!.CommandChannel);
        public XpStorage? xpSystem;
        public XPModule(Program _bot) : base(_bot) { }

        ///Constructor
        public override Task InitilizeModule()
        {
            Bot.AddConnectedCallback(async () =>
            {
                /**
                 * start-event
                 * Starts the event after confirmation, takes in 
                 * an event name and will refrence the directory to 
                 * see if the event needs loaded from a crash state.
                 */
                await Bot.AddCommand(new Program.Command()
                {
                    name = "start-event",
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
                            await AnnouncementChannel().SendMessageAsync(text: $"@everyone Welcome to {xpSystem.EventName}!\nLook to this channel for future updates and visit the {CommandChannel().Mention} channel to register youself to this event! (/join-event)\n**Disclaimer**: you need to be a current student at Ohio University and be at the event to recieve any prizes");
                        
                        ///Saves changes
                        await xpSystem.Save();

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
                 * end-event
                 * Ends the current event after confirmation and
                 * displays information about the event in the 
                 * announcements channel *note* does not clear 
                 * event so admins can still call functions
                 */
                await Bot.AddCommand(new Program.Command()
                {
                    name = "end-event",
                    description = "ends the all-day event.",
                    callback = async (SocketSlashCommand _command) =>
                    {
                        ///Checks if the event has started
                        if (xpSystem == null)
                            throw new Exception("Error: No event currently running.");

                        ///Sets user permissions for the command channel back to default
                        await CommandChannel().AddPermissionOverwriteAsync(Program.Server().EveryoneRole, OverwritePermissions.DenyAll(CommandChannel()));

                        ///Get Data
                        List<XpStorage.User> top = xpSystem.GetTopXUsers(3);
                        string temp = "";
                        for(int i = 0; i < 3 && i < top.Count; ++i)
                        {
                            temp += $"\n{i+1}: {top[i].DiscordUser.Mention} - {top[i].CurrentPoints()} points\n";
                        }
                        ///Logs the end of all day message
                        //*note* could display overall statistics for the all-day as well
                        await AnnouncementChannel().SendMessageAsync(text: $"@everyone Thank you all for participating in {xpSystem.EventName}!\nWe hope you all had fun, here are the results: {temp} \nOnce again thank you all for showing up and we hope to see you at our next event!");

                        ///Saves changes
                        await xpSystem.Save();

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

                        ///Saves changes
                        await xpSystem.Save();

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
                            for (int i = 0; i < top.Count; i++)
                                output = string.Join(output, $"{i+1}: {top[i].DiscordUser.Username} - {top[i].CurrentPoints()}");

                            ///Displays the leaderboard
                            await _command.RespondAsync(embed: new EmbedBuilder().AddField($"Top {top.Count} Users", output).Build(), ephemeral: true);
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
                            XpStorage.User user = xpSystem.GetUser(((SocketUser)_command.Data.Options.First().Value).Id);

                            ///Remove the game
                            user.RemoveGame(gameId);
                        }
                        catch { throw; }

                        ///Saves changes
                        await xpSystem.Save();

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

                        ///Saves changes
                        await xpSystem.Save();

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
                            XpStorage.User user = xpSystem.GetUser(((SocketUser)_command.Data.Options.First().Value).Id);

                            ///Remove user
                            xpSystem.RemoveUser(user);
                        }
                        catch
                        {
                            ///Throw error if user is not found
                            throw new NullReferenceException($"User {((SocketUser)_command.Data.Options.First().Value).Username} not found in the system.");
                        }

                        ///Saves changes
                        await xpSystem.Save();

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
                            if (PID[0] != 'P' || PID.Length != 10 || !int.TryParse(PID[1..9], out int value))
                                throw new InvalidDataException(message: "Invalid PID.");
                            for (int i = 1; i < 10; i++)
                                if (PID[i] < '0' || PID[i] > '9')
                                    throw new InvalidDataException(message: "Invalid PID.");

                            ///Adds the user
                            xpSystem.AddNewUser(_command.User, PID);
                        }
                        catch { throw; }

                        ///Saves changes
                        await xpSystem.Save();

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

                        ///Saves changes
                        await xpSystem.Save();

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
                            string gameName = (string)_command.Data.Options.First().Value;
                            uint playerCount = Convert.ToUInt32(_command.Data.Options.ElementAt(1).Value);
                            GameType type = (_command.Data.Options.ElementAt(2).Value as string) switch
                            {
                                "ranked" => GameType.Ranked,
                                "coop" => GameType.CoOp,
                                "teams" => GameType.Teams,
                                "party" => GameType.Party,
                                _ =>
                                throw new InvalidDataException(message: "Invalid Game Type.")
                            };
                            uint rank = Convert.ToUInt32(_command.Data.Options.ElementAt(3).Value);
                            uint time = Convert.ToUInt32(_command.Data.Options.ElementAt(4).Value);

                            ///Adds the game
                            user.AddGame(gameName, playerCount, type, rank, time);
                        }
                        catch { throw; }

                        ///Saves changes
                        await xpSystem.Save();

                        ///User Feedback
                        await _command.RespondAsync(text: "Successfully added game.", ephemeral: true);
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

                        ///Saves changes
                        await xpSystem.Save();

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
                 * see-games
                 * Show's the caller's games played
                 */
                await Bot.AddCommand(new Program.Command()
                {
                    name = "see-games",
                    description = "shows your completed games",
                    callback = async (SocketSlashCommand _command) =>
                    {
                        ///Checks if the event has started
                        if (xpSystem == null)
                            throw new Exception("Error: No event currently running.");

                        try
                        {
                            ///Gets user
                            
                            XpStorage.User user = xpSystem.GetUser(_command.User.Id);
                            
                            ///Gets data and creates embed
                            List<EmbedBuilder> gamelist = user.ShowGames();
                            
                            ///Sets up MultiPageEmbed
                            user.pageEmbed = new MultiPageEmbed(gamelist);
                            
                            ///Display's games
                            await user.pageEmbed.StartPage(_command);
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
                            string achievementName = (string)_command.Data.Options.First().Value;
                            XpStorage.User user = xpSystem.GetUser(_command.User.Id);

                            ///Adds the achievement
                            user.ClaimAchievement(achievementName);
                        }
                        catch { throw; }

                        ///Saves changes
                        await xpSystem.Save();

                        ///User Feedback
                        await _command.RespondAsync(text: "Successfully added achievement.", ephemeral: true);
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
                            string achievementName = (string)_command.Data.Options.First().Value;
                            XpStorage.User user = xpSystem.GetUser(_command.User.Id);

                            ///Remove achievement
                            user.UnclaimAchievement(achievementName);
                        }
                        catch { throw; }

                        ///Saves changes
                        await xpSystem.Save();

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
                 * see-achievements
                 * Shows a multi-page-embed of the caller's achievements
                 */
                await Bot.AddCommand(new Program.Command()
                {
                    name = "see-achievements",
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
                            List<EmbedBuilder> achievementlist = new List<EmbedBuilder>();
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
                            ///Create embed
                            achievementlist = user.ShowAchievements(showAll: isShowAll, name: achievementName);

                            ///Display achievements
                            // EmbedBuilder embed = new EmbedBuilder();
                            // for (int i = 0; i < achievementlist.Count; ++i)
                            //     embed.AddField($"Achievement {i}", achievementlist[i]);

                            // await _command.RespondAsync(embed: achievementlist[0].Build(), ephemeral: true);
                            user.pageEmbed = new MultiPageEmbed(achievementlist);
                            await user.pageEmbed.StartPage(_command);
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
                
                await Bot.AddCommand(new Program.Command()
                {
                    name = "buy-tickets",
                    description = "buy tickets",
                    callback = async (SocketSlashCommand _command) =>
                    {
                        ///Checks if the event has started
                        if (xpSystem == null)
                            throw new Exception("Error: No event currently running.");

                        try
                        {
                            ///Get the information
                            int ticketsBought = Convert.ToInt32(_command.Data.Options.First().Value);
                            XpStorage.User user = xpSystem.GetUser(_command.User.Id);
                            if (user.TotalPoints() < 1250)
                            {
                                throw new Exception("Error: User did not reach point threshold of 1250");
                            }
                            else if ((ticketsBought * XpStorage.TicketPrice) > user.TotalPoints())
                            {
                                throw new Exception($"Error: User does not have enough points. Tickets cost {XpStorage.TicketPrice} points each.");
                            }
                            user.BoughtTickets += ticketsBought;
                            

                        }
                        catch { throw; }

                        ///Saves changes
                        await xpSystem.Save();

                        ///User Feedback
                        //await _command.ModifyOriginalResponseAsync(m => { m.Components = null; m.Content = "Successfully removed game."; });
                        await _command.ModifyOriginalResponseAsync(m => { m.Components = null; m.Content = $"Successfully bought {_command.Data.Options.First().Value} tickets."; });
                    },
                    requiresConfirmation = true,
                    options = new List<SlashCommandOptionBuilder>() {
                        new SlashCommandOptionBuilder(){
                            Name = "tickets",
                            Type = ApplicationCommandOptionType.Integer,
                            Description = "the number of tickets to be bought",
                            IsRequired = true,
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