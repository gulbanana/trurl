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

    public static Result Successes(string desc, IList<int> rolls, int target)
	{
		var ss = rolls.Where(r => r >= target);
		var fs = rolls.Where(r => r < target);

        return new Result {
            Description = desc,
            Summary = string.Format("[Successes: {0}, Failed: {1}]", notable(ss.Count()), notable(fs.Count())),
            Verbose = string.Format("[{0}]", string.Join(" ", rolls.Select(r => (r >= target) ? notable(r) : digit(r))))
		};
	}

    public static Result ExplodingSuccesses(string desc, IList<IList<int>> rolls, int target)
    {
        var ss = rolls.SelectMany(x => x).Where(r => r >= target);
        var fs = rolls.SelectMany(x => x).Where(r => r < target);

        return new Result
        {
            Description = desc,
            Summary = string.Format("[Successes: {0}, Failed: {1}]", notable(ss.Count()), notable(fs.Count())),
            Verbose = string.Format("[{0}]", string.Join(" ", rolls.Select(g => string.Join("->", g.Select(r => (r >= target) ? notable(r) : digit(r))))))
        };
    }

    private static string good(object text)
    {
        return string.Format("\x000309{0}\x03", text);
    }

    private static string bad(object text)
    {
        return string.Format("\x000301{0}\x03", text);
    }

    private static string notable(object text)
    {
        return string.Format("\x000312{0}\x03", text);
    }

    private static string digit(object text)
    {
        return string.Format("\x000305{0}\x03", text);
    }
}