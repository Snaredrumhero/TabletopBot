# TabletopBot
Bot for Board Game Club server to calculate and display Xp for an all-day event
<br><br>
## Instructions for Use
1. Go into bot-test channel and use slash command: /runallday. Type *YES* to confirm the start of the event
2. Have each player type in their PID to sign into the event *this will tell the bot to start tracking their XP, make sure that it's known that games played before signing in will not be counted for XP*
3. To pull raffles, go into bot-test channel and use the slash command: /raffledraw to award a member a ticket. Type *YES* to confirm the drawing, *NO* to cancel the draw, or *REDRAW* to cancel the draw and redraw a new ticket
4. When the event is over, go into bot-test channel and use slash command /endallday. Type *YES* to confirm the end of the event.
5. The bot will post a listing of every person who attended, how long they played, how many games they played, their scores, whether or not they won raffles/grand prizes, the average XP total, time played, games played, and the total number of attendees
