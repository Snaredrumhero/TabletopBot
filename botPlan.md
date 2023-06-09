# Coding Plan 
## Needs
* A way to store data for each user
* Slash command to gain xp
   * Game type, number of players, time played, bonus xp
       * For game type, how do people know what types of games can be selected and what each one means?
       * I know that Discord on PC has previews for what you select from adding reactions using carl-bot, but does that display on phones?
       * Make sure that people can't put in times that are longer than however long the event has been going (give or take 20 minutes)
           * This should also shut down people from making joke answers that become permanent. Maybe have something fun like "This option is impossible, but if you're curious, here's how much points you'd earn: ______"
   * Sign in using student PID's before collecting points
* Way to turn on/off tracking that's controlled by Discord Username
* An undo command for every user to undo their last command
* Channels
   * Leaderboard
   * Rules/commands 

## Wants
* Feature to generate a spreadsheet of attendees, number of games played, and average points
* Adding raffle functionality (Do we want this? I also feel like the ritual of people going up to the front and adding tickets being too fun to remove. Needs future discussion) 
   * Tracking users' points to give them raffles
   * Giving user's slash command to spend points

## Data Storage
* We can either just do this all in memory with a file backup, or immediately store everything in a file backup as each command is put in and run the bot based on that data
* Store the data onto a JSON file
     * Each event would be its own JSON file that contains various participant objects
         * Inside each participant, you can store each command that they gave to the bot
         * Along with that array, we can store each xp value in array, so we can easily see any out of the ordinary XP requests
         * With C# we can use a lot of short cuts for queries such as members.games.select(*function that takes a specific game and returns what you want*)
         * Pro-tip, usernames and nicknames on Discord are different, if a user doesn't have a server specific nickname, then nickname will be null
      * Need to have an option to continue an event should the bot crash part way through an event
