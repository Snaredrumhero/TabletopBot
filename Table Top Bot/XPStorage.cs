﻿namespace Table_Top_Bot
{
    internal static class XPStorage
    {
        static List<User> Users = new List<User>();
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
            public string discordID = "";
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

        public static User? GetUser(string _discordID)
        {
            User? u = null;
            for (int i = 0; i < Users.Count; i++)
                if (Users[i].discordID == _discordID)
                    u = Users[i];
            return u;
        }
        public static void InitUser(string _discordID, string _pid)
        {
            for (int i = 0; i < Users.Count; i++)
                if (Users[i].discordID == _discordID)
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
        public static void UserAddGame(User _user)
        {
            /*_user.gamesPlayed.Add(new Game
            {
                id = u.numberGamesPlayed,
                type = ,
                playerCount = ,
                rank = ,
                length = ,
            });*/
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