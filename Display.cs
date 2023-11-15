using System.Linq;
using System.Collections.Generic;

static class Display
{
	public static Result Total(string desc, IList<int> rolls)
	{
		return new Result {
			Description = desc,
			Summary = notable(rolls.Sum()),
			Verbose = string.Format("[{0}]", string.Join(" ", rolls.Select(r => digit(r))))
		};
	}

    public static Result FATE(string desc, IList<int> rolls)
    {
        return new Result
        {
            Description = desc,
            Summary = rolls.Sum() > 0 ? notable("+" + rolls.Sum().ToString()) :
                      notable(rolls.Sum()),
            Verbose = string.Format("[{0}]", string.Join(" ", rolls.Select(r => r > 0 ? good("+") : r < 0 ? bad("-") : " ")))
        };
    }

    public static Result Binary(string desc, IList<int> rolls, int successTarget, int failureTarget)
	{
		var ss = rolls.Where(r => r >= successTarget);
		var fs = rolls.Where(r => r <= failureTarget);

        return new Result {
            Description = desc,
            Summary = ss.Count() == rolls.Count() ? "[Success]" : 
                      fs.Count() == rolls.Count() ? $"[{bad("Dramatic")} Failure]" : "[Failure]",
            Verbose = string.Format("[{0}]", string.Join(" ", rolls.Select(r => (r >= successTarget) ? successDigit(r) : (r <= failureTarget ? bad(r) : digit(r)))))
		};
	}

    public static Result Successes(string desc, IList<int> rolls, int target, int extraSuccesses = 0)
	{
		var ss = rolls.Where(r => r >= target);

        return new Result {
            Description = desc,
            Summary = ss.Any() ? string.Format("[Success: {0}]", notable(ss.Count() + extraSuccesses)) : "[Failure]",
            Verbose = string.Format("[{0}]", string.Join(" ", rolls.Select(r => (r >= target) ? successDigit(r) : digit(r))))
		};
	}

    public static Result ExplodingSuccesses(string desc, int successTarget, int successes, int botches, bool exceptional, IList<IList<IList<int>>> lists)
    {
        var resultLists = new List<string>();
        foreach (var rolls in lists)
        {
            var ss = rolls.SelectMany(x => x).Where(r => r >= successTarget);
            resultLists.Add(string.Format("[{0}]", string.Join(" ", rolls.Select(g => string.Join("->", g.Select(r => (r >= successTarget) ? successDigit(r) : ((r == 1 && botches > 0) ? bad(r) : digit(r))))))));
        }

        return new Result
        {
            Description = desc,
            Summary = exceptional ? $"[{good("Exceptional")} Success: {notable(successes)}]" :
                      successes > 0 ? $"[Success: {notable(successes)}]" : 
                      (botches > 0 ? $"[{bad("Dramatic")} Failure]" : "[Failure]"),
            Verbose = string.Join(" ", resultLists)
        };
    }

    private static string good(object text)
    {
        return string.Format("\x000309{0}\x03", text);
    }

    private static string bad(object text)
    {
        return string.Format("\x000304{0}\x03", text);
    }

    private static string notable(object text)
    {
        return string.Format("\x0002{0}\x02", text);
    }

    private static string successDigit(object text)
    {
        return string.Format("{0}", text);
    }

    private static string digit(object text)
    {
        return string.Format("\x000314{0}\x03", text);
    }
}