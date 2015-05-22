using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using IrcDotNet;
using IrcDotNet.Samples.Common;

namespace trurl
{
    class Bot : BasicIrcBot
    {
        public override IrcRegistrationInfo RegistrationInfo
        {
            get
            {
                return new IrcUserRegistrationInfo()
                {
                    NickName = "trurl",
                    UserName = "trurl",
                    RealName = "Trurl Klapaucius, Cyberiad Dicebot"
                };
            }
        }

        protected override void OnChannelMessageReceived(IrcChannel channel, IrcMessageEventArgs e)
        {
        }

        protected override void OnChannelNoticeReceived(IrcChannel channel, IrcMessageEventArgs e)
        {
        }

        protected override void OnChannelUserJoined(IrcChannel channel, IrcChannelUserEventArgs e)
        {
        }

        protected override void OnChannelUserLeft(IrcChannel channel, IrcChannelUserEventArgs e)
        {
        }

        protected override void OnClientConnect(IrcClient client)
        {
        }

        protected override void OnClientDisconnect(IrcClient client)
        {
        }

        protected override void OnClientRegistered(IrcClient client)
        {
        }

        protected override void OnLocalUserJoinedChannel(IrcLocalUser localUser, IrcChannelEventArgs e)
        {
        }

        protected override void OnLocalUserLeftChannel(IrcLocalUser localUser, IrcChannelEventArgs e)
        {
        }

        protected override void OnLocalUserMessageReceived(IrcLocalUser localUser, IrcMessageEventArgs e)
        {
        }

        protected override void OnLocalUserNoticeReceived(IrcLocalUser localUser, IrcMessageEventArgs e)
        {
        }
    }
}
