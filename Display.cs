using Dice;

static class Display
{
	public static Result Total(string desc, IEnumerable<int> rolls)
	{
		return new Result {
			Description = desc,
			Summary = rolls.Sum().ToString(),
			Verbose = string.Format("[{0}]", string.Join(" ", rolls))
		};
	}
	
	public static Result Successes(string desc, IEnumerable<int> rolls, int target)
	{
		var ss = rolls.Where(r => r >= target);
		var fs = ss.Except(ss);

		return new Result {
			Description = desc,
			Summary = string.Format("[Successes: {0}, Failed: {1}]", ss.Count(), fs.Count()),
			Verbose = string.Format("[{0}]", string.Join(" ", rolls))
		};
	}
}