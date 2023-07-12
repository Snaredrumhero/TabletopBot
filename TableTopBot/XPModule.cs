using Discord;
using Discord.WebSocket;
using System.Text.Json;

namespace TableTopBot
{
    internal class XPModule : Module
    {
        public enum GameType { Ranked = 1, CoOp = 2, Teams = 3, Party = 4 } ///Represents the types of games
        static Func<List<Tuple<String,EmbedBuilder>>> HelpListExpandedBuilder = () =>
        {
            List<Tuple<String,EmbedBuilder>> g = new List<Tuple<String,EmbedBuilder>>();
            
            g.Add(new Tuple<string, EmbedBuilder>("join-event", new EmbedBuilder().WithTitle("Player Commands").AddField("**join-event**",
            "Adds you to the currently running event, allowing you to earn points and participate in raffles."
            + "\n\n__Parameters__\n- pid: Personal Identification Number assigned to Ohio University students"
            )));
            
            g.Add(new Tuple<string, EmbedBuilder>("leave-event", new EmbedBuilder().WithTitle("Player Commands").AddField("**leave-event**",
            "Takes you out of the event and deletes your data from the event."
            )));
            
            g.Add(new Tuple<string, EmbedBuilder>("see-self", new EmbedBuilder().WithTitle("Player Commands").AddField("**see-self**",
            "Look at your PID, number of points earned, number of tickets bought, time played, games played, and raffle earnings."
            )));
            
            g.Add(new Tuple<string, EmbedBuilder>("add-game", new EmbedBuilder().WithTitle("Player Commands").AddField("**add-game**",
            "Adds a game to your profile and calculate points based on game type, time spent, rank, and number of players."
            + "\n\n__Parameters__\n- name: Name of the game that was played\n- player-count: How many players were playing in the game"
            + "\n- type: The type of game that was played\n> __*ranked*__: Each player plays against each other\n> __*coop*__: All players are working together\n> __*teams*__: Players split up into groups and compete against each other\n> __*party*__: Player join in a casual and fun game"
            + "\n- placing: Your ranking/placing in the game (if team-based, consider all members as having the same rank unless otherwise noted by the game)\n- time: How long you have played the current game for"
            )));
            
            g.Add(new Tuple<string, EmbedBuilder>("remove-game", new EmbedBuilder().WithTitle("Player Commands").AddField("remove-game",
            "Removes a specified game from your profile based on their id and makes adjustments to your profile as needed."
            + "\n\n__Parameters__\n- id: The id number of the game to be removed from your profile\n(Note: You can find the id of the game to remove by using the see-games command)"
            )));
            
            g.Add(new Tuple<string, EmbedBuilder>("see-games", new EmbedBuilder().WithTitle("Player Commands").AddField("see-games",
            "Lists all of the games you have played, displaying each game's id, points, game type, player count, personal ranking, and game length."
            )));
            
            g.Add(new Tuple<string, EmbedBuilder>("add-achievement", new EmbedBuilder().WithTitle("Player Commands").AddField("add-achievement",
            "Adds an achievement from the list of available achievements to your profile based on the name given."
            + "\n\n__Parameters__\n- name: The name of the achievement to add to your profile]\n(Note: You may find all available achievements by using the see-achievements command)"
            )));
            
            g.Add(new Tuple<string, EmbedBuilder>("remove-achievement", new EmbedBuilder().WithTitle("Player Commands").AddField("remove-achievement",
            "Unclaims an achievment from your profile and adjusts your profile accordingly."
            + "\n\n__Parameters__\n- name: The name of the achievement to remove from your profile\n(Note: You may find all claimed achievements by using the see-achievements command)"
            )));
            
            g.Add(new Tuple<string, EmbedBuilder>("see-achievements", new EmbedBuilder().WithTitle("Player Commands").AddField("see-achievements",
            "Either displays all possible achievements that are active in the event, displays all achievements you have earned at that point, or display a single achievement based on the user's input."
            + "\n\n__Parameters__\n- show-all: True will return all achievements listed in the event, false will either display all claimed achievements or a specific achievement\n(Note: Will automatically display all achievements if no input is given) \n- name: the name of the achievement to display\n(Note: Will override show-all if an input is given)"
            )));
            
            g.Add(new Tuple<string, EmbedBuilder>("buy-tickets", new EmbedBuilder().WithTitle("Player Commands").AddField("buy-tickets",
            "Adds the specified number tickets to your profile and will calculate points as needed. Will not add tickets if required number of points is too low.\n(Note: Points used to purchase tickets WILL decrease your scoring)"
            + "\n\n__Parameters__\n- tickets: Number of tickets to add to your account. Will not work if you do not have enough points to purchase tickets"
            )));
            
            g.Add(new Tuple<string, EmbedBuilder>("show-x-users", new EmbedBuilder().WithTitle("Player Commands").AddField("show-x-users",
            "Displays the top specified number of event attendees that are ranking based on the number of points they currently have\n(Note: Points used to purchase tickets WILL decrease your scoring)"
            + "\n\n__Parameters__\n- number: The number of users to display based on their points ranking in the event"
            )));
            
            
            
            
            g.Add(new Tuple<string, EmbedBuilder>("start-event", new EmbedBuilder().WithTitle("Admin Commands").AddField("start-event",
            "Creates a new or existing event based on the name given. It will run the event to which event attendees can now use the bot."
            + "\n\n__Parameters__\n- event-name: Name of the event that will be run. Running the exact name of an already created event will rerun the event"
            )));
            
            g.Add(new Tuple<string, EmbedBuilder>("end-event", new EmbedBuilder().WithTitle("Admin Commands").AddField("end-event",
            "Will end a running event, removing commands from the event attendees as well as displaying the top 3 players and general statistics."
            )));
            
            g.Add(new Tuple<string, EmbedBuilder>("draw-raffle",new EmbedBuilder().WithTitle("Admin Commands").AddField("draw-raffle",
            "Takes each user in the event and creates a raffle. Additional tickets that a user has will increase their chances of winning."
            )));
            
            g.Add(new Tuple<string, EmbedBuilder>("see-player", new EmbedBuilder().WithTitle("Admin Commands").AddField("see-player",
            "Checks a user's profile, listing their points, games played, time spent in the event, and other statistics."
            + "\n\n__Parameters__\n- player: User to look through and see all statistics about them"
            )));
            
            g.Add(new Tuple<string, EmbedBuilder>("see-player-games", new EmbedBuilder().WithTitle("Admin Commands").AddField("see-player-games",
            "Checks a user's completed games, listing all statistics associated with each game."
            + "\n\n__Parameters__\n- player: User to view their completed games"
            )));
            
            g.Add(new Tuple<string, EmbedBuilder>("see-player-achievements", new EmbedBuilder().WithTitle("Admin Commands").AddField("see-player-achievements",
            "Checks all achievements claimed by a user."
            + "\n\n__Parameters__\n- player: User to view their claimed achievements"
            )));
            
            g.Add(new Tuple<string, EmbedBuilder>("remove-player", new EmbedBuilder().WithTitle("Admin Commands").AddField("remove-player",
            "Takes out a user's profile that is currently in the event, removing all data they had."
            + "\n\n__Parameters__\n- player: User to remove from the event"
            )));
            
            g.Add(new Tuple<string, EmbedBuilder>("remove-player-game", new EmbedBuilder().WithTitle("Admin Commands").AddField("remove-player-game",
            "Takes out a user's game from their profile and adjusts their profile accordingly."
            + "\n\n__Parameters__\n- player: User that will have one of their games removed\n- id: the id of the user's game to be removed"
            )));
            
            g.Add(new Tuple<string, EmbedBuilder>("remove-player-achievement", new EmbedBuilder().WithTitle("Admin Commands").AddField("remove-player-achievement",
            "Unclaims a user's achievement from their profile and adjusts their profile accordingly."
            + "\n\n__Parameters__\n- player: User that will have one of their achievements unclaimed\n- name: the name of the user's achievement to be unclaimed"
            )));
            
            return g;
        };
        
        static Func<EmbedBuilder> HelpListBuilder = () =>
        {       
            EmbedBuilder g = new EmbedBuilder().WithTitle("**All Commands**").AddField("__**Player Commands**__",
                "- **join-event**\n> registers you for the current event\n"
                + "\n- **leave-event**\n> unregisters you for the current event\n"
                + "\n- **see-self**\n> shows you your stats\n"
                + "\n- **add-game**\n> adds a game to your profile\n"
                + "\n- **remove-game**\n> removes a game from your profile\n"
                + "\n- **see-games**\n> shows your completed games\n"
                + "\n- **add-achievement**\n> adds an achievement to your profile\n"
                + "\n- **remove-achievement**\n> removes an achievement from your profile\n"
                + "\n- **see-achievements**\n> shows achievements you have completed or all available achievements\n"
                + "\n- **buy-tickets**\n> buy tickets and add them to your profile\n"
                + "\n- **show-x-users**\n> shows a leaderboard of top x users\n", false)
                
                .AddField("__**Admin Commands**__",
                "- **start-event**\n> starts the all-day event\n"
                + "\n- **end-event**\n> ends the all-day event\n"
                + "\n- **draw-raffle**\n> draws araffle ticket\n"
                + "\n- **see-player**\n> views a player's profile\n"
                + "\n- **see-player-games**\n> views a player's completed games\n"
                + "\n- **see-player-achievements**\n> views a player's claimed achievements\n"
                + "\n- **remove-player**\n> removes a player's profile\n"
                + "\n- **remove-player-game**\n> removes a game from a player's profile\n"
                + "\n- **remove-player-achievement**\n> removes an achievement from a player's profile\n", false
            ); 
            
            return g;
            
        };
        
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
                private readonly List<string> _achievementsClaimed;         ///The list of all possible achievements a user can get
                private readonly List<Game> _gamesPlayed;                   ///The games the user has played in
                public SocketUser DiscordUser;                              ///The user's discord
                public readonly string Pid;                                 ///The user's Ohio University ID
                public bool IsRaffleWinner;                                 ///If the user has won a raffle prize
                private ushort NumberGamesPlayed;                           ///only used for tracking game ids
                public MultiPageEmbed? PageEmbed;                           ///Used to create embed pages for user's games and achievements
                public int BoughtTickets;                                   ///Tickets that are bought with the user's points
                public DateTime StartTime;                                  ///The time that they have logged into the event
                public TimeSpan CurrentTime => DateTime.Now - StartTime;    ///The total time a user has spent in the event 
                
                
                ///Tells the current points a user has by using the maximum points and the tickets bought by the user
                public int CurrentPoints() { return TotalPoints() - (BoughtTickets * TicketPrice); } 
            
                ///Constructor
                public User(SocketUser user, string pid, List<Game>? gamesPlayed = null, bool isRaffleWinner = false, ushort numberGamesPlayed = 0, List<string>? achievements = null, int tickets = 0, DateTime startTime = new DateTime())
                {
                    DiscordUser = user;
                    Pid = pid;
                    _gamesPlayed = gamesPlayed == null ? new List<Game>() : gamesPlayed;
                    IsRaffleWinner = isRaffleWinner;
                    NumberGamesPlayed = numberGamesPlayed;
                    _achievementsClaimed = achievements ?? new List<string>();
                    BoughtTickets = tickets;
                    StartTime = startTime;
                }
                private User(ulong id, string pid, List<Game>? gamesPlayed = null, bool isRaffleWinner = false, ushort numberGamesPlayed = 0, List<string>? achievements = null, int tickets = 0, DateTime startTime = new DateTime()) : this(Program.Server().GetUser(id), pid, gamesPlayed, isRaffleWinner, numberGamesPlayed, achievements, tickets, startTime) { }

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
                public override string ToString() { return $"__**{DiscordUser.Username}**__\n- PID: {Pid}\n- Points: {CurrentPoints()}\n- Claimed Raffle: {IsRaffleWinner}\n- Bought Tickets: {BoughtTickets}\n- Time Played: {Math.Round(CurrentTime.TotalMinutes,2)} Minutes\n- Games Played: {_gamesPlayed.Count}"; }
                public List<EmbedBuilder> ShowGames(int list_games = Int32.MinValue)
                {
                    List<EmbedBuilder> embedlist = new List<EmbedBuilder>();
                    string gamelist = "";
                    if (list_games == Int32.MinValue)
                    {
                        int totalPages = _gamesPlayed.Count()/5 + (_gamesPlayed.Count%5 == 0 ? 0:1);
                        for (int i = 0; i < _gamesPlayed.Count(); ++i)
                        {
                            gamelist += (_gamesPlayed[i].ToString() + "\n\n");
                            if ((i + 1) % 5 == 0)
                            {
                                embedlist.Add(new EmbedBuilder().WithColor(Color.Blue).WithTitle("Your Games").WithDescription(gamelist));
                                gamelist = "";
                            }
                        }
                        if (!string.IsNullOrEmpty(gamelist))
                            embedlist.Add(new EmbedBuilder().WithColor(Color.Blue).WithTitle("Your Games").WithDescription(gamelist));
                    }
                    else
                    {
                        gamelist += _gamesPlayed.FirstOrDefault(game => game.Data.Id == list_games)!.ToString() ?? throw new Exception("Cannot find game");
                        embedlist.Add(new EmbedBuilder().WithColor(Color.Blue).WithFooter("Page 1/1").WithCurrentTimestamp().WithTitle("Found Game").WithDescription(gamelist));
                    }
                    //return gamelist;
                    return embedlist;
                }
                public List<EmbedBuilder> ShowAchievements(bool showAll = false, string? name = null)
                {
                    List<EmbedBuilder> embedlist = new List<EmbedBuilder>();
                    string achievementlist = "";

                    if(String.IsNullOrEmpty(name))
                    {
                        if(showAll)
                        {
                            int totalPages = DefaultAchievements.Count/5 + (DefaultAchievements.Count%5 == 0 ? 0:1);
                            for (int i = 0; i < DefaultAchievements.Count; ++i)
                            {
                                achievementlist += $"{DefaultAchievements[i]}\nClaimed: {_achievementsClaimed.Contains(DefaultAchievements[i].Data.Name)}\n\n";
                                if ((i + 1) % 5 == 0)
                                {
                                    embedlist.Add((new EmbedBuilder().WithColor(Color.Blue).WithTitle("List of Achievements").WithDescription(achievementlist)));
                                    achievementlist = "";
                                }
                            }
                            if (!string.IsNullOrEmpty(achievementlist))
                                embedlist.Add(new EmbedBuilder().WithColor(Color.Blue).WithTitle("List of Achievements").WithDescription(achievementlist)); 
                        }
                        else
                        {
                            int totalPages = Achievements.Count/5 + (Achievements.Count%5 == 0 ? 0:1);
                            for (int i = 0; i < Achievements.Count; ++i)
                            {
                                achievementlist += $"{Achievements[i]}\n\n";
                                if ((i + 1) % 5 == 0)
                                {
                                    embedlist.Add((new EmbedBuilder().WithColor(Color.Blue).WithTitle("Your Achievements").WithDescription(achievementlist)));
                                    achievementlist = "";
                                }
                            }
                            if (!string.IsNullOrEmpty(achievementlist))
                                embedlist.Add((new EmbedBuilder().WithColor(Color.Blue).WithTitle("Your Achievements").WithDescription(achievementlist)));
                        }
                    }
                    else
                    {
                        achievementlist += Achievements.FirstOrDefault(achievement => achievement.Data.Name == name)!.ToString() ?? throw new ArgumentException("Achievement Not Found");
                        embedlist.Add((new EmbedBuilder().WithColor(Color.Blue).WithFooter("Page 1/1").WithCurrentTimestamp().WithTitle("Achievement Found").WithDescription(achievementlist)));
                    }
                    
                    return embedlist;
                }

                ///Operators
                public static bool operator >(User a, User b) => a.TotalPoints() > b.TotalPoints();
                public static bool operator <(User a, User b) => a.TotalPoints() < b.TotalPoints();
                public static implicit operator UserData(User u) => new UserData(u.DiscordUser.Id, u.Pid, u.GamesPlayed.Select(g => (GameData)g).ToArray(), u.IsRaffleWinner, u._achievementsClaimed.ToArray(), u.NumberGamesPlayed, u.BoughtTickets, u.StartTime);
                public static implicit operator User(UserData u) => new User(u.DiscordId, u.PID, u.GamesPlayed.Select(g => (Game)g).ToList(), u.WonRaffle, u.NumberGamesPlayed, u.AchievementsClaimed.ToList(), u.Tickets, u.Start);
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
                public DateTime Start;

                public UserData(ulong discordId, string pID, GameData[] gamesPlayed, bool wonRaffle, string[] achievementsClaimed, ushort numberGamesPlayed, int tickets, DateTime start)
                {
                    DiscordId = discordId;
                    PID = pID;
                    GamesPlayed = gamesPlayed;
                    WonRaffle = wonRaffle;
                    AchievementsClaimed = achievementsClaimed;
                    NumberGamesPlayed = numberGamesPlayed;
                    Tickets = tickets;
                    Start = start;
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
                public override string ToString() => $"**{Data.Name}**\n- ID: {Data.Id}\n- Game Type: {Data.Type}\n- Player Count: {Data.PlayerCount}\n- Ranking: {Data.Rank}\n- Game Length: {Data.GameLength} min\n- Points: {XpValue}\n";

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
                public override string ToString() { return $"**{Data.Name}**\n- Description: {Data.Description}\n- Points: {Data.XpValue}"; }

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
                    Users.Add(new User(addedUser, pid, startTime: DateTime.Now));
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
            
            //Lists all players and statistics
            public string DisplayAll()
            {
                
                string displayAll = "List of all players: \n\n";
                double averageTime = 0;
                int averageGames = 0;
                int averageXP = 0;
                int totalUsers = Users.Count;
                foreach(User u in Users)
                {
                    displayAll += $"{u.ToString()}\n";
                    averageTime += u.CurrentTime.TotalMinutes;
                    averageGames += u.GamesPlayed.Count;
                    averageXP += u.TotalPoints();
                }
                displayAll += $"\nStatistics:\n\nAverage Time Spent: {Math.Round(averageTime/totalUsers,2)} Minutes\nAverage Number of Games Played: {averageGames/totalUsers}\nAverage Amount of XP Earned: {averageXP/totalUsers}\nTotal Attendees: {totalUsers}";
                
                return displayAll;
            }

            ///Private Functions
            public async Task Save() => await File.WriteAllTextAsync(Path, JsonSerializer.Serialize(Users.Select(u => (UserData)u), JsonOptions));
        }

        ///Variables
        private static PrivateVariable? PrivateVariables = JsonSerializer.Deserialize<PrivateVariable>(File.ReadAllText("./PrivateVariables.json"));
        public static SocketTextChannel AnnouncementChannel() => Program.Server().GetTextChannel(PrivateVariables!.AnnouncementChannel);
        public static SocketTextChannel CommandChannel() => Program.Server().GetTextChannel(PrivateVariables!.CommandChannel);
        public XpStorage? xpSystem;
        public XPModule(Program _bot) : base(_bot) { }

        ///Constructor
        public override Task InitilizeModule()
        {
            Bot.AddConnectedCallback(async () =>
            {
                await Bot.AddCommand(new Program.Command()
                {
                    name = "help",
                    description = "show all commands available and explain them in detail",
                    callback = async (SocketSlashCommand _command) =>
                    {
                        try
                        {
                            
                            SocketGuildUser user = (SocketGuildUser) _command.User;
                            string? showCommand = null;
                            foreach (SocketSlashCommandDataOption? option in _command.Data.Options)
                            {
                                if (option is null)
                                    continue;
                                showCommand = (string) option.Value;
                            }
                            
                            if(String.IsNullOrEmpty(showCommand))
                            {
                                await _command.RespondAsync(embed: HelpListBuilder.Invoke().WithColor(Color.Red).Build(), ephemeral: true);
                            }
                            else
                            {
                                EmbedBuilder commandFound = HelpListExpandedBuilder.Invoke().Find(e => e.Item1 == (string) _command.Data.Options.First().Value )!.Item2 ?? throw new Exception("Command does not exist");
                                
                                await _command.RespondAsync(embed: commandFound.WithColor(Color.Red).Build(), ephemeral: true);
                            }                 

                        }
                        catch { throw; }
                    },
                    options = new List<SlashCommandOptionBuilder>() {
                        new SlashCommandOptionBuilder(){
                            Name = "command",
                            Type = ApplicationCommandOptionType.String,
                            Description = "provide more details about given comand",
                            IsRequired = false,
                        }
                            .AddChoice("join-event", "join-event")
                            .AddChoice("leave-event", "leave-event")
                            .AddChoice("see-self", "see-self")
                            .AddChoice("add-game","add-game")
                            .AddChoice("remove-game", "remove-game")
                            .AddChoice("see-games","see-games")
                            .AddChoice("add-achievement", "add-achievement")
                            .AddChoice("remove-achievement", "remove-achievement")
                            .AddChoice("see-achievements", "see-achievements")
                            .AddChoice("buy-tickets", "buy-tickets")
                            .AddChoice("show-x-users", "show-x-users")
                            .AddChoice("start-event", "start-event")
                            .AddChoice("end-event", "end-event")
                            .AddChoice("draw-raffle", "draw-raffle")
                            .AddChoice("see-player", "see-player")
                            .AddChoice("see-player-games", "see-player-games")
                            .AddChoice("see-player-achievements", "see-player-achievements")
                            .AddChoice("remove-player", "remove-player")
                            .AddChoice("remove-player-game", "remove-player-game")
                            .AddChoice("remove-player-achievement", "remove-player-achievement"),
                            
                            
                    },
                });
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
                        
                        await AnnouncementChannel().SendMessageAsync(text: xpSystem.DisplayAll());
                        ///Saves changes
                        await xpSystem.Save();

                        xpSystem = null;
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

                        XpStorage.User user = xpSystem.GetUser( ((SocketUser) _command.Data.Options.First().Value).Id);
                        ///Responds with an ephemeral embed of the info
                        //*note* this will be ephemeral, and will show private data
                        await _command.RespondAsync(embed: (new EmbedBuilder().WithTitle($"{user.DiscordUser.Username}'s Data").WithDescription(user.ToString()))
                        .WithFooter("Page 1/1").WithColor(Color.Blue).WithCurrentTimestamp().Build(), ephemeral: true);
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
                 * see-player-games
                 * show all games of a specified user
                 */
                await Bot.AddCommand(new Program.Command()
                {
                    name = "see-player-games",
                    description = "views a player's completed games",
                    callback = async (SocketSlashCommand _command) =>
                    {
                        ///Checks if the event has started
                        if (xpSystem == null)
                            throw new Exception("Error: No event currently running.");

                        try
                        {
                            ///Gets users
                            XpStorage.User user = xpSystem.GetUser(((SocketUser) _command.Data.Options.First().Value).Id);
                            XpStorage.User adminUser = xpSystem.GetUser(_command.User.Id);
                            
                            ///Gets data and creates embed
                            List<EmbedBuilder> gamelist = user.ShowGames();
                            
                            foreach(EmbedBuilder g in gamelist)
                                g.WithTitle($"{user.DiscordUser.Username}'s Games");
                            
                            
                            ///Sets up MultiPageEmbed
                            adminUser.PageEmbed = new MultiPageEmbed(gamelist);
                            
                            ///Display's games
                            await adminUser.PageEmbed.StartPage(_command);
                        }
                        catch { throw; }
                    },
                    modOnly = true,
                    options = new List<SlashCommandOptionBuilder>(){
                        new SlashCommandOptionBuilder(){
                            Name = "player",
                            Type = ApplicationCommandOptionType.User,
                            Description = "the user to be checked",
                            IsRequired = true,
                        }
                    }
                });
                
                /**
                 * see-player-achievements
                 * show all achievements of a specified user
                 */
                await Bot.AddCommand(new Program.Command()
                {
                    name = "see-player-achievements",
                    description = "views a player's claimed achievements",
                    callback = async (SocketSlashCommand _command) =>
                    {
                        ///Checks if the event has started
                        if (xpSystem == null)
                            throw new Exception("Error: No event currently running.");

                        try
                        {
                            ///Gets data
                            XpStorage.User user = xpSystem.GetUser( ((SocketUser) _command.Data.Options.First().Value).Id);
                            XpStorage.User adminUser = xpSystem.GetUser(_command.User.Id);
                            List<EmbedBuilder> achievementlist = new List<EmbedBuilder>();

                            ///Create embed
                            achievementlist = user.ShowAchievements(showAll: false);
                            
                            foreach(EmbedBuilder g in achievementlist)
                                g.WithTitle($"{user.DiscordUser.Username}'s Achievements");
                                
                            ///Display achievements
                            adminUser.PageEmbed = new MultiPageEmbed(achievementlist);
                            await adminUser.PageEmbed.StartPage(_command);
                        }
                        catch { throw; }
                    },
                    modOnly = true,
                    options = new List<SlashCommandOptionBuilder>(){
                        new SlashCommandOptionBuilder(){
                            Name = "player",
                            Type = ApplicationCommandOptionType.User,
                            Description = "the user to be checked",
                            IsRequired = true,
                        }
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
                    description = "shows a leaderboard of top x users",
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
                            await _command.RespondAsync(embed: new EmbedBuilder().AddField($"Top {top.Count} Users", output)
                            .WithColor(Color.Red).WithFooter("Page 1/1").WithCurrentTimestamp().Build(), ephemeral: true);
                        }
                        catch { throw; }
                    },
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
                            Name = "player",
                            Type = ApplicationCommandOptionType.User,
                            Description = "the user to remove an achievement from",
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
                            await _command.RespondAsync(embed: new EmbedBuilder().WithTitle("Your Data").WithDescription(xpSystem.GetUser(_command.User.Id).ToString())
                            .WithColor(Color.Blue).WithFooter("Page 1/1").WithCurrentTimestamp().Build(), ephemeral: true);
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
                            Name = "placing",
                            Type = ApplicationCommandOptionType.Integer,
                            Description = "where you or your team ranked/placed",
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
                            user.PageEmbed = new MultiPageEmbed(gamelist);
                            
                            ///Display's games
                            await user.PageEmbed.StartPage(_command);
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
                            bool isShowAll = true;
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
                            user.PageEmbed = new MultiPageEmbed(achievementlist);
                            await user.PageEmbed.StartPage(_command);
                        }
                        catch { throw; }
                    },
                    options = new List<SlashCommandOptionBuilder>() {
                        new SlashCommandOptionBuilder(){
                            Name = "show-all",
                            Type = ApplicationCommandOptionType.Boolean,
                            Description = "True: shows all achievements | False: shows your completed achievements",
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
                    description = "buy tickets and add them to your profile",
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