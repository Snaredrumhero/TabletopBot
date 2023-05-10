using Discord.WebSocket;

namespace TableTopBot
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
            /*
            This means that every function will fire back their message received functions
            every time a message gets sent 
            */
        }

        //All functions including callbacks (each callback takes different arguments)
        private async Task MessageReceived(SocketMessage message)
        {
            if (message.Content == "!ping")
                await message.Channel.SendMessageAsync("Pong!");
        }
    }
}
