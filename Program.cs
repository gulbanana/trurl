using IrcDotNet;
using Microsoft.Extensions.Configuration;
using System.Linq;

namespace trurl;

class Program
{
    static void Main(string[] args)
    {
        var config = new ConfigurationBuilder()
            .AddJsonFile("config.json")
#if DEBUG
            .AddJsonFile("config.Development.json")
#endif
            .Build();                         

        var server = config["server"];
        var nick = config["nick"];
        var channels = config["channels"];
        var owner = config["owner"];
        var admins = config["admins"];

        var bot = new DiceBot(args.Contains("--daemon"), owner, admins.Split(','));
        
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