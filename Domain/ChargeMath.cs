namespace CharmCore.Domain;

/// <summary>Game-free arithmetic of a charging charm, so it can be unit tested.</summary>
internal static class ChargeMath
{
	/// <summary>
	/// How charged a valuable is, from 0 to 1: the time since it started charging divided by the time a full charge takes.
	/// A full-charge time of 0 or less means "always full"; a start in the future counts as just started.
	/// </summary>
	internal static float Fraction(float now, float start, float seconds)
	{
		if (seconds <= 0f)
		{
			return 1f;
		}

		float elapsed = now - start;
		if (elapsed <= 0f)
		{
			return 0f;
		}

		float fraction = elapsed / seconds;
		return fraction >= 1f ? 1f : fraction;
	}
}
