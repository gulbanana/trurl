using System.Configuration;
using IrcDotNet;

namespace trurl
{
    class Program
    {
        static void Main(string[] args)
        {
            //Gather config
            var server = ConfigurationManager.AppSettings["server"];
            var nick = ConfigurationManager.AppSettings["nick"];
            var channels = ConfigurationManager.AppSettings["channels"];
            var admins = ConfigurationManager.AppSettings["admins"];

            var bot = new DiceBot(admins.Split(','));
            
            var userInfo = new IrcUserRegistrationInfo()
            {
                NickName = nick,
                UserName = nick,
                RealName = "Trurl Klapaucius, Cyberiad Dicebot"
            };

            //blocks until bot.Stop() is called or 'exit' is entered on the commandline
            bot.Run(server, userInfo, channels.Split(','));
        }
    }
}