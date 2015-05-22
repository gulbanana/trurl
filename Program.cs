using IrcDotNet;

namespace trurl
{
    class Program
    {
        static void Main(string[] args)
        {
            var bot = new DiceBot();
            
            var userInfo = new IrcUserRegistrationInfo()
            {
                NickName = "trurl",
                UserName = "trurl",
                RealName = "Trurl Klapaucius, Cyberiad Dicebot"
            };

            //blocks until bot.Stop() is called or 'exit' is entered on the commandline
            bot.Run("irc.sorcery.net", userInfo, new[] { "#au-ooc", "#aurora" });
        }
    }
}