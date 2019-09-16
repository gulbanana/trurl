using System;
using System.Collections.Generic;
using System.Linq;

namespace trurl
{
	static class Dice
	{
		private static readonly Random gen = new Random();

		public static int D(int sides)
		{
			if (sides > 10000) throw new LimitsExceededException(nameof(sides), 10000);

			return gen.Next(sides)+1;
		}

		public static IEnumerable<int> N(int dice, Func<int> d)
		{
			if (dice > 100) throw new LimitsExceededException(nameof(dice), 100);

			for (int i = 0; i < dice; i++)
				yield return d();
		}

		public static IEnumerable<IList<int>> N(int dice, Func<int> d, int explode)
		{
			if (dice > 100) throw new LimitsExceededException(nameof(dice), 100);

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
}