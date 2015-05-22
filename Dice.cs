using System;
using System.Collections.Generic;

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

	public static IEnumerable<IEnumerable<int>> N(int dice, Func<int> d, int explode)
	{
		for (int i = 0; i < dice; i++)
			yield return E(d, explode);
	}

	public static IEnumerable<int> E(Func<int> d, int explode)
	{
		var roll = 0;
		while (roll < explode)
		{
			roll = d();
			yield return roll;
		} 
	}
}