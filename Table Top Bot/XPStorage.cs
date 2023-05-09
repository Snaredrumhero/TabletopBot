namespace Table_Top_Bot
{
    internal static class XpStorage
    {
        private static readonly List<User> Users = new();

        private static readonly int[] TicketThresholds = new int[]
        {
            0, //need to add more ticket values
        };

        public struct AchievementData
        {
            public string Name;
            public string Description;
            public int XpValue;
        }

        private static readonly List<AchievementData> DefaultAchievements = new()
        {
            new AchievementData {Name = "name", Description = "description", XpValue = 0},
        };

        public class User
        {
            public ulong DiscordId;
            public string Pid = "";
            private readonly List<Game> _gamesPlayed = new();
            public bool IsRaffleWinner = false; //Could be changed to an int if wanted multiple wins
            private readonly List<Achievement> _allAchievements = DefaultAchievements.Select(data => new Achievement(data)).ToList();
            public List<Achievement> Achievements => _allAchievements.Where(achievement => achievement.IsClaimed).ToList();
            public uint NumberGamesPlayed; //only used for tracking game ids
            public static bool operator >(User a, User b) => a.AddPointValues() > b.AddPointValues();
            public static bool operator <(User a, User b) => a.AddPointValues() < b.AddPointValues();

            //Game type 1 = ranked, 2 = CoOp, 3 = Teams, 4 = Party
            //Rank = 1 for win 2 for loss
            public void AddGame(uint playerCount, Game.GameType type, uint rank, uint length)
            {
                _gamesPlayed.Add(new Game(NumberGamesPlayed, type, playerCount, rank, length));
                NumberGamesPlayed++;
            }
            public void RemoveGame(int id)
            {
                _gamesPlayed.RemoveAll(game => game.Id == id);
            }

            public int Points => AddPointValues();
            private int AddPointValues()
            {
                return _gamesPlayed.Select(game => game.Xp).Sum() + Achievements.Select(achievement => achievement.XpValue).Sum();
            }

            public override string ToString()
            {
                return $"{DiscordId}\nPID: {Pid}\nPoints: {AddPointValues()}";
            }

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
        }
        public class Game
        {
            public readonly uint Id;
            public enum GameType { Ranked = 1, CoOp = 2, Teams = 3, Party = 4 }
            public readonly GameType Type;
            public readonly uint PlayerCount;
            public readonly uint Rank;
            public readonly uint GameLengthInMinutes;
            public int Xp => ComputeXp();

            private const double PointsScale = 1.5;//scale of points awarded (makes numbers bigger bc big numbers are fun)
            private static readonly double[] RankedPositions = { 24d / 24d, 22d / 24d, 20d / 24d, 18d / 24d, 17d / 24d, 16d / 24d };

            public Game(uint id, GameType type, uint playerCount, uint rank, uint gameLengthInMinutes)
            {
                if (rank == 0)
                {
                    throw new ArgumentException(message: "rank can not be 0", paramName: nameof(rank));
                }

                if (playerCount < rank)
                {
                    throw new ArgumentException(paramName: nameof(rank), message: "rank cannot be less than playerCount");
                }

                Id = id;
                Type = type;
                PlayerCount = playerCount;
                Rank = rank;
                GameLengthInMinutes = gameLengthInMinutes;
            }

            private int ComputeXp()
            {
                double points = PointsScale * GameLengthInMinutes * 3 / 2;
                if (Type != GameType.Ranked)
                    return (int)(points * (Rank == 1 ? RankedPositions[2] : RankedPositions[^1]));
                if (PlayerCount < RankedPositions.Length)
                    //1st gets full (24/24)
                    //if 3 people 2 gets 20
                    //if 5 people 2: 22 3:20 4:18
                    //last gets 2/3 (16/24)
                    //CoOp Teams Party W: 3rd place L: 2/3 last
                    return (int)(points * (8d * ((double)PlayerCount - Rank) / (PlayerCount - 1d + 16d) / 24d));
                if (Rank > RankedPositions.Length)
                    return (int)(points * RankedPositions[^1]);
                return (int)(points * RankedPositions[Rank - 1]);
            }
        }

        public class Achievement
        {
            public string Name => _data.Name;
            public string Description => _data.Description;
            public int XpValue => _data.XpValue;
            public bool IsClaimed;
            private readonly AchievementData _data;

            public Achievement(AchievementData data)
            {
                _data = data;
            }
        }

        public static User? GetUser(ulong discordId) => Users.FirstOrDefault(user => user.DiscordId == discordId);

        public static void AddNewUser(ulong discordId, string pid)
        {
            if(Users.Any(user => user.DiscordId == discordId))
            {
                return;
            }
            Users.Add(new User { DiscordId = discordId, Pid = pid });
        }
        public static void RemoveUser(User user) => Users.Remove(user);

        public static string DrawRaffle() //returns the message to send to the server
        {
            List<User> raffleEntries = new();
            foreach (User user in Users.Where(user => !user.IsRaffleWinner))
            {
                raffleEntries.AddRange(TicketThresholds.Where(points => user.Points > points).Select(_ => user));
            }
            Random r = new(DateTime.Now.Minute + DateTime.Now.Second + DateTime.Now.Millisecond);
            return $"@Everyone Congrats to {raffleEntries[r.Next(raffleEntries.Count)]} for winning the raffle!";
        }
        public static string DisplayTopXUsers(int x)
        {
            //Fail loudly, not quietly. If it if it doesn't work, say so. That way the caller can fix it
            if (x > Users.Count)
                throw new ArgumentOutOfRangeException(nameof(x), "There aren't enough users to populate the list");
            Users.Sort();

            //String concat is very memory intensive because each concat requires allocating and deallocating the memory for the previous string (this isn't a string buffer, that's a different type entirely)
            //This is an easier way of getting what you want
            List<string> lines = new();

            for (int i = 0; i < x; i++)
                lines.Add($"{i}: {Users[i].Points} - {Users[i].DiscordId}");

            return string.Join('\n', lines);
        }
    }
}