using System;
using System.Collections.Generic;

namespace Code
{
	public class SwampThing : Thing
	{
		public string? SwampName { get; set; }

		public int License { get; set; }

		public long Value { get; set; }
	}

	public class SpaceThing : Thing
	{
		public string? StarshipName { get; set; }

		public int? License { get; set; }

		public string? Value { get; set; } = "Hello world";
	}

	public class Thing
	{
		public int Id { get; set; }

		public string? Text { get; set; }

		public DayOfWeek Day { get; set; }

		public bool? Flag { get; set; }

		public static List<Thing> GenerateThings(int count, bool useDerivedTyped = true)
		{
			List<Thing> Things = new List<Thing>(count);
			for (int i = 0; i < count; i++)
			{
				Thing Thing = CreateThing(i, useDerivedTyped);

				Thing.Id = i;
				Thing.Text = i % 2 == 0
					? "Even"
					: i % 3 == 0
						? "Odd"
						: null;
				Thing.Day = (DayOfWeek)(i % 7);
				Thing.Flag = i % 4 == 0
					? true
					: i % 5 == 0
						? false
						: (bool?)null;

				Things.Add(Thing);
			}
			return Things;
		}

		private static Thing CreateThing(int i, bool useDerivedTyped)
		{
			if (!useDerivedTyped)
				return new Thing();

			switch (i % 3)
			{
				case 1:
					return new SpaceThing()
					{
						StarshipName = $"Ship {i}",
						License = i % 4 == 0
							? (i * 10000) + i
							: (int?)null
					};
				case 2:
					return new SwampThing
					{
						SwampName = $"Swamp {i}",
						License = (i * 10000) + i
					};
				default:
					return new Thing();
			}
		}
	}
}
