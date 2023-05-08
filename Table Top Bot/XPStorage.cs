namespace Table_Top_Bot
{
    internal static class XPStorage
    {
        static List<User> Users = new List<User>();
        readonly static Dictionary<string, int> Achievements = new Dictionary<string, int>()
        {
            { "Temp", 0 }, //just temp
        };
        public struct User
        {
            public string discordID;
            public List<Game> gamesPlayed;
            public bool wonRaffle; //Could be changed to an int if wanted multiple wins
            public bool[] achievements; //true for each achievement achieved
        }
        public struct Game
        {
            enum GameType { NULL = 0, Ranked, CoOp, Unranked }
            GameType type;
            uint playerCount;
            uint rank;
            float length;
        }

        public static void InitUser(string _discordID)
        {
            for (int i = 0; i < Users.Count; i++)
                if (Users[i].discordID == _discordID)
                    return;
            Users.Add(new User
            {
                discordID = _discordID,
                gamesPlayed = new List<Game>(),
                wonRaffle = false,
                achievements = new bool[Achievements.Count],
            });;
        }
        public static void RemoveUser(string _discordID)
        {
            for (int i = 0; i < Users.Count; i++)
                if (Users[i].discordID == _discordID)
                {
                    Users.RemoveAt(i);
                    return;
                }
        }
        public static void ClaimAchivement(string _discordID, string _achievementName)
        {
            if (!Achievements.ContainsKey(_achievementName))
                return;
            User? u = null;
            for (int i = 0; i < Users.Count; i++)
                if (Users[i].discordID == _discordID)
                    u = Users[i];
            if (u == null)
                return;
            u.Value.achievements[Achievements.Keys.ToList().IndexOf(_achievementName)] = true;
        }
        public static void UnclaimAchievement(string _discordID, string _achievementName)
        {
            if (!Achievements.ContainsKey(_achievementName))
                return;
            User? u = null;
            for (int i = 0; i < Users.Count; i++)
                if (Users[i].discordID == _discordID)
                    u = Users[i];
            if (u == null)
                return;
            u.Value.achievements[Achievements.Keys.ToList().IndexOf(_achievementName)] = false;
        }
        public static void UserAddGame()
        {

        }
        public static void UserRemoveGame()
        {

        }
        public static int GetUserPointValue(string _discordID)
        {
            //Verrifies user is in system
            User? u = null;
            for (int i = 0; i < Users.Count; i++)
                if (Users[i].discordID == _discordID)
                    u = Users[i];
            if (u == null)
                return 0;

            int points = 0;

            //Adds for each game played
            //foreach game += xp code           //needs replaced

            //Adds for each achievement
            int[] achievementValues = Achievements.Values.ToArray();
            for (int i = 0; i < achievementValues.Length; i++)
                if (u.Value.achievements[i])
                    points += achievementValues[i];
            return points;
        }
        public static string DisplayUser(string _discordID)
        {
            return "";
        }
        public static void DrawRaffle()
        {

        }
        public static string DisplayTopXUsers(int _x)
        {
            return "";
        }
    }
}