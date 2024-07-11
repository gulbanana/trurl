using IrcDotNet;
using System;
using System.Collections.Generic;
using System.Linq;
using static trurl.Dice;

namespace trurl;

class DiceBot(bool ignoreEOF, string owner, string[] admins) : BotBase(ignoreEOF)
{
    public override string QuitMessage => "We want the Demon, you see, to extract from the dance of atoms only information that is genuine.";

    protected override IEnumerable<Command> InitializeCommands()
    {
        yield return new("help", Help, ["self-explanatory"]);
        yield return new("join", Join, ["join <channel-name>: join a channel"]);
        yield return new("leave", Leave, ["leave <channel-name>: leave a channel"]);
        yield return new("quit", Quit, ["quit: disconnect from irc"]);
        yield return new("rights", Rights, ["rights: check your privilege"]);
        
        yield return new("roll", Roll, [
            "roll <count> <sides>: roll dice and sum them",
            "roll <count> <sides> <target>: roll dice and test against target numbers"
        ]);

        yield return new("fate", FateRoll, ["fate: roll 4 FATE/FUDGE dice"]);

        yield return new("wod", WodRoll, [
            "wod <count> [<n=10>] [rote] [a<x>] [d<x>] [e<x>] [t<x>]: roll n-again Storyteller dice and report successes",
            "'rote' applies the rote quality",
            "'eX' sets the exceptional success threshhold to X (default 5; 0 will disable it)",
            "'aX' adds X successes ('a2' adds 2, etc)",
            "'dX' adds X extra successes only when a success is rolled (for weapon damage)",
            "'tX' sets the target number to X (default 8)"        
        ]);
        yield return new("chance", WodChanceRoll, ["chance: roll a Storyteller chance die (TN 10, 1 is dramatic failure)"]);

        yield return new("scion", ScionRoll, [
            "scion <count> [<n=10>] [a<x>] [e<x>] [e<x>]: roll n-again dice and report successes",
            "'aX' adds X successes ('a2' adds 2, etc)",
            "'eX' adds X extra successes only when a success is rolled (for enhancements)",
            "'tX' sets the target number to X (default 8)"
        ]);
    }

    private void Help(IrcClient client, IIrcMessageSource source, IList<IIrcMessageTarget> targets, string command, IList<string> parameters)
    {
        CheckParams(parameters, 0, 1);

        var replyTarget = GetDefaultReplyTarget(client, source, targets);
        if (parameters.Count == 0 || !commands.ContainsKey(parameters[0]))
        {
            client.LocalUser.SendMessage(replyTarget, "I know these commands:");
            client.LocalUser.SendMessage(replyTarget, string.Join(", ", commands.Select(kvPair => "!" + kvPair.Key)));
        }
        else
        {
            foreach (var line in commands[parameters[0]].HelpLines)
            {
                client.LocalUser.SendMessage(replyTarget, line);
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

            var desc = $"{count}d{sides} at TN {target}";
            var rolls = N(count, () => D(sides)).ToList();
            var result = Display.Successes(desc, rolls, target);

            DisplayRollResult(client, source, targets, result);
        }
        else
        {
            var desc = $"{count}d{sides}";
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

            if (explode < 8)
            {
                throw new LimitsExceededException(nameof(explode), 8);
            }

            var autos = 0;
            var damage = 0;
            var target = 8;
            int? exceptional = 5;
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
                else if (p.StartsWith('t') && int.TryParse(p[1..], out var t))
                {
                    target = t;
                }
                else if (p.StartsWith('e') && int.TryParse(p[1..], out var e))
                {
                    exceptional = e == 0 ? null : e;
                }
                else if (p == "rote")
                {
                    rote = true;
                }
            }

            var result = StorytellerRoll(count, explode, target, exceptional, autos, damage, rote, false);
            DisplayRollResult(client, source, targets, result);
        }
    }

    private void WodChanceRoll(IrcClient client, IIrcMessageSource source, IList<IIrcMessageTarget> targets, string command, IList<string> parameters)
    {
        var rolls = N(1, () => D(10)).ToList();
        var result = Display.Binary("a chance die", rolls, 10, 1);

        DisplayRollResult(client, source, targets, result);
    }

    private void ScionRoll(IrcClient client, IIrcMessageSource source, IList<IIrcMessageTarget> targets, string command, IList<string> parameters)
    {
        CheckParams(parameters, 1);

        var count = int.Parse(parameters[0]);

        var remainder = parameters.Skip(2);
        if (!(parameters.Count >= 2 && int.TryParse(parameters[1], out var explode)))
        {
            explode = 10;
            remainder = parameters.Skip(1);
        }

        if (explode < 8)
        {
            throw new LimitsExceededException(nameof(explode), 8);
        }

        var autos = 0;
        var enhancements = 0;
        var target = 8;
        foreach (var p in remainder)
        {
            if (p.StartsWith('a') && int.TryParse(p[1..], out var a))
            {
                autos = a;
            }
            if (p.StartsWith('e') && int.TryParse(p[1..], out var e))
            {
                enhancements = e;
            }
            else if (p.StartsWith('t') && int.TryParse(p[1..], out var t))
            {
                target = t;
            }
        }

        var result = StorytellerRoll(count, explode, target, null, autos, enhancements, false, true);
        DisplayRollResult(client, source, targets, result);
    }

    private Result StorytellerRoll(int count, int explode, int targetNumber, int? exceptionalThreshold, int autos, int damage, bool rote, bool botch)
    {
        var qualifiers = new List<string>();
        if (explode <= 10)
        {
            qualifiers.Add($"{explode}-again");
        }

        if (rote)
        {
            qualifiers.Add("rote");
        }

        var desc = $"{count} dice";
        if (qualifiers.Any())
        {
            desc += $" ({string.Join(", ", qualifiers)})";
        }

        var rolls = N(count, () => D(10), explode).ToList();
        var lists = new List<IList<IList<int>>> { rolls };
        var successes = rolls.SelectMany(x => x).Where(r => r >= targetNumber).Count();
        var failures = rolls.SelectMany(x => x).Where(r => r < targetNumber).Count();
        var botches = rolls.SelectMany(x => x).Where(r => r == 1).Count();

        if (rote)
        {
            var roteRolls = N(failures, () => D(10), explode).ToList();
            lists.Add(roteRolls);
            successes += roteRolls.SelectMany(x => x).Where(r => r >= targetNumber).Count();
        }

        var exceptional = exceptionalThreshold.HasValue && successes >= exceptionalThreshold;
        botches = (botch && successes == 0) ? botches : 0;
        return Display.ExplodingSuccesses(desc, targetNumber, successes + autos + (successes > 0 ? damage : 0), botches, exceptional, lists);
    }

    private void DisplayRollResult(IrcClient client, IIrcMessageSource source, IList<IIrcMessageTarget> targets, Result rollResult)
    {
        var replyTarget = GetDefaultReplyTarget(client, source, targets);
        client.LocalUser.SendMessage(replyTarget, $"{source.Name} rolls {rollResult.Description}: {rollResult.Summary}");
        client.LocalUser.SendMessage(replyTarget, rollResult.Verbose);
    }

    private void CheckParams(IList<string> parameters, int min, int? max = null)
    {
        if (parameters.Count < min || (max.HasValue && parameters.Count > max.Value))
        {
            throw new InvalidParametersException(min, max);
        }
    }

    private void CheckOwner(IIrcMessageSource source)
    {
        if (!owner.Equals(source.Name, StringComparison.CurrentCultureIgnoreCase))
        {
            throw new InsufficientPrivilegeException("owner");
        }
    }

    private void CheckAdmin(IIrcMessageSource source)
    {
        if (!admins.Any(a => a.Equals(source.Name, StringComparison.CurrentCultureIgnoreCase)))
        {
            throw new InsufficientPrivilegeException("admin");
        }
    }
}
