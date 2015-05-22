using System.Linq;
using static Dice;

static class Commands
{
	public static Result Roll(int count, int sides)
	{
        var desc = string.Format("{0}d{1}", count, sides);
		var rolls = N(count, () => D(sides));
		return Display.Total(desc, rolls);
	}

	public static Result Roll(int count, int sides, int target)
	{
        var desc = string.Format("{0}d{1} at TN {2}", count, sides, target);
		var rolls = N(count, () => D(sides));
		return Display.Successes(desc, rolls, target);
	}

	public static Result Wodroll(int count)
	{
        var desc = string.Format("{0} dice (10-again)", count);
		var rolls = N(count, () => D(10), 10).SelectMany(x => x);
		return Display.Successes(desc, rolls, 8);
	}
}