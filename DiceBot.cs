using System.Collections.Generic;
using System.Linq;
using IrcDotNet;
using static Dice;
using System;

namespace trurl
{
    class DiceBot : BotBase
    {
        private readonly string[] admins;

        public DiceBot(string[] adminList) : base() {
            admins = adminList;
        }

        protected override void InitializeCommands()
        {
            this.Commands.Add("help", Help);
            this.Commands.Add("join", Join);
            this.Commands.Add("leave", Leave);
            this.Commands.Add("quit", Quit);
            this.Commands.Add("roll", Roll);
            this.Commands.Add("wod", WodRoll);
            this.Commands.Add("fate", FateRoll);
        }

        private void Help(IrcClient client, IIrcMessageSource source, IList<IIrcMessageTarget> targets, string command, IList<string> parameters)
        {
            CheckParams(parameters, 0, 1);

            var replyTarget = GetDefaultReplyTarget(client, source, targets);
            if (parameters.Count == 0)
            {
                client.LocalUser.SendMessage(replyTarget, "I know these commands:");
                client.LocalUser.SendMessage(replyTarget, string.Join(", ", this.Commands.Select(kvPair => "!" + kvPair.Key)));
            }
            else
            {
                switch (parameters[0])
                {
                    case "help":
                        client.LocalUser.SendMessage(replyTarget, "self-explanatory");
                        break;

                    case "join":
                        client.LocalUser.SendMessage(replyTarget, "join <channel-name>: joins a channel");
                        break;

                    case "leave":
                        client.LocalUser.SendMessage(replyTarget, "leave <channel-name>: leaves a channel");
                        break;

                    case "quit":
                        client.LocalUser.SendMessage(replyTarget, "quit: disconnects from irc");
                        break;

                    case "roll":
                        client.LocalUser.SendMessage(replyTarget, "roll <count> <sides>: roll dice and sum them");
                        client.LocalUser.SendMessage(replyTarget, "roll <count> <sides> <target>: roll dice and test against target numbers");
                        break;

                    case "fate":
                        client.LocalUser.SendMessage(replyTarget, "fate: roll 4 FATE/FUDGE dice");
                        break;

                    case "wod":
                        client.LocalUser.SendMessage(replyTarget, "wodroll <count>: roll 10-again dice and report successes");
                        client.LocalUser.SendMessage(replyTarget, "wodroll <count> <n>: roll n-again dice and report successes");
                        break;
                }
            }
        }

        private void Join(IrcClient client, IIrcMessageSource source, IList<IIrcMessageTarget> targets, string command, IList<string> parameters)
        {
            CheckParams(parameters, 1);
            CheckAdmin(source);

            this.Join(parameters[0]);
        }

        private void Leave(IrcClient client, IIrcMessageSource source, IList<IIrcMessageTarget> targets, string command, IList<string> parameters)
        {
            CheckParams(parameters, 1);
            CheckAdmin(source);

            this.Leave(parameters[0]);
        }

        private void Quit(IrcClient client, IIrcMessageSource source, IList<IIrcMessageTarget> targets, string command, IList<string> parameters)
        {
            CheckParams(parameters, 0);
            CheckAdmin(source);

            this.Stop();
        }

        private void Roll(IrcClient client, IIrcMessageSource source, IList<IIrcMessageTarget> targets, string command, IList<string> parameters)
        {
            CheckParams(parameters, 2, 3);

            var count = int.Parse(parameters[0]);
            var sides = int.Parse(parameters[1]);

            if (parameters.Count == 3)
            {
                var target = int.Parse(parameters[2]);

                var desc = string.Format("{0}d{1} at TN {2}", count, sides, target);
                var rolls = N(count, () => D(sides)).ToList();
                var result = Display.Successes(desc, rolls, target);

                DisplayRollResult(client, source, targets, result);
            }
            else
            {
                var desc = string.Format("{0}d{1}", count, sides);
                var rolls = N(count, () => D(sides)).ToList();
                var result = Display.Total(desc, rolls);

                DisplayRollResult(client, source, targets, result);
            }
        }

        private void FateRoll(IrcClient client, IIrcMessageSource source, IList<IIrcMessageTarget> targets, string command, IList<string> parameters)
        {
            CheckParams(parameters, 0, 0);

            var rolls = N(4, () => D(3) - 2).ToList();

            var result = Display.FATE("4dF", rolls);

            DisplayRollResult(client, source, targets, result);
        }

        private void WodRoll(IrcClient client, IIrcMessageSource source, IList<IIrcMessageTarget> targets, string command, IList<string> parameters)
        {
            CheckParams(parameters, 1, 2);

            var count = int.Parse(parameters[0]);

            if (parameters.Count == 2)
            {
                var explode = int.Parse(parameters[1]);
                if (explode < 2) throw new Exception("minimum n-value = 2");

                var desc = string.Format("{0} dice ({1}-again)", count, explode);
                var rolls = N(count, () => D(10), explode).ToList();
                var result = Display.ExplodingSuccesses(desc, rolls, 8);

                DisplayRollResult(client, source, targets, result);
            }
            else
            {
                var desc = string.Format("{0} dice (10-again)", count);
                var rolls = N(count, () => D(10), 10).ToList();
                var result = Display.ExplodingSuccesses(desc, rolls, 8);

                DisplayRollResult(client, source, targets, result);
            }
        }

        private void DisplayRollResult(IrcClient client, IIrcMessageSource source, IList<IIrcMessageTarget> targets, Result rollResult)
        {
            var replyTarget = GetDefaultReplyTarget(client, source, targets);
            client.LocalUser.SendMessage(replyTarget, string.Format("{0} rolls {1}: {2}", source.Name, rollResult.Description, rollResult.Summary));
            client.LocalUser.SendMessage(replyTarget, rollResult.Verbose);
        }

        private void CheckParams(IList<string> parameters, int min, int? max = null)
        {
            if (parameters.Count < min || (max.HasValue && parameters.Count > max.Value))
                throw new InvalidCommandParametersException(min, max);
        }

        private void CheckAdmin(IIrcMessageSource source)
        {
            if (!admins.Any(a => a.ToLowerInvariant().Equals(source.Name.ToLowerInvariant()))) throw new InsufficientPrivilegeException();
        }
    }
}
