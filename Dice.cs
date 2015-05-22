using System;
using System.Collections.Generic;
using System.Linq;

static class Dice
{
	private static readonly Random gen = new Random();

	public static int D(int sides)
	{
		return gen.Next(sides)+1;
	}

	public static IEnumerable<int> N(int dice, Func<int> d)
	{
		for (int i = 0; i < dice; i++)
			yield return d();
	}

	public static IEnumerable<IList<int>> N(int dice, Func<int> d, int explode)
	{
		for (int i = 0; i < dice; i++)
			yield return E(d, explode).ToList();
	}

	public static IEnumerable<int> E(Func<int> d, int explode)
	{
		var roll = 0;
        do
        {
            roll = d();
            yield return roll;
        } while (roll >= explode);
    }
}