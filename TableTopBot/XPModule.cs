namespace TableTopBot
{
    internal class XPModule : Module
    {
        private const ushort TICKET_VALUE = 1000;
        public static readonly int[] TICKET_THRESHOLDS = new int[] { 0, 75, 300, 675, 1250 };
        public enum GameType { NULL = 0, Ranked = 1, CoOp = 2, Teams = 3, Party = 4 } ///Represents the types of games

        ///Sub Classes
        public class XpStorage
        {
            ///Static
            private static readonly JsonSerializerOptions JsonOptions = new JsonSerializerOptions { WriteIndented = true, IncludeFields = true };
            public static bool CanLoad(string eventName) => File.Exists($"./{eventName}.json");

            ///Dynamic
            public readonly string EventName;
            private string Path => $"./{EventName}.json";
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
                Save();
            }
            private void Save() => File.WriteAllText(Path, JsonSerializer.Serialize(Users.Select(u => (UserData)u), JsonOptions));
            public string DisplayAll()
            {
                ulong averageTime = 0;
                uint averageGames = 0;
                ulong averageXP = 0;
                uint totalUsers = (uint)Users.Count;
                foreach (User u in Users)
                {
                    averageTime += u.CurrentTime;
                    averageGames += (uint)u.GamesPlayed.Count;
                    averageXP += u.TotalPoints;
                }
                return $"***Average Time Spent:*** {Math.Round((double)averageTime / totalUsers, 2)} Minutes\n***Average Number of Games Played:*** {Math.Round((double)averageGames / totalUsers, 2)}\n***Average Amount of XP Earned:*** {Math.Round((double)averageXP / totalUsers, 2)}\n***Total Attendees:*** {totalUsers}";
            }

            #region Users
            private class User
            {
                ///Variables
                private readonly List<string> _achievementsClaimed; ///The list of all possible achievements a user can get
                private readonly List<Game> _gamesPlayed;           ///The games the user has played in

                public SocketUser DiscordUser;                      ///The user's discord
                public readonly string Pid;                         ///The user's Ohio University ID
                public bool IsRaffleWinner;                         ///If the user has won a raffle prize
                private ushort NumberGamesPlayed;                   ///only used for tracking game ids
                public uint BoughtTickets;                          ///Tickets that are bought with the user's points

                public uint CurrentPoints => TotalPoints - (BoughtTickets * TICKET_VALUE); ///Tells the current points a user has by using the maximum points and the tickets bought by the user
                public uint CurrentTime => (uint)_gamesPlayed.Sum(g => g.Data.GameLength);

                ///Constructor
                public User(SocketUser user, string pid, List<Game>? gamesPlayed = null, bool isRaffleWinner = false, ushort numberGamesPlayed = 0, List<string>? achievements = null, uint tickets = 0)
                {
                    DiscordUser = user;
                    Pid = pid;
                    _gamesPlayed = gamesPlayed == null ? new List<Game>() : gamesPlayed;
                    IsRaffleWinner = isRaffleWinner;
                    NumberGamesPlayed = numberGamesPlayed;
                    _achievementsClaimed = achievements ?? new List<string>();
                    BoughtTickets = tickets;
                }
                private User(ulong id, string pid, List<Game>? gamesPlayed = null, bool isRaffleWinner = false, ushort numberGamesPlayed = 0, List<string>? achievements = null, uint tickets = 0) : this(Program.Server.GetUser(id), pid, gamesPlayed, isRaffleWinner, numberGamesPlayed, achievements, tickets) { }

                ///Mutators
                ///Rank = 1 for win 2 for loss in an unranked game
                public Game AddGame(string name, uint playerCount, GameType type, uint rank, uint length){
                    _gamesPlayed.Add(new Game(name, NumberGamesPlayed++, type, playerCount, rank, length));
                    return _gamesPlayed.Last();
                }
                public Game RemoveGame(int id)
                { 
                    Game removedGame = _gamesPlayed.Find(g => g.Data.Id == id) ?? throw new Exception("Cannot find game"); 
                    _gamesPlayed.RemoveAll(game => game.Data.Id == id);
                    return removedGame;
                }
                public string ClaimAchievement(string achievementName)
                {
                    if (!DefaultAchievements.Select(a => a.Data.Name).Contains(achievementName))
                        throw new ArgumentException("Achievement Not Found");
                    else if (_achievementsClaimed.Select(a => a).Contains(achievementName))
                        throw new ArgumentException("Already Claimed Achievement");
                    _achievementsClaimed.Add(achievementName);
                    
                    return achievementName;
                    
                }
                public string UnclaimAchievement(string achievementName) 
                {
                    
                    if(_achievementsClaimed.Remove(achievementName))
                        return achievementName;
                    else
                        throw new Exception("Achievement not found");
                    
    
                }   
                ///Accessors
                public List<Game> GamesPlayed => _gamesPlayed;
                public uint TotalPoints => (uint)(_gamesPlayed.Select(game => game.XpValue).Sum() + DefaultAchievements.Where(achievement => _achievementsClaimed.Contains(achievement.Data.Name)).Select(achievement => achievement.Data.XpValue).Sum());
                public override string ToString() => $"__**{DiscordUser.Username}**__\n- PID: {Pid}\n- Points: {CurrentPoints}\n- Claimed Raffle: {IsRaffleWinner}\n- Bought Tickets: {BoughtTickets}";
                public EmbedBuilder[] ShowGames()
                {
                    List<EmbedBuilder> embedlist = new List<EmbedBuilder>();

                    for (int i = 0; i < _gamesPlayed.Count; i++)
                        embedlist.Add(new EmbedBuilder().WithDescription(_gamesPlayed[i].ToString()));
                    
                    return embedlist.ToArray();
                }
                public EmbedBuilder[] ShowAchievements(bool showAll = false)
                {
                    List<EmbedBuilder> embedlist = new List<EmbedBuilder>();

                    Achievement[] achievements = (showAll ? DefaultAchievements : DefaultAchievements.Where(a => _achievementsClaimed.Contains(a.Data.Name))).ToArray();

                    for (int i = 0; i < achievements.Length; i++)
                        embedlist.Add(new EmbedBuilder().WithDescription(achievements[i].ToString()));

                    return embedlist.ToArray();
                }

                ///Operators
                public static bool operator >(User a, User b) => a.TotalPoints > b.TotalPoints;
                public static bool operator <(User a, User b) => a.TotalPoints < b.TotalPoints;
                public static implicit operator UserData(User u) => new UserData(u.DiscordUser.Id, u.Pid, u.GamesPlayed.Select(g => (GameData)g).ToArray(), u.IsRaffleWinner, u._achievementsClaimed.ToArray(), u.NumberGamesPlayed, u.BoughtTickets);
                public static implicit operator User(UserData u) => new User(u.DiscordId, u.PID, u.GamesPlayed.Select(g => (Game)g).ToList(), u.WonRaffle, u.NumberGamesPlayed, u.AchievementsClaimed.ToList());
            }
            private struct UserData
            {
                public ulong DiscordId;
                public string PID;
                public GameData[] GamesPlayed;
                public bool WonRaffle;
                public String[] AchievementsClaimed;
                public ushort NumberGamesPlayed;
                public uint Tickets;

                public UserData(ulong discordId, string pID, GameData[] gamesPlayed, bool wonRaffle, string[] achievementsClaimed, ushort numberGamesPlayed, uint tickets)
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
            private readonly List<User> Users;
            public void AddNewUser(SocketUser addedUser, string pid)
            {
                ///Validate PID
                if (pid[0] != 'P' || pid.Length != 10 || !int.TryParse(pid[1..9], out int value))
                    throw new InvalidDataException(message: "Invalid PID.");
                for (int i = 1; i < 10; i++)
                    if (pid[i] < '0' || pid[i] > '9')
                        throw new InvalidDataException(message: "Invalid PID.");
                if (Users.Any(user => user.DiscordUser.Id == addedUser.Id))
                    throw new InvalidDataException(message: "User is already registered.");
                Users.Add(new User(addedUser, pid));
                Save();
            }
            public void RemoveUser(ulong discordId)
            {
                Users.RemoveAll(user => user.DiscordUser.Id == discordId);
                Save();
            }
            public string GetUser(ulong discordId) => (Users.FirstOrDefault(user => user.DiscordUser.Id == discordId) ?? throw new NullReferenceException(message: "User not found in system.")).ToString();
            public string GetTopXUsersString(int x, bool mention = false)
            {
                if (x > Users.Count)
                    x = Users.Count;
                Users.Sort();
                List<User> top = Users.Take(x > Users.Count ? Users.Count : x).ToList();
                string output = "";
                for (int i = 0; i < top.Count; i++)
                    output = string.Join(output, $"{i + 1}: {(mention ? top[i].DiscordUser.Mention : Program.Server.GetUser(top[i].DiscordUser.Id).DisplayName)} - {top[i].CurrentPoints}\n");
                return output;
            }
            public void UserBuyTickets(ulong discordId, uint x = 1)
            {
                User u = Users.FirstOrDefault(user => user.DiscordUser.Id == discordId) ?? throw new NullReferenceException(message: "User not found in system.");
                if (u.TotalPoints < TICKET_THRESHOLDS[TICKET_THRESHOLDS.Length - 1])
                    throw new Exception($"Error: User did not reach point threshold of {TICKET_THRESHOLDS[TICKET_THRESHOLDS.Length - 1]}");
                else if (x * TICKET_VALUE > u.TotalPoints)
                    throw new Exception($"Error: User does not have enough points. Tickets cost {TICKET_VALUE} points each.");
                u.BoughtTickets += x;
                Save();
            } 
            public EmbedBuilder[] GetUserGames(ulong discordId) => (Users.FirstOrDefault(user => user.DiscordUser.Id == discordId) ?? throw new NullReferenceException(message: "User not found in system.")).ShowGames();
            public EmbedBuilder[] GetUserAchievements(ulong discordId, bool showAll = false) => (Users.FirstOrDefault(user => user.DiscordUser.Id == discordId) ?? throw new NullReferenceException(message: "User not found in system.")).ShowAchievements(showAll);
            #endregion

            #region Games
            private class Game
            {
                ///Static
                private static readonly double PointsScale = 1.5;             ///The scale of points per minute
                private static readonly double XtraScale = PointsScale / 0.5; ///The scale of extra points awarded
                private static readonly int RankedPositions = 5;              ///The amount of ranked positions

                ///Variables
                public readonly GameData Data; ///Stores the data for a game
                public readonly int XpValue;   ///The amount of Xp the game is worth

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
                public override string ToString() => $"__**{Data.Name}**__\n- ID: {Data.Id}\n- Game Type: {Data.Type}\n- Player Count: {Data.PlayerCount}\n- Ranking: {Data.Rank}\n- Game Length: {Data.GameLength} min\n- Points: {XpValue}\n- Played on: {Data.PlayedAt.ToString()}";

                ///Operators
                public static implicit operator GameData(Game g) => g.Data;
                public static implicit operator Game(GameData g) => new Game(g);
            }
            private struct GameData
            {
                public string Name;      ///The name of the game
                public uint Id;          ///The id of the game
                public GameType Type;    ///The type of game
                public uint PlayerCount; ///The amount of players in the game
                public uint Rank;        ///The user's rank
                public uint GameLength;  ///The length of the game in minutes
                public DateTime PlayedAt;

                public GameData(string name, uint id, GameType type, uint playerCount, uint rank, uint gameLength)
                {
                    Name = name;
                    Id = id;
                    Type = type;
                    PlayerCount = playerCount;
                    Rank = rank;
                    GameLength = gameLength;
                    PlayedAt = DateTime.Now;
                }
            }
            public EmbedBuilder AddUserGame(ulong discordId, string name, uint playerCount, GameType type, uint rank, uint length)
            {
                Game gameAdded = (Users.FirstOrDefault(user => user.DiscordUser.Id == discordId) ?? throw new NullReferenceException(message: "User not found in system.")).AddGame(name, playerCount, type, rank, length);
                Save();
                return new EmbedBuilder().WithDescription(gameAdded.ToString());
            }
            public EmbedBuilder RemoveUserGame(ulong discordId, int gameId)
            {
                Game gameRemoved = (Users.FirstOrDefault(user => user.DiscordUser.Id == discordId) ?? throw new NullReferenceException(message: "User not found in system.")).RemoveGame(gameId);
                Save();
                return new EmbedBuilder().WithDescription(gameRemoved.ToString());
                
            }
            #endregion

            #region Achievements
            private class Achievement
            {
                public readonly AchievementData Data; ///Stores the data for an achievement
                private Achievement(AchievementData data) { Data = data; } ///Achievement Constructor
                public override string ToString() => $"__**{Data.Name}**__\n- Description: {Data.Description}\n- Points: {Data.XpValue}"; ///Turns the Achievement into a string

                public static implicit operator Achievement(AchievementData a) => new Achievement(a); ///Converts data into an achievement
            }
            private struct AchievementData
            {
                public string Name { get; set; }        ///The name of the achievement
                public string Description { get; set; } ///The requirements to get it
                public int XpValue { get; set; }        ///The reward amount

                public AchievementData(string name, string description, int xpValue)
                {
                    Name = name;
                    Description = description;
                    XpValue = xpValue;
                }
            }
            private static readonly List<Achievement> DefaultAchievements = (JsonSerializer.Deserialize<AchievementData[]>(File.ReadAllText("./Achievements.json")) ?? throw new NullReferenceException("No Data In Achievement File")).Select(a => (Achievement)a).ToList();
            public EmbedBuilder ClaimUserAchievement(ulong discordId, string achievementName)
            {
                string achievementClaimed = (Users.FirstOrDefault(user => user.DiscordUser.Id == discordId) ?? throw new NullReferenceException(message: "User not found in system.")).ClaimAchievement(achievementName);
                Save();
                return new EmbedBuilder().WithDescription((DefaultAchievements.Find(g => g.Data.Name == achievementClaimed) ?? throw new ArgumentException("Achievement not Found")).ToString());
            }
            public EmbedBuilder UnclaimUserAchievement(ulong discordId, string achievementName)
            {
                string achievementUnclaimed = (Users.FirstOrDefault(user => user.DiscordUser.Id == discordId) ?? throw new NullReferenceException(message: "User not found in system.")).UnclaimAchievement(achievementName);
                Save();
                return new EmbedBuilder().WithDescription(achievementUnclaimed.ToString());
            }
            #endregion

            #region Raffles
            public SocketUser DrawRaffle()
            {
                try
                {
                    List<User> raffleEntries = new List<User>();
                    foreach (User user in Users.Where(user => !user.IsRaffleWinner))
                    {
                        raffleEntries.AddRange(TICKET_THRESHOLDS.Where(points => user.TotalPoints > points).Select(_ => user));
                        for (int i = 0; i < user.BoughtTickets; ++i)
                            raffleEntries.Add(user);
                    }

                    if (raffleEntries.Count() <= 0)
                        throw new IndexOutOfRangeException("No valid users for raffle.");
                    Random r = new Random(DateTime.Now.Minute + DateTime.Now.Second + DateTime.Now.Millisecond);
                    User winner = raffleEntries[r.Next(raffleEntries.Count)];                    
                    return winner.DiscordUser;
                }
                catch (IndexOutOfRangeException) { throw; }
                catch { throw new Exception("Error in processing raffle"); }

            }
            public void ConfirmRaffle(SocketUser winner)
            {
                User u = Users.FirstOrDefault(user => user.DiscordUser.Id == winner.Id) ?? throw new NullReferenceException(message: "User not found in system.");
                u.IsRaffleWinner = true;
            }
            #endregion
        }

        private class PrivateVariable
        {
            public ulong CommandChannel { get; set; } = 0;
            public ulong AnnouncementChannel { get; set; } = 0;
            public SocketTextChannel SocketCommandChannel => Program.Server.GetTextChannel(CommandChannel);
            public SocketTextChannel SocketAnnouncementChannel => Program.Server.GetTextChannel(AnnouncementChannel);
        }

        ///Variables
        private static PrivateVariable PrivateVariables = JsonSerializer.Deserialize<PrivateVariable>(File.ReadAllText("./XPModulePrivateVariables.json")) ?? throw new NullReferenceException("No Private Variable File Found!");
        public XpStorage? xpSystem;
        public XPModule(Program _bot) : base(_bot) { }

        ///Constructor
        public override Task InitilizeModule()
        {
            Bot.AddConnectedCallback(async () =>
            {
                /**
                 * start-event
                 * Starts the event after confirmation, takes an 
                 * event name and will refrence the directory to  
                 * see if the event needs loaded from a crash state.
                 */
                await Bot.AddCommand(new Program.Command()
                {
                    name = "start-event",
                    description = "starts the all-day event.",
                    extendedDecription = "Creates a new or existing event based on the name given. It will run the event to which event attendees can now use the bot.",
                    parameters = "- event-name: Name of the event that will be run. Running the exact name of an already created event will rerun the event",
                    callback = StartEvent,
                    modOnly = true,
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
                    extendedDecription = "Will end a running event, removing commands from the event attendees as well as displaying the top 3 players and general statistics.",
                    callback = EndEvent,
                    modOnly = true,
                });
                /**
                 * draw-raffle
                 * Draws a raffle ticket after confirmation
                 */
                await Bot.AddCommand(new Program.Command()
                {
                    name = "draw-raffle",
                    description = "draws a raffle ticket",
                    extendedDecription = "Takes each user in the event and creates a raffle. Additional tickets that a user has will increase their chances of winning.",
                    callback = DrawRaffle,
                    modOnly = true,
                });
                /**
                 * see-user
                 * Shows all data of a user including private info
                 */
                await Bot.AddCommand(new Program.Command()
                {
                    name = "see-user",
                    description = "view a player's profile",
                    extendedDecription = "Checks a user's profile, listing their points, games played, time spent in the event, and other statistics.",
                    parameters = "- user: User to look through and see all statistics about them",
                    callback = SeeUser,
                    modOnly = true,
                    options = new List<SlashCommandOptionBuilder>() {
                         new SlashCommandOptionBuilder(){
                             Name = "user",
                             Type = ApplicationCommandOptionType.User,
                             Description = "the user to see",
                             IsRequired = true,
                         },
                     }
                });

                /**
                 * see-user-games
                 * show all games of a specified user
                 */
                await Bot.AddCommand(new Program.Command()
                {
                    name = "see-user-games",
                    description = "views a user's completed games",
                    extendedDecription = "Checks a user's completed games, listing all statistics associated with each game.",
                    parameters = "- user: Player to view their completed games",
                    callback = SeeUserGames,
                    modOnly = true,
                    options = new List<SlashCommandOptionBuilder>(){
                        new SlashCommandOptionBuilder(){
                            Name = "user",
                            Type = ApplicationCommandOptionType.User,
                            Description = "the user to be checked",
                            IsRequired = true,
                        }
                    }
                });

                /**
                 * see-user-achievements
                 * show all achievements of a specified user
                 */
                await Bot.AddCommand(new Program.Command()
                {
                    name = "see-user-achievements",
                    description = "views a user's claimed achievements",
                    extendedDecription = "Checks all achievements claimed by a user.",
                    parameters = "- user: Player to view their claimed achievements",
                    callback = SeeUserAchievements,
                    modOnly = true,
                    options = new List<SlashCommandOptionBuilder>(){
                        new SlashCommandOptionBuilder(){
                            Name = "user",
                            Type = ApplicationCommandOptionType.User,
                            Description = "the user to be checked",
                            IsRequired = true,
                        }
                    }
                });

                /**
                 * see-x-users
                 * shows an ephemeral leaderboard of the top x 
                 * users and displays private information
                 */
                await Bot.AddCommand(new Program.Command()
                {
                    name = "see-x-users",
                    description = "shows a leaderboard to you",
                    extendedDecription = "Displays the top specified number of event attendees that are ranking based on the number of points they currently have\n(Note: Points used to purchase tickets WILL decrease your scoring)",
                    parameters = "- number: The number of users to display based on their points ranking in the event",
                    callback = SeeXUsers,
                    modOnly = true,
                    options = new List<SlashCommandOptionBuilder>() {
                        new SlashCommandOptionBuilder(){
                            Name = "number",
                            Type = ApplicationCommandOptionType.Integer,
                            Description = "the number of users to see",
                            IsRequired = true,
                        },
                    }
                });
                /**
                 * remove-user-game
                 * Removes a game from a specific user after confirmation 
                 * *warnning* only do this if a descrepancy is found
                 */
                await Bot.AddCommand(new Program.Command()
                {
                    name = "remove-user-game",
                    description = "removes a game from a user's profile",
                    extendedDecription = "Takes out a user's game from their profile and adjusts their profile accordingly.",
                    parameters = "- user: Player that will have one of their games removed\n- id: the id of the user's game to be removed",
                    callback = RemoveUserGame,
                    modOnly = true,
                    options = new List<SlashCommandOptionBuilder>() {
                        new SlashCommandOptionBuilder(){
                            Name = "user",
                            Type = ApplicationCommandOptionType.User,
                            Description = "the user to remove a game from",
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
                 * remove-user-achievement
                 * Removes an achievement from a specific user after confirmation 
                 * *warning* only do this if a descrepancy is found
                 */
                await Bot.AddCommand(new Program.Command()
                {
                    name = "remove-user-achievement",
                    description = "removes an achievement from a user's profile",
                    extendedDecription = "Unclaims a user's achievement from their profile and adjusts their profile accordingly.",
                    parameters = "- user: Player that will have one of their achievements unclaimed\n- name: the name of the user's achievement to be unclaimed",
                    callback = RemoveUserAchievement,
                    modOnly = true,
                    options = new List<SlashCommandOptionBuilder>() {
                        new SlashCommandOptionBuilder() {
                            Name = "user",
                            Type = ApplicationCommandOptionType.User,
                            Description = "the player to remove an achievement from",
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
                 * remove-user
                 * Removes a user from the event *warning* only do 
                 * this if a descrepancy is found
                 */
                await Bot.AddCommand(new Program.Command()
                {
                    name = "remove-user",
                    description = "removes a user's profile",
                    extendedDecription = "Takes out a user's profile that is currently in the event, removing all data they had.",
                    parameters = "- user: Player to remove from the event",
                    callback = RemoveUser,
                    modOnly = true,
                    options = new List<SlashCommandOptionBuilder>(){
                        new SlashCommandOptionBuilder(){
                            Name = "user",
                            Type = ApplicationCommandOptionType.User,
                            Description = "the player to remove",
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
                    extendedDecription = "Adds you to the currently running event, allowing you to earn points and participate in raffles.",
                    parameters = "- pid: Personal Identification Number assigned to Ohio University students",
                    callback = JoinEvent,
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
                    extendedDecription = "Takes you out of the event and deletes your data from the event.",
                    callback = LeaveEvent,
                });
                /**
                 * see-self
                 * Shows the caller's private information
                 */
                await Bot.AddCommand(new Program.Command()
                {
                    name = "see-self",
                    description = "shows you your stats",
                    extendedDecription = "Look at your PID, number of points earned, number of tickets bought, time played, games played, and raffle earnings.",
                    callback = SeeSelf,
                });
                /**
                 * add-game
                 * Adds a game to the caller's profile
                 */
                await Bot.AddCommand(new Program.Command()
                {
                    name = "add-game",
                    description = "adds a game to your profile",
                    extendedDecription = "Adds a game to your profile and calculate points based on game type, time spent, rank, and number of players.",
                    parameters = "- name: Name of the game that was played\n- player-count: How many players were playing in the game"
                        + "\n- type: The type of game that was played\n> __*ranked*__: Each player plays against each other\n> __*coop*__: All players are working together\n> __*teams*__: Players split up into groups and compete against each other\n> __*party*__: Player join in a casual and fun game"
                        + "\n- placing: Your ranking/placing in the game (if team-based, consider all members as having the same rank unless otherwise noted by the game)\n- time: How long you have played the current game for",
                    callback = AddGame,
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
                            Name = "placing",
                            Type = ApplicationCommandOptionType.Integer,
                            Description = "where you or your team ranked/placed, for unranked games a win is 1 and a loss is 2",
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
                    extendedDecription = "Removes a specified game from your profile based on their id and makes adjustments to your profile as needed.",
                    parameters = "- id: The id number of the game to be removed from your profile\n(Note: You can find the id of the game to remove by using the see-games command)",
                    callback = RemoveGame,
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
                    extendedDecription = "Lists all of the games you have played, displaying each game's id, points, game type, player count, personal ranking, and game length.",
                    callback = SeeGames,
                });
                /**
                 * add-achievement
                 * Adds an achievement to the user's profile
                 */
                await Bot.AddCommand(new Program.Command()
                {
                    name = "add-achievement",
                    description = "adds an achievement to your profile",
                    extendedDecription = "Adds an achievement from the list of available achievements to your profile based on the name given.",
                    parameters = "- name: The name of the achievement to add to your profile]\n(Note: You may find all available achievements by using the see-achievements command)",
                    callback = AddAchievement,
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
                 * remove-achievement
                 * Removes an achievement from the user's profile
                 * after confirmation
                 */
                await Bot.AddCommand(new Program.Command()
                {
                    name = "remove-achievement",
                    description = "removes an achievement from your profile",
                    extendedDecription = "Unclaims an achievment from your profile and adjusts your profile accordingly.",
                    parameters = "- name: The name of the achievement to remove from your profile\n(Note: You may find all claimed achievements by using the see-achievements command)",
                    callback = RemoveAchievement,
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
                    extendedDecription = "Either displays all possible achievements that are active in the event, displays all achievements you have earned at that point, or display a single achievement based on the user's input.",
                    parameters = "- show-all: True will return all achievements listed in the event, false will either display all claimed achievements or a specific achievement\n(Note: Will automatically display all achievements if no input is given) \n- name: the name of the achievement to display\n(Note: Will override show-all if an input is given)",
                    callback = SeeAchievements,
                    options = new List<SlashCommandOptionBuilder>() {
                        new SlashCommandOptionBuilder(){
                            Name = "show-all",
                            Type = ApplicationCommandOptionType.Boolean,
                            Description = "True: shows all achievements | False: shows your completed achievements",
                            IsRequired = true,
                        }
                    },
                });
                /**
                 * buy-tickets
                 * Buys a raffle ticket from the user's points
                 */
                await Bot.AddCommand(new Program.Command()
                {
                    name = "buy-tickets",
                    description = "buy tickets",
                    extendedDecription = "Adds the specified number tickets to your profile and will calculate points as needed. Will not add tickets if required number of points is too low.\n(Note: Points used to purchase tickets WILL decrease your scoring)",
                    parameters = "- tickets: Number of tickets to add to your account. Will not work if you do not have enough points to purchase tickets",
                    callback = BuyTickets,
                    options = new List<SlashCommandOptionBuilder>() {
                        new SlashCommandOptionBuilder(){
                            Name = "tickets",
                            Type = ApplicationCommandOptionType.Integer,
                            Description = "the number of tickets to be bought",
                            IsRequired = true,
                        },
                    },
                });
            });
            return Task.CompletedTask;
        }

        private async Task StartEvent(SocketSlashCommand _command)
        {
            ///Checks if the event has started
            if (xpSystem != null)
                throw new Exception("Error: Event currently running.");

            await Program.Interactions[_command].Respond(components: new ComponentBuilder().WithButton(new Program.Button(_command, "Confirm", ButtonStyle.Danger, async Task(SocketSlashCommand _command) =>
            {
                ///Adds permissions for everyone to input commands
                //*note* uncomment next line when publishing
                //await CommandChannel().AddPermissionOverwriteAsync(Bot.Server().EveryoneRole, OverwritePermissions.DenyAll(CommandChannel()).Modify(viewChannel: PermValue.Allow, useApplicationCommands: PermValue.Allow, sendMessages: PermValue.Allow));

                ///Parses the event name
                string eventName = (string)_command.Data.Options.First().Value;

                ///Check if the event needs loaded
                bool canLoad = XpStorage.CanLoad(eventName);

                ///Creates the event
                xpSystem = new XpStorage(eventName);

                ///Checks if the event was loaded and gives appropriate response
                await PrivateVariables.SocketAnnouncementChannel.SendMessageAsync(text: canLoad ? $"@everyone Thank you for your patience, the event is back up and running!" : $"@everyone Welcome to {xpSystem.EventName}!\nLook to this channel for future updates and visit the {PrivateVariables.SocketCommandChannel.Mention} channel to register youself to this event! (/join-event)\n**Disclaimer**: you need to be a current student at Ohio University and be at the event to recieve any prizes");

                ///User Feedback
                await Program.Interactions[_command].Respond("Successfully started the event.");
            }).GetButton()).Build());
        }

        private async Task EndEvent(SocketSlashCommand _command)
        {
            ///Checks if the event has started
            if (xpSystem == null)
                throw new Exception("Error: No event currently running.");

            await Program.Interactions[_command].Respond(components: new ComponentBuilder().WithButton(new Program.Button(_command, "Confirm", ButtonStyle.Danger, async Task (SocketSlashCommand _command) => {
                ///Sets user permissions for the command channel back to default
                await PrivateVariables.SocketCommandChannel.AddPermissionOverwriteAsync(Program.Server.EveryoneRole, OverwritePermissions.DenyAll(PrivateVariables.SocketCommandChannel));

                ///Logs the end of all day message
                await PrivateVariables.SocketAnnouncementChannel.SendMessageAsync(text: $"@everyone Thank you all for participating in {xpSystem.EventName}!\nWe hope you all had fun, here are the results:\n {xpSystem.GetTopXUsersString(3, true)}\nHere are some statistics for today's event:\n{xpSystem.DisplayAll()}\n\nOnce again thank you all for showing up and we hope to see you at our next event!");

                ///User Feedback
                await Program.Interactions[_command].Respond("Successfully ended the event.");
                xpSystem = null;
            }).GetButton()).Build());
        }

        private async Task DrawRaffle(SocketSlashCommand _command)
        {
            ///Checks if the event has started
            if (xpSystem == null)
                throw new Exception("Error: No event currently running.");

            await Program.Interactions[_command].Respond(components: new ComponentBuilder().WithButton(new Program.Button(_command, "Confirm", ButtonStyle.Danger, async Task (SocketSlashCommand _command) => {
                SocketUser winner = xpSystem.DrawRaffle();

                ///Mod Confirmation
                await Program.Interactions[_command].Respond(text: $"Winner: {Program.Server.GetUser(winner.Id).Nickname}", components: new ComponentBuilder().WithButton(new Program.Button(_command, "Confirm", ButtonStyle.Success, async Task (SocketSlashCommand _command) => {
                    xpSystem.ConfirmRaffle(winner);
                    
                    ///Displays a message notifying everyone of a raffle being drawn
                    await PrivateVariables.SocketAnnouncementChannel.SendMessageAsync(text: $"@everyone Congratulations to {winner.Mention} for winning the raffle!\nMake sure to contact an officer to redeem your prize.");

                    ///User Feedback
                    await Program.Interactions[_command].Respond("Successfully drew a raffle.");
                }).GetButton()).Build());
            }).GetButton()).Build());
        }

        private async Task SeeUser(SocketSlashCommand _command)
        {
            ///Checks if the event has started
            if (xpSystem == null)
                throw new Exception("Error: No event currently running.");

            ///Responds with an ephemeral embed of the info
            await Program.Interactions[_command].Respond(embed: new EmbedBuilder().WithTitle("Player Data").WithDescription(xpSystem.GetUser(((SocketUser)_command.Data.Options.First().Value).Id)).WithColor(Color.Red).Build());
        }

        private async Task SeeUserGames(SocketSlashCommand _command)
        {
            ///Checks if the event has started
            if (xpSystem == null)
                throw new Exception("Error: No event currently running.");

            await Program.RecursiveMuliPageEmbed(_command, xpSystem.GetUserGames(((SocketUser)_command.Data.Options.First().Value).Id),$"{((SocketUser)_command.Data.Options.First().Value).Username}'s Games", color: Color.Red);
        }

        private async Task SeeUserAchievements(SocketSlashCommand _command)
        {
            ///Checks if the event has started
            if (xpSystem == null)
                throw new Exception("Error: No event currently running.");

            await Program.RecursiveMuliPageEmbed(_command, xpSystem.GetUserAchievements(((SocketUser)_command.Data.Options.First().Value).Id),$"{((SocketUser)_command.Data.Options.First().Value).Username}'s Achievements", color: Color.Red);
        }

        private async Task SeeXUsers(SocketSlashCommand _command)
        {
            ///Checks if the event has started
            if (xpSystem == null)
                throw new Exception("Error: No event currently running.");

            try
            {
                ///Gets X
                int numberOfUsers = Convert.ToInt32(_command.Data.Options.First().Value);

                ///Displays the leaderboard
                await Program.Interactions[_command].Respond(embed: new EmbedBuilder().WithTitle($"Top {numberOfUsers} Users").WithDescription(xpSystem.GetTopXUsersString(numberOfUsers)).WithColor(Color.Orange).Build());
            }
            catch { throw; }
        }

        private async Task RemoveUserGame(SocketSlashCommand _command)
        {
            ///Checks if the event has started
            if (xpSystem == null)
                throw new Exception("Error: No event currently running.");

            await Program.Interactions[_command].Respond(components: new ComponentBuilder().WithButton(new Program.Button(_command, "Confirm", ButtonStyle.Danger, async Task (SocketSlashCommand _command) => {
                ///Remove the game
                EmbedBuilder displayGame = xpSystem.RemoveUserGame(((SocketUser)_command.Data.Options.First().Value).Id, Convert.ToInt32(_command.Data.Options.ElementAt(1).Value));

                ///User Feedback
                await Program.Interactions[_command].Respond(embed: displayGame.WithTitle($"Successfully removed {((SocketUser)_command.Data.Options.First().Value).Username}'s game").WithColor(Color.Red).Build());
            }).GetButton()).Build());
        }

        private async Task RemoveUserAchievement(SocketSlashCommand _command)
        {
            ///Checks if the event has started
            if (xpSystem == null)
                throw new Exception("Error: No event currently running.");

            await Program.Interactions[_command].Respond(components: new ComponentBuilder().WithButton(new Program.Button(_command, "Confirm", ButtonStyle.Danger, async Task (SocketSlashCommand _command) => {
                ///Remove the achievement
                EmbedBuilder displayAchievement = xpSystem.UnclaimUserAchievement(((SocketUser)_command.Data.Options.First().Value).Id, (string)_command.Data.Options.ElementAt(1).Value);

                ///User Feedback
                await Program.Interactions[_command].Respond(embed: displayAchievement.WithTitle($"Successfully removed {((SocketUser)_command.Data.Options.First().Value).Username}'s achievement").WithColor(Color.Red).Build());
            }).GetButton()).Build());
        }

        private async Task RemoveUser(SocketSlashCommand _command)
        {
            ///Checks if the event has started
            if (xpSystem == null)
                throw new Exception("Error: No event currently running.");

            await Program.Interactions[_command].Respond(components: new ComponentBuilder().WithButton(new Program.Button(_command, "Confirm", ButtonStyle.Danger, async Task (SocketSlashCommand _command) => {
                ///Remove user
                xpSystem.RemoveUser(((SocketUser)_command.Data.Options.First().Value).Id);

                ///User Feedback
                await Program.Interactions[_command].Respond("Successfully removed player.");
            }).GetButton()).Build());
        }

        private async Task JoinEvent(SocketSlashCommand _command)
        {
            ///Checks if the event has started
            if (xpSystem == null)
                throw new Exception("Error: No event currently running.");

            ///Adds the user
            xpSystem.AddNewUser(_command.User, ((string)_command.Data.Options.First().Value).ToUpper());

            ///User Feedback
            await Program.Interactions[_command].Respond("Successfully joined event.");
        }

        private async Task LeaveEvent(SocketSlashCommand _command)
        {
            ///Checks if the event has started
            if (xpSystem == null)
                throw new Exception("Error: No event currently running.");

            await Program.Interactions[_command].Respond(components: new ComponentBuilder().WithButton(new Program.Button(_command, "Confirm", ButtonStyle.Danger, async Task (SocketSlashCommand _command) => {
                ///Removes user
                xpSystem.RemoveUser(_command.User.Id);

                ///User Feedback
                await Program.Interactions[_command].Respond("Successfully left event.");
            }).GetButton()).Build());
        }

        private async Task SeeSelf(SocketSlashCommand _command)
        {
            ///Checks if the event has started
            if (xpSystem == null)
                throw new Exception("Error: No event currently running.");

            ///Shows the caller's information
            await Program.Interactions[_command].Respond(embed: new EmbedBuilder().WithTitle("Your Data").WithDescription(xpSystem.GetUser(_command.User.Id).ToString()).WithColor(Color.Blue).Build());
        }

        private async Task AddGame(SocketSlashCommand _command)
        {
            ///Checks if the event has started
            if (xpSystem == null)
                throw new Exception("Error: No event currently running.");

            ///Get game data
            string gameName = (string)_command.Data.Options.First().Value;
            uint playerCount = Convert.ToUInt32(_command.Data.Options.ElementAt(1).Value);
            GameType type = (_command.Data.Options.ElementAt(2).Value as string) switch { "ranked" => GameType.Ranked, "coop" => GameType.CoOp, "teams" => GameType.Teams, "party" => GameType.Party, _ => throw new InvalidDataException(message: "Invalid Game Type.") };
            uint rank = Convert.ToUInt32(_command.Data.Options.ElementAt(3).Value);
            uint time = Convert.ToUInt32(_command.Data.Options.ElementAt(4).Value);

            ///Adds the game
            EmbedBuilder displayGame = xpSystem.AddUserGame(_command.User.Id, gameName, playerCount, type, rank, time);

            ///User Feedback
            await Program.Interactions[_command].Respond(embed: displayGame.WithTitle("Successfully added game.").WithColor(Color.Blue).Build());
        }

        private async Task RemoveGame(SocketSlashCommand _command)
        {
            ///Checks if the event has started
            if (xpSystem == null)
                throw new Exception("Error: No event currently running.");

            await Program.Interactions[_command].Respond(components: new ComponentBuilder().WithButton(new Program.Button(_command, "Confirm", ButtonStyle.Danger, async Task(SocketSlashCommand _command) => {
                ///Remove self
                EmbedBuilder displayGame = xpSystem.RemoveUserGame(_command.User.Id, Convert.ToInt32(_command.Data.Options.First().Value));

                ///User Feedback
                await Program.Interactions[_command].Respond(embed: displayGame.WithTitle("Successfully removed game.").WithColor(Color.Blue).Build());
            }).GetButton()).Build());
        }

        private async Task SeeGames(SocketSlashCommand _command)
        {
            ///Checks if the event has started
            if (xpSystem == null)
                throw new Exception("Error: No event currently running.");

            await Program.RecursiveMuliPageEmbed(_command, xpSystem.GetUserGames(_command.User.Id), "Your Games");
        }

        private async Task AddAchievement(SocketSlashCommand _command)
        {

            ///Checks if the event has started
            if (xpSystem == null)
                throw new Exception("Error: No event currently running.");

            ///Adds the achievement
            EmbedBuilder displayAchievement = xpSystem.ClaimUserAchievement(_command.User.Id, (string)_command.Data.Options.First().Value);

            ///User Feedback
            await Program.Interactions[_command].Respond(embed: displayAchievement.WithTitle("Successfully claimed achievement").WithColor(Color.Blue).Build());
        }

        private async Task RemoveAchievement(SocketSlashCommand _command)
        {
            

            ///Checks if the event has started
            if (xpSystem == null)
                throw new Exception("Error: No event currently running.");

            await Program.Interactions[_command].Respond(components: new ComponentBuilder().WithButton(new Program.Button(_command, "Confirm", ButtonStyle.Danger, async Task (SocketSlashCommand _command) => {
                ///Remove achievement
                EmbedBuilder achievementUnclaimed = xpSystem.UnclaimUserAchievement(_command.User.Id, (string)_command.Data.Options.First().Value);
                
                ///User Feedback
                await Program.Interactions[_command].Respond(embed: achievementUnclaimed.WithTitle("Successfully removed achievement.").WithColor(Color.Blue).Build());
            }).GetButton()).Build());
            
        }

        private async Task SeeAchievements(SocketSlashCommand _command)
        {
            ///Checks if the event has started
            if (xpSystem == null)
                throw new Exception("Error: No event currently running.");

            await Program.RecursiveMuliPageEmbed(_command, xpSystem.GetUserAchievements(_command.User.Id, (bool)_command.Data.Options.First()), (bool)_command.Data.Options.First() ? "List of Achievements" : "Your Achievements");
        }

        private async Task BuyTickets(SocketSlashCommand _command)
        {
            ///Checks if the event has started
            if (xpSystem == null)
                throw new Exception("Error: No event currently running.");

            await Program.Interactions[_command].Respond(components: new ComponentBuilder().WithButton(new Program.Button(_command, "Confirm", ButtonStyle.Danger, async Task (SocketSlashCommand _command) => {
                ///Get the information
                int ticketsBought = Convert.ToInt32(_command.Data.Options.First().Value);
                ulong user = _command.User.Id;

                ///Buy the tickets
                xpSystem.UserBuyTickets(user, (uint)ticketsBought);

                ///User Feedback
                await Program.Interactions[_command].Respond($"Successfully bought {_command.Data.Options.First().Value} tickets.");
            }).GetButton()).Build());
        }

    }
}