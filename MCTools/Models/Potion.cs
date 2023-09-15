using System.Collections.Generic;
using System;

namespace MCTools.Models
{
	public class Potion
	{
		public string Name { get; set; } = null!;
		public string PotionName { get; set; } = null!;
		public string TippedArrowName { get; set; }
		public Effect Effect { get; set; }
		public List<Effect> Effects { get; set; }
		public bool DisablePotion { get; set; }
		public bool DisableSplashPotion { get; set; }
		public bool DisableLingeringPotion { get; set; }

		public string GetPotionName()
		{
			if (!DisablePotion && PotionName != null)
			{
				if (PotionName.ToLower() == "water")
					return "potion_bottle_drinkable.png";
				return $"potion_bottle_{PotionName.Replace(" ", "_")}.png";
			}
			return null;
		}

		public string GetSplashPotionName()
		{
			if (!DisableSplashPotion && PotionName != null)
			{
				if (PotionName.ToLower() == "water")
					return "potion_bottle_splash.png";
				return $"potion_bottle_splash_{PotionName.Replace(" ", "_")}.png";
			}
			return null;
		}

		public string GetLingeringPotionName()
		{
			if (!DisableLingeringPotion && PotionName != null)
			{
				if (PotionName.ToLower() == "water")
					return "potion_bottle_lingering.png";
				return $"potion_bottle_lingering_{PotionName.Replace(" ", "_")}.png";
			}
			return null;
		}

		public string GetTippedArrowName()
			=> TippedArrowName != null ? $"tipped_arrow_{TippedArrowName.Replace(" ", "_")}.png" : null;

		public int GetColour()
		{
			if (Effect != null)
				return GetAverageTint(new List<Effect> { Effect });
			if (Effects != null)
				return GetAverageTint(Effects);

			Console.WriteLine($"No effect for {Name}");
			return 0;
		}

		private static int GetAverageTint(List<Effect> effects)
		{
			if (effects.Count == 0)
				return 3694022;

			float rSum = 0.0f;
			float gSum = 0.0f;
			float bSum = 0.0f;
			int totalAmplification = 0;

			foreach (var effect in effects)
			{
				int colorValue = effect.Colour.StartsWith("0x") ? Convert.ToInt32(effect.Colour[2..], 16) : Convert.ToInt32(effect.Colour);

				int red = (colorValue >> 16) & 0xFF;
				int green = (colorValue >> 8) & 0xFF;
				int blue = (colorValue >> 0) & 0xFF;

				int amplification = (effect.Amplifier ?? 0) + 1;

				rSum += amplification * red / 255.0f;
				gSum += amplification * green / 255.0f;
				bSum += amplification * blue / 255.0f;

				totalAmplification += amplification;
			}

			if (totalAmplification == 0)
				return 0; // Fully transparent

			double rAvg = (rSum / totalAmplification) * 255.0f;
			double gAvg = (gSum / totalAmplification) * 255.0f;
			double bAvg = (bSum / totalAmplification) * 255.0f;

			return (int)rAvg << 16 | (int)gAvg << 8 | (int)bAvg;
		}
	}

	public class Effect
	{
		public string Colour { get; set; } = null!;
		public int? Amplifier { get; set; }
	}
}
