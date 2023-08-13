using System;

namespace TableTopBot
{
    internal class XPModule
    {
        private enum GameType { NULL = 0, Ranked = 1, CoOp = 2, Teams = 3, Party = 4 }

        private class XpStorage
        {
            public static bool CanLoad(string eventName) => File.Exists($"./events/{eventName}.json");
            public readonly string EventName;
            private string Path => $"./events/{EventName}.json";
            public XpStorage(string eventName)
            {
                EventName = eventName;
                if (File.Exists(Path))
                    using (FileStream stream = new FileStream(Path, FileMode.Open))
                        Users = (JsonSerializer.Deserialize<UserData[]>(stream, JsonOptions) ?? new UserData[0]).Select(u => (User)u).ToList();
                else Users = new List<User>();
                Save();
                SimpleLog("XP System Initalized");
            }
            private void Save() => File.WriteAllText(Path, JsonSerializer.Serialize(Users.Select(u => (UserData)u), JsonOptions));
            public string DisplayAll() => $"***Average Time Spent:*** {Math.Round((double)Users.Sum(u => u.CurrentTime) / Users.Count, 2)} Minutes\n***Average Number of Games Played:*** {Math.Round((double)Users.Sum(u => u.GamesPlayed.Count) / Users.Count, 2)}\n***Average Amount of XP Earned:*** {Math.Round((double)Users.Sum(u => u.TotalPoints) / Users.Count, 2)}\n***Total Attendees:*** {Users.Count}";

            #region Users
            private class User : IComparable<User>
            {
                ///Variables
                private readonly List<string> _achievementsClaimed; ///The list of all possible achievements a user can get
                private readonly List<Game> _gamesPlayed;           ///The games the user has played in

                public SocketUser DiscordUser;                      ///The user's discord
                public readonly string Pid;                         ///The user's Ohio University ID
                public bool IsRaffleWinner;                         ///If the user has won a raffle prize
                private ushort NumberGamesPlayed;                   ///only used for tracking game ids
                public uint BoughtTickets;                          ///Tickets that are bought with the user's points

                public uint CurrentPoints => TotalPoints - (BoughtTickets * PrivateVariables.TicketValue); ///Tells the current points a user has by using the maximum points and the tickets bought by the user
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
                private User(ulong id, string pid, List<Game>? gamesPlayed = null, bool isRaffleWinner = false, ushort numberGamesPlayed = 0, List<string>? achievements = null, uint tickets = 0) : this(Server.GetUser(id), pid, gamesPlayed, isRaffleWinner, numberGamesPlayed, achievements, tickets) { }

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
                public int CompareTo(User? other) => other == null ? 0 : (int)(other.TotalPoints - TotalPoints);

                ///Operators
                public static implicit operator UserData(User u) => new UserData(u.DiscordUser.Id, u.Pid, u.GamesPlayed.Select(g => (GameData)g).ToArray(), u.IsRaffleWinner, u._achievementsClaimed.ToArray(), u.NumberGamesPlayed, u.BoughtTickets);
                public static implicit operator User(UserData u) => new User(u.DiscordId, u.PID, u.GamesPlayed.Select(g => (Game)g).ToList(), u.WonRaffle, u.NumberGamesPlayed, u.AchievementsClaimed.ToList());
            }
            private struct UserData
            {
                public ulong DiscordId;
                public string PID;
                public GameData[] GamesPlayed;
                public bool WonRaffle;
                public string[] AchievementsClaimed;
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
                Users.Sort();
                int i = 0;
                return string.Join('\n', Users.Take(x > Users.Count ? Users.Count : x).Select(u => $"{++i}: {(mention ? u.DiscordUser.Mention : Server.GetUser(u.DiscordUser.Id).DisplayName)} - {u.CurrentPoints}"));
            }
            public void UserBuyTickets(ulong discordId, uint x = 1)
            {
                User u = Users.FirstOrDefault(user => user.DiscordUser.Id == discordId) ?? throw new NullReferenceException(message: "User not found in system.");
                if (u.TotalPoints < PrivateVariables.TicketThresholds[PrivateVariables.TicketThresholds.Length - 1])
                    throw new Exception($"Error: User did not reach point threshold of {PrivateVariables.TicketThresholds[PrivateVariables.TicketThresholds.Length - 1]}");
                else if (x * PrivateVariables.TicketValue > u.CurrentPoints)
                    throw new Exception($"Error: User does not have enough points. Tickets cost {PrivateVariables.TicketValue} points each.");
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
                    Users.Where(user => !user.IsRaffleWinner).ToList().ForEach(user =>
                    {
                        raffleEntries.AddRange(PrivateVariables.TicketThresholds.Where(points => user.TotalPoints > points).Select(_ => user));
                        for (int i = 0; i < user.BoughtTickets; ++i, raffleEntries.Add(user)) ;
                    });
                    if (raffleEntries.Count() == 0)
                        throw new IndexOutOfRangeException("No valid users for raffle.");
                    Random r = new Random(DateTime.Now.Minute + DateTime.Now.Second + DateTime.Now.Millisecond);
                    User winner = raffleEntries[r.Next(raffleEntries.Count)];                    
                    return winner.DiscordUser;
                }
                catch (IndexOutOfRangeException) { throw; }
                catch { throw new Exception("Error in processing raffle"); }

            }
            public void ConfirmRaffle(SocketUser winner) => (Users.FirstOrDefault(user => user.DiscordUser.Id == winner.Id) ?? throw new NullReferenceException(message: "User not found in system.")).IsRaffleWinner = true;
            #endregion
        }

        private static class PrivateVariables
        {
            public static ulong CommandChannel { get; set; } = 0;
            public static ulong AnnouncementChannel { get; set; } = 0;
            public static ushort TicketValue { get; set; } = 0;
            public static int[] TicketThresholds { get; set; } = new int[0];
            public static SocketTextChannel SocketCommandChannel => Server.GetTextChannel(CommandChannel);
            public static SocketTextChannel SocketAnnouncementChannel => Server.GetTextChannel(AnnouncementChannel);

            [Start] public static void LoadVariables()
            {
                PV pv = JsonSerializer.Deserialize<PV>(File.ReadAllText("./XPModulePrivateVariables.json")) ?? throw new NullReferenceException("No Private Variable File Found!");
                CommandChannel = pv.CommandChannel;
                AnnouncementChannel = pv.AnnouncementChannel;
                TicketValue = pv.TicketValue;
                TicketThresholds = pv.TicketThresholds;
            }

            private class PV
            {
                public ulong CommandChannel { get; set; } = 0;
                public ulong AnnouncementChannel { get; set; } = 0;
                public ushort TicketValue { get; set; } = 0;
                public int[] TicketThresholds { get; set; } = new int[0];
            }
        }

        private static XpStorage? xpSystem;

        #region Commands
        [Command(description: "Creates a new or existing event based on the name given.", modOnly: true)]
        [Option(name: "event-name", type: ApplicationCommandOptionType.String, description: "Name of the event to run, will load an event of the same name.", isRequired: true)]
        public static async Task StartEvent(SocketSlashCommand _command)
        {
            ///Checks if the event has started
            if (xpSystem != null)
                throw new Exception("Error: Event currently running.");

            await Respond(_command, buttons: new Button[] { new Button(async (SocketSlashCommand _command, SocketMessageComponent _component) =>
            {
                ///Adds permissions for everyone to input commands
                //*note* uncomment next line when publishing
                //await PrivateVariables.SocketCommandChannel.AddPermissionOverwriteAsync(Server.EveryoneRole, OverwritePermissions.DenyAll(PrivateVariables.SocketCommandChannel).Modify(viewChannel: PermValue.Allow, useApplicationCommands: PermValue.Allow, sendMessages: PermValue.Allow));

                ///Parses the event name
                string eventName = (string)_command.Data.Options.First().Value;

                ///Check if the event needs loaded
                bool canLoad = XpStorage.CanLoad(eventName);

                ///Creates the event
                xpSystem = new XpStorage(eventName);

                //*note* readd the @ before everyone
                ///Checks if the event was loaded and gives appropriate response
                await PrivateVariables.SocketAnnouncementChannel.SendMessageAsync(text: canLoad ? $"everyone Thank you for your patience, the event is back up and running!" : $"everyone Welcome to {xpSystem.EventName}!\nLook to this channel for future updates and visit the {PrivateVariables.SocketCommandChannel.Mention} channel to register youself to this event! (/join-event)\n**Disclaimer**: you need to be a current student at Ohio University and be at the event to recieve any prizes");

                ///User Feedback
                await Respond(_command, text: "Successfully started the event.");
            }, "Confirm", ButtonStyle.Danger) });
        }

        [Command(description: "Ends the current event, displaying all of the event's statistics in the announcement channel.", modOnly: true)]
        public static async Task EndEvent(SocketSlashCommand _command)
        {
            ///Checks if the event has started
            if (xpSystem == null)
                throw new Exception("Error: No event currently running.");

            await Respond(_command, buttons: new Button[] { new Button(async Task (SocketSlashCommand _command, SocketMessageComponent _component) => {
                ///Sets user permissions for the command channel back to default
                await PrivateVariables.SocketCommandChannel.AddPermissionOverwriteAsync(Server.EveryoneRole, OverwritePermissions.DenyAll(PrivateVariables.SocketCommandChannel));

                ///Logs the end of all day message
                await PrivateVariables.SocketAnnouncementChannel.SendMessageAsync(text: $"@everyone Thank you all for participating in {xpSystem.EventName}!\nWe hope you all had fun, here are the results:\n {xpSystem.GetTopXUsersString(3, true)}\nHere are some statistics for today's event:\n{xpSystem.DisplayAll()}\n\nOnce again thank you all for showing up and we hope to see you at our next event!");

                ///User Feedback
                await Respond(_command, text: "Successfully ended the event.");
                xpSystem = null;
            }, "Confirm", ButtonStyle.Danger) });
        }

        [Command(description: "Draws a raffle winner from the pool of current users, odds increase with additional tickets.", modOnly: true)]
        public static async Task DrawRaffle(SocketSlashCommand _command)
        {
            ///Checks if the event has started
            if (xpSystem == null)
                throw new Exception("Error: No event currently running.");

            await Respond(_command, buttons: new Button[] { new Button(async Task (SocketSlashCommand _command, SocketMessageComponent _component) => {
                SocketUser winner = xpSystem.DrawRaffle();

                ///Mod Confirmation
                await Respond(_command, text: $"Winner: {Server.GetUser(winner.Id).Nickname}", buttons: new Button[]{ new Button(async Task (SocketSlashCommand _command, SocketMessageComponent _component) => {
                    xpSystem.ConfirmRaffle(winner);

                    ///Displays a message notifying everyone of a raffle being drawn
                    await PrivateVariables.SocketAnnouncementChannel.SendMessageAsync(text: $"@everyone Congratulations to {winner.Mention} for winning the raffle!\nMake sure to contact an officer to redeem your prize.");

                    ///User Feedback
                    await Respond(_command, text: "Successfully drew a raffle.");
                })});
            }, "Confirm", ButtonStyle.Danger)});
        }

        [Command(description: "Checks a user's profile, listing their general statistics.", modOnly: true)]
        [Option(name: "user", type: ApplicationCommandOptionType.User, description: "The user to check the stats of.", isRequired: true)]
        public static async Task SeeUser(SocketSlashCommand _command)
        {
            ///Checks if the event has started
            if (xpSystem == null)
                throw new Exception("Error: No event currently running.");

            ///Responds with an ephemeral embed of the info
            await Respond(_command, embed: new EmbedBuilder().WithTitle("Player Data").WithDescription(xpSystem.GetUser(((SocketUser)_command.Data.Options.First().Value).Id)).WithColor(Color.Red).Build());
        }

        [Command(description: "Checks a user's completed games, and their asociated statistics.", modOnly: true)]
        [Option(name: "user", type: ApplicationCommandOptionType.User, description: "The user to check the games of.", isRequired: true)]
        public static async Task SeeUserGames(SocketSlashCommand _command)
        {
            ///Checks if the event has started
            if (xpSystem == null)
                throw new Exception("Error: No event currently running.");

            ///Creates a recursive embed
            await CreateRecursiveMuliPageEmbed(_command, xpSystem.GetUserGames(((SocketUser)_command.Data.Options.First().Value).Id), $"{((SocketUser)_command.Data.Options.First().Value).Username}'s Games", color: Color.Red);
        }

        [Command(description: "Checks a user's completed achievements.", modOnly: true)]
        [Option(name: "user", type: ApplicationCommandOptionType.User, description: "The user to check the achievements of.", isRequired: true)]
        public static async Task SeeUserAchievements(SocketSlashCommand _command)
        {
            ///Checks if the event has started
            if (xpSystem == null)
                throw new Exception("Error: No event currently running.");

            ///Creates a recursive embed
            await CreateRecursiveMuliPageEmbed(_command, xpSystem.GetUserAchievements(((SocketUser)_command.Data.Options.First().Value).Id), $"{((SocketUser)_command.Data.Options.First().Value).Username}'s Achievements", color: Color.Red);
        }

        [Command(description: "Displays the top users of the current event based on the number of points they have.", modOnly: true)]
        [Option(name: "number", type: ApplicationCommandOptionType.Integer, description: "The number of users to display.", isRequired: true)]
        public static async Task SeeXUsers(SocketSlashCommand _command)
        {
            ///Checks if the event has started
            if (xpSystem == null)
                throw new Exception("Error: No event currently running.");

            ///Gets X
            int numberOfUsers = Convert.ToInt32(_command.Data.Options.First().Value);

            ///Displays the leaderboard
            await Respond(_command, embed: new EmbedBuilder().WithTitle($"Top {numberOfUsers} Users").WithDescription(xpSystem.GetTopXUsersString(numberOfUsers)).WithColor(Color.Orange).Build());
        }

        [Command(description: "Removes a game from a user's profile.", modOnly: true)]
        [Option(name: "user", type: ApplicationCommandOptionType.User, description: "The user to remove the game from.", isRequired: true)]
        [Option(name: "id", type: ApplicationCommandOptionType.Integer, description: "The ID of the game to remove from the profile.", isRequired: true)]
        public static async Task RemoveUserGame(SocketSlashCommand _command)
        {
            ///Checks if the event has started
            if (xpSystem == null)
                throw new Exception("Error: No event currently running.");

            await Respond(_command, buttons: new Button[]{ new Button(async Task (SocketSlashCommand _command, SocketMessageComponent _component) => {                
                ///Remove the game
                EmbedBuilder displayGame = xpSystem.RemoveUserGame(((SocketUser)_command.Data.Options.First().Value).Id, Convert.ToInt32(_command.Data.Options.ElementAt(1).Value));

                ///User Feedback
                await Respond(_command, embed: displayGame.WithTitle($"Successfully removed {((SocketUser)_command.Data.Options.First().Value).Username}'s game").WithColor(Color.Red).Build());
            }, "Confirm", ButtonStyle.Danger)});
        }

        [Command(description: "Removes an achievement from a user's profile.", modOnly: true)]
        [Option(name: "user", type: ApplicationCommandOptionType.User, description: "The user to remove the achievement from.", isRequired: true)]
        [Option(name: "name", type: ApplicationCommandOptionType.String, description: "The name of the achievement to remove from the profile.", isRequired: true)]
        public static async Task RemoveUserAchievement(SocketSlashCommand _command)
        {
            ///Checks if the event has started
            if (xpSystem == null)
                throw new Exception("Error: No event currently running.");

            await Respond(_command, buttons: new Button[]{ new Button(async Task (SocketSlashCommand _command, SocketMessageComponent _component) => {
                ///Remove the achievement
                EmbedBuilder displayAchievement = xpSystem.UnclaimUserAchievement(((SocketUser)_command.Data.Options.First().Value).Id, (string)_command.Data.Options.ElementAt(1).Value);

                ///User Feedback
                await Respond(_command, embed: displayAchievement.WithTitle($"Successfully removed {((SocketUser)_command.Data.Options.First().Value).Username}'s achievement").WithColor(Color.Red).Build());
            }, "Confirm", ButtonStyle.Danger)});
        }

        [Command(description: "Removes a user's profile from the event, *Warning* this removes all of their data.", modOnly: true)]
        [Option(name: "user", type: ApplicationCommandOptionType.User, description: "The user to remove from the event.", isRequired: true)]
        public static async Task RemoveUser(SocketSlashCommand _command)
        {
            ///Checks if the event has started
            if (xpSystem == null)
                throw new Exception("Error: No event currently running.");

            await Respond(_command, buttons: new Button[]{ new Button(async Task (SocketSlashCommand _command, SocketMessageComponent _component) => {
                ///Remove user
                xpSystem.RemoveUser(((SocketUser)_command.Data.Options.First().Value).Id);

                ///User Feedback
                await Respond(_command, text:"Successfully removed player.");
            }, "Confirm", ButtonStyle.Danger)});
        }

        [Command(description: "Registers you for the current event, this allows you to participate in raffles and win prizes.")]
        [Option(name: "pid", type: ApplicationCommandOptionType.String, description: "Your Personal Identification Number given to Ohio University students (note include the 'p').", isRequired: true)]
        public static async Task JoinEvent(SocketSlashCommand _command)
        {
            ///Checks if the event has started
            if (xpSystem == null)
                throw new Exception("Error: No event currently running.");

            ///Adds the user
            xpSystem.AddNewUser(_command.User, ((string)_command.Data.Options.First().Value).ToUpper());

            ///User Feedback
            await Respond(_command, text: "Successfully joined event.");
        }

        [Command(description: "Unregisters you from the current event, you can rejoin but you will not have any of your data.")]
        public static async Task LeaveEvent(SocketSlashCommand _command)
        {
            ///Checks if the event has started
            if (xpSystem == null)
                throw new Exception("Error: No event currently running.");

            await Respond(_command, buttons: new Button[]{ new Button(async Task (SocketSlashCommand _command, SocketMessageComponent _component) => {
                ///Removes user
                xpSystem.RemoveUser(_command.User.Id);

                ///User Feedback
                await Respond(_command, text: "Successfully left event.");
            }, "Confirm", ButtonStyle.Danger)});
        }

        [Command(description: "Shows you your statistics, PID, points earned, tickets, and more.")]
        public static async Task SeeSelf(SocketSlashCommand _command)
        {
            ///Checks if the event has started
            if (xpSystem == null)
                throw new Exception("Error: No event currently running.");

            ///Shows the caller's information
            await Respond(_command, embed: new EmbedBuilder().WithTitle("Your Data").WithDescription(xpSystem.GetUser(_command.User.Id).ToString()).WithColor(Color.Blue).Build());
        }

        [Command(description: "Adds a game to your profile and assigns points accordingly.")]
        [Option(name: "name", type: ApplicationCommandOptionType.String, description: "The name of the game played.", isRequired: true)]
        [Option(name: "player-count", type: ApplicationCommandOptionType.Integer, description: "The number of players or teams in the game.", isRequired: true)]
        [Option(name: "type", type: ApplicationCommandOptionType.String, description: "One of: ranked/coop/teams/party based on the type of game played.", isRequired: true, channelTypes: new ChannelType[0], choiceKeys: new string[] { "Ranked", "Co-op", "Teams", "Party" }, choiceValues: new string[] { "ranked", "coop", "teams", "party" })]
        [Option(name: "placing", type: ApplicationCommandOptionType.Integer, description: "Where you ranked, teams share the same rank, for unranked games a win is 1 and a loss is 2.", isRequired: true)]
        [Option(name: "length", type: ApplicationCommandOptionType.Integer, description: "The length of the game in minutes.", isRequired: true)]
        public static async Task AddGame(SocketSlashCommand _command)
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
            await Respond(_command, embed: displayGame.WithTitle("Successfully added game.").WithColor(Color.Blue).Build());
        }

        [Command(description: "Removes a specified game from your profile.")]
        [Option(name: "id", type: ApplicationCommandOptionType.Integer, description: "The Id of the game to remove (can be found with see-games).", isRequired: true)]
        public static async Task RemoveGame(SocketSlashCommand _command)
        {
            ///Checks if the event has started
            if (xpSystem == null)
                throw new Exception("Error: No event currently running.");

            await Respond(_command, buttons: new Button[]{ new Button(async Task (SocketSlashCommand _command, SocketMessageComponent _component) => {
                ///Remove self
                EmbedBuilder displayGame = xpSystem.RemoveUserGame(_command.User.Id, Convert.ToInt32(_command.Data.Options.First().Value));

                ///User Feedback
                await Respond(_command, embed: displayGame.WithTitle("Successfully removed game.").WithColor(Color.Blue).Build());
            }, "Confirm", ButtonStyle.Danger) });
        }

        [Command(description: "Lists all of the games you've played.")]
        public static async Task SeeGames(SocketSlashCommand _command)
        {
            ///Checks if the event has started
            if (xpSystem == null)
                throw new Exception("Error: No event currently running.");

            ///Creates a recursive embed
            await Interaction.CreateRecursiveMuliPageEmbed(_command, xpSystem.GetUserGames(_command.User.Id), "Your Games");
        }

        [Command(description: "Adds an achievement to your profile from the list of available achievements.")]
        [Option(name: "name", type: ApplicationCommandOptionType.String, description: "The name of the achievement to add (can be found by using see-achievements).", isRequired: true)]
        public static async Task AddAchievement(SocketSlashCommand _command)
        {

            ///Checks if the event has started
            if (xpSystem == null)
                throw new Exception("Error: No event currently running.");

            ///Adds the achievement
            EmbedBuilder displayAchievement = xpSystem.ClaimUserAchievement(_command.User.Id, (string)_command.Data.Options.First().Value);

            ///User Feedback
            await Respond(_command, embed: displayAchievement.WithTitle("Successfully claimed achievement").WithColor(Color.Blue).Build());
        }

        [Command(description: "Unclaims an achievment from your profile.")]
        [Option(name: "name", type: ApplicationCommandOptionType.String, description: "The name of the achievement to remove from your profile.", isRequired: true)]
        public static async Task RemoveAchievement(SocketSlashCommand _command)
        {
            ///Checks if the event has started
            if (xpSystem == null)
                throw new Exception("Error: No event currently running.");

            await Respond(_command, buttons: new Button[]{ new Button(async Task (SocketSlashCommand _command, SocketMessageComponent _component) => {
                ///Remove achievement
                EmbedBuilder achievementUnclaimed = xpSystem.UnclaimUserAchievement(_command.User.Id, (string)_command.Data.Options.First().Value);

                ///User Feedback
                await Respond(_command, embed: achievementUnclaimed.WithTitle("Successfully removed achievement.").WithColor(Color.Blue).Build());
            }, "Confirm", ButtonStyle.Danger) });
        }

        [Command(description: "Either displays all of your achievements or all possible achievements.")]
        [Option(name: "show-all", type: ApplicationCommandOptionType.Boolean, description: "True: shows all possible achievements | False: shows your personal completed achievements.", isRequired: true)]
        public static async Task SeeAchievements(SocketSlashCommand _command)
        {
            ///Checks if the event has started
            if (xpSystem == null)
                throw new Exception("Error: No event currently running.");

            ///Creates a recursive embed
            await CreateRecursiveMuliPageEmbed(_command, xpSystem.GetUserAchievements(_command.User.Id, (bool)_command.Data.Options.First()), (bool)_command.Data.Options.First() ? "List of Achievements" : "Your Achievements");
        }

        [Command(description: "Buys a specified amount of tickets and removes points accordingly.")]
        [Option(name: "tickets", type: ApplicationCommandOptionType.Integer, description: "The number of tickets you wish to buy.", isRequired: true)]
        public static async Task BuyTickets(SocketSlashCommand _command)
        {
            ///Checks if the event has started
            if (xpSystem == null)
                throw new Exception("Error: No event currently running.");

            await Respond(_command, buttons: new Button[]{ new Button(async Task (SocketSlashCommand _command, SocketMessageComponent _component) => {
                ///Get the information
                int ticketsBought = Convert.ToInt32(_command.Data.Options.First().Value);
                ulong user = _command.User.Id;

                ///Buy the tickets
                xpSystem.UserBuyTickets(user, (uint)ticketsBought);

                ///User Feedback
                await Respond(_command, text: $"Successfully bought {_command.Data.Options.First().Value} tickets.");
            }, "Confirm", ButtonStyle.Danger)});
        }
        #endregion
    }
}