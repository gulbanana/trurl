﻿using System.Collections.Generic;
using System.Linq;
using IrcDotNet;
using static trurl.Dice;
using System;
using System.Text.RegularExpressions;

namespace trurl
{
    class DiceBot : BotBase
    {
        private readonly string owner;
        private readonly string[] admins;

        public DiceBot(string owner, string[] admins) : base() {
            this.owner = owner;
            this.admins = admins;
        }

        protected override void InitializeCommands()
        {
            this.Commands.Add("help", Help);
            this.Commands.Add("join", Join);
            this.Commands.Add("leave", Leave);
            this.Commands.Add("quit", Quit);
            this.Commands.Add("rights", Rights);
            this.Commands.Add("roll", Roll);
            this.Commands.Add("wod", WodRoll);
            this.Commands.Add("chance", WodChanceRoll);
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

                    case "rights":
                        client.LocalUser.SendMessage(replyTarget, "rights: check your privilege");
                        break;

                    case "roll":
                        client.LocalUser.SendMessage(replyTarget, "roll <count> <sides>: roll dice and sum them");
                        client.LocalUser.SendMessage(replyTarget, "roll <count> <sides> <target>: roll dice and test against target numbers");
                        break;

                    case "fate":
                        client.LocalUser.SendMessage(replyTarget, "fate: roll 4 FATE/FUDGE dice");
                        break;

                    case "chance":
                        client.LocalUser.SendMessage(replyTarget, "chance: roll a chance die (TN 10, 1 is dramatic failure)");
                        break;

                    case "wod":
                        client.LocalUser.SendMessage(replyTarget, "wod <count> [<n=10>] [rote] [a<x>] [d<x>] [e<x>]: roll n-again dice and report successes");
                        client.LocalUser.SendMessage(replyTarget, "'rote' applies the rote quality");
                        client.LocalUser.SendMessage(replyTarget, "'eX' sets the exceptional success threshhold to X");
                        client.LocalUser.SendMessage(replyTarget, "'aX' adds X successes ('a2' adds 2, etc)");
                        client.LocalUser.SendMessage(replyTarget, "'dX' adds X successes only in the event of a hit (like weapon damage)");
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
            CheckOwner(source);

            this.Stop();
        }

        private void Rights(IrcClient client, IIrcMessageSource source, IList<IIrcMessageTarget> targets, string command, IList<string> parameters)
        {
            var replyTarget = GetDefaultReplyTarget(client, source, targets);

            if (owner.Equals(source.Name, StringComparison.CurrentCultureIgnoreCase)) 
            {
                client.LocalUser.SendMessage(replyTarget, "you are an owner, and may execute any command");
            }
            else if (admins.Any(a => a.Equals(source.Name, StringComparison.CurrentCultureIgnoreCase))) 
            {
                client.LocalUser.SendMessage(replyTarget, "you are an admin, and may execute any command except !quit");
            }
            else
            {
                client.LocalUser.SendMessage(replyTarget, "you have no rights");
            }
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
            CheckParams(parameters, 1);

            var count = int.Parse(parameters[0]);

            if (parameters.Count == 1 && count == 0)
            {
                WodChanceRoll(client, source, targets, command, new List<string>());
            }
            else
            {
                var remainder = parameters.Skip(2);
                if (!(parameters.Count >= 2 && int.TryParse(parameters[1], out var explode)))
                {
                    explode = 10;
                    remainder = parameters.Skip(1);
                }

                if (explode < 8) throw new LimitsExceededException(nameof(explode), 8);

                var autos = 0;
                var damage = 0;
                var exceptional = 5;
                var rote = false;
                foreach (var p in remainder)
                {
                    if (p.StartsWith('a') && int.TryParse(p[1..], out var a))
                    {
                        autos = a;
                    }
                    if (p.StartsWith('d') && int.TryParse(p[1..], out var d))
                    {
                        damage = d;
                    }
                    else if (p.StartsWith('e') && int.TryParse(p[1..], out var e))
                    {
                        exceptional = e;
                    }
                    else if (p == "rote")
                    {
                        rote = true;
                    }
                }

                var desc = explode <= 10 ? string.Format("{0} dice ({1}-again)", count, explode) : $"{count} dice";
                var rolls = N(count, () => D(10), explode).ToList();
                var successes = rolls.SelectMany(x => x).Where(r => r >= 8);
                var result = Display.ExplodingSuccesses(desc, rolls, 8, exceptional, autos + (successes.Any() ? damage : 0));

                DisplayRollResult(client, source, targets, result);
            }
        }

        private void WodChanceRoll(IrcClient client, IIrcMessageSource source, IList<IIrcMessageTarget> targets, string command, IList<string> parameters)
        {
            var rolls = N(1, () => D(10)).ToList();
            var result = Display.Binary("a chance die", rolls, 10, 1);

            DisplayRollResult(client, source, targets, result);
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

        private void CheckOwner(IIrcMessageSource source)
        {
            if (!owner.Equals(source.Name, StringComparison.CurrentCultureIgnoreCase)) throw new InsufficientPrivilegeException("owner");
        }

        private void CheckAdmin(IIrcMessageSource source)
        {
            if (!admins.Any(a => a.Equals(source.Name, StringComparison.CurrentCultureIgnoreCase))) throw new InsufficientPrivilegeException("admin");
        }
    }
}
