using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using IrcDotNet;

namespace trurl
{
    class Bot : BotBase
    {
        public Bot() : base() { }

        protected override void InitializeChatCommandProcessors()
        {
            this.ChatCommandProcessors.Add("help", ProcessChatCommandHelp);
            this.ChatCommandProcessors.Add("quit", ProcessChatCommandQuit);
        }

        private void ProcessChatCommandHelp(IrcClient client, IIrcMessageSource source, IList<IIrcMessageTarget> targets, string command, IList<string> parameters)
        {
            CheckParams(parameters, 0);

            // List all commands recognized by this bot.
            var replyTarget = GetDefaultReplyTarget(client, source, targets);
            client.LocalUser.SendMessage(replyTarget, "I know these commands:");
            client.LocalUser.SendMessage(replyTarget, string.Join(", ",
                this.ChatCommandProcessors.Select(kvPair => "!" + kvPair.Key)));
        }

        private void ProcessChatCommandQuit(IrcClient client, IIrcMessageSource source, IList<IIrcMessageTarget> targets, string command, IList<string> parameters)
        {
            CheckParams(parameters, 0);
            CheckAdmin(source);

            this.Stop();
        }

        private void CheckParams(IList<string> parameters, int expected)
        {
            if (parameters.Count != expected) throw new InvalidCommandParametersException(expected);
        }

        private void CheckAdmin(IIrcMessageSource source)
        {
            if (source.Name != "banana") throw new InsufficientPrivilegeException();
        }
    }
}
