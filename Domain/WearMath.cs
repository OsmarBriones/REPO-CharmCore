using System;

namespace CharmCore.Domain;

/// <summary>Game-free wear arithmetic, so it can be unit tested.</summary>
internal static class WearMath
{
	/// <summary>
	/// The dollar value a wear step removes: <paramref name="percent"/> of the valuable's original value (scaled by
	/// <paramref name="multiplier"/>), at least 1, never more than what is left.
	/// </summary>
	internal static float ValueLost(float original, float current, float percent, float multiplier)
	{
		if (original <= 0f || current <= 0f || percent <= 0f || multiplier <= 0f)
		{
			return 0f;
		}

		float lost = (float)Math.Round(original * (percent / 100f) * multiplier);
		return Math.Min(Math.Max(lost, 1f), current);
	}

	/// <summary>Remaining fraction of a cooldown, 0 when it is over or there is none.</summary>
	internal static float CooldownFraction(float remaining, float total)
	{
		if (total <= 0f || remaining <= 0f)
		{
			return 0f;
		}

		return Math.Min(remaining / total, 1f);
	}
}
