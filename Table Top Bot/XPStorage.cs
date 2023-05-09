namespace Table_Top_Bot
{
    internal static class XPStorage
    {
        private static List<User> Users = new List<User>();
        readonly static Dictionary<string, int> Achievements = new Dictionary<string, int>()
        {
            { "Temp", 0 }, //just temp
        };
        readonly static int[] PointsToTickets = new int[]
        {
            0, //need to add more ticket values
        };
        public class User
        {
            public ulong discordID = 0;
            public string PID = "";
            public List<Game> gamesPlayed = new List<Game>();
            public bool wonRaffle = false; //Could be changed to an int if wanted multiple wins
            public bool[] achievements = new bool[Achievements.Count]; //true for each achievement achieved
            public uint numberGamesPlayed = 0; //only used for tracking game ids
            public static bool operator > (User _a, User _b) { return GetUserPointValue(_a) > GetUserPointValue(_b); }
            public static bool operator <(User _a, User _b) { return GetUserPointValue(_a) < GetUserPointValue(_b); }
        }
        public class Game
        {
            public uint id = 0;
            public enum GameType { NULL = 0, Ranked = 1, CoOp = 2, Teams = 3, Party = 4 }
            public GameType type = GameType.NULL;
            public uint playerCount = 0;
            public uint rank = 0;
            public uint length = 0;
        }

        public static User? GetUser(ulong _discordID) => Users.FirstOrDefault(user => user.discordID == _discordID);
        public static void InitUser(ulong _discordID, string _pid)
        {
            if (Users.Any(user => user.discordID == _discordID))
                return;
            Users.Add(new User { discordID = _discordID, PID = _pid });
        }
        public static void RemoveUser(User _user) { Users.Remove(_user); }
        public static void ClaimAchivement(User _user, string _achievementName)
        {
            if (!Achievements.ContainsKey(_achievementName))
                return;
            _user.achievements[Achievements.Keys.ToList().IndexOf(_achievementName)] = true;
        }
        public static void UnclaimAchievement(User _user, string _achievementName)
        {
            if (!Achievements.ContainsKey(_achievementName))
                return;
            _user.achievements[Achievements.Keys.ToList().IndexOf(_achievementName)] = false;
        }
        //Game type 1 = ranked, 2 = CoOp, 3 = Teams, 4 = Party
        //Rank = 1 for win 2 for loss
        public static void UserAddGame(User _user, uint _playerCount, uint _gameType, uint _rank, uint _length)
        {
            Game.GameType type = Game.GameType.NULL;
            if (_gameType == 1)
                type = Game.GameType.Ranked;
            else if (_gameType == 2)
                type = Game.GameType.CoOp;
            else if (_gameType == 3)
                type = Game.GameType.Teams;
            else if (_gameType == 4)
                type = Game.GameType.Party;
            if (type == Game.GameType.NULL)//error
                return;
            _user.gamesPlayed.Add(new Game
            {
                id = _user.numberGamesPlayed,
                type = type,
                playerCount = _playerCount,
                rank = _rank,
                length = _length,
            });
            _user.numberGamesPlayed++;
        }
        public static void UserRemoveGame(User _user, int _id)
        {
            for (int i = 0; i < _user.gamesPlayed.Count; i++)
                if (_user.gamesPlayed[i].id == _id)
                {
                    _user.gamesPlayed.RemoveAt(i);
                    return;
                }
        }
        public static int GetUserPointValue(User _user)
        {
            int points = 0;

            //Adds for each game played
            for (int i = 0; i < _user.gamesPlayed.Count; i++)
                points += XPCalc(_user.gamesPlayed[i]);

            //Adds for each achievement
            int[] achievementValues = Achievements.Values.ToArray();
            for (int i = 0; i < achievementValues.Length; i++)
                if (_user.achievements[i])
                    points += achievementValues[i];
            return points;
        }
        public static string DisplayUser(User _user) { return $"{_user.discordID}\nPID: {_user.PID}\nPoints: {GetUserPointValue(_user)}"; }
        public static string DrawRaffle() //returns the message to send to the server
        {
            List<int> raffleIDs = new List<int>();
            for (int i = 0; i < Users.Count; i++)
                if (!Users[i].wonRaffle)
                {
                    int p = GetUserPointValue(Users[i]);
                    for (int j = 0; j < PointsToTickets.Length; j++)
                        if (PointsToTickets[i] <= p)
                            raffleIDs.Add(i);
                }
            Random r = new Random(DateTime.Now.Minute + DateTime.Now.Second + DateTime.Now.Millisecond);
            return $"@Everyone Congrats to {Users[raffleIDs[r.Next(raffleIDs.Count)]]} for winning the raffle!";
        }
        public static string DisplayTopXUsers(int _x)
        {
            if (_x > Users.Count)
                return "";
            Users.Sort();
            string s = "";
            for (int i = 0; i < _x; i++)
                s += $"{i}: {GetUserPointValue(Users[i])} - {Users[i].discordID}\n";
            return s;
        }

        private const float POINTS_SCALE = 1.5f;//scale of points awarded (makes numbers bigger bc big numbers are fun)
        private static readonly float[] RANKED_POSITIONS = { 24f/24f, 22f/24f, 20f/24f, 18f/24f, 17f/24f, 16f/24f };
        private static int XPCalc(Game _game)
        {
            float points = POINTS_SCALE * _game.length * 3 / 2;
            if (_game.type == Game.GameType.NULL || _game.rank <= 0 || _game.rank > _game.playerCount)//error
                return 0;
            if(_game.type == Game.GameType.Ranked)
                if (_game.playerCount >= RANKED_POSITIONS.Length)
                {
                    if(_game.rank > RANKED_POSITIONS.Length)
                        return (int)(points * RANKED_POSITIONS[RANKED_POSITIONS.Length - 1]);
                    return (int)(points * RANKED_POSITIONS[_game.rank - 1]);
                }
                else
                    //1st gets full (24/24)
                    //if 3 people 2 gets 20
                    //if 5 people 2: 22 3:20 4:18
                    //last gets 2/3 (16/24)
                    return (int)(points * (8f * (_game.playerCount - _game.rank) / (_game.playerCount - 1 + 16) / 24));
            //CoOp Teams Party W: 3rd place L: 2/3 last
            return (int)(points * (_game.rank == 1 ? RANKED_POSITIONS[2] : RANKED_POSITIONS[RANKED_POSITIONS.Length - 1]));
        }
    }
}