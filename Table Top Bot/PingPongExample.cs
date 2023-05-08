using Discord.WebSocket;

namespace Table_Top_Bot
{
    //An example Module
    internal class PingPong : Module
    {
        //Constructor must be here even if there is nothing except base call
        public PingPong(Program _bot) : base(_bot) { }

        //Place all code that adds a callback here
        public override void InitilizeModule()
        {
            Bot.AddMessageReceivedCallback(MessageReceived);
        }

        //All functions including callbacks (each callback takes different arguements)
        private async Task MessageReceived(SocketMessage message)
        {
            if (message.Content == "!ping")
                await message.Channel.SendMessageAsync("Pong!");
        }
    }
}
