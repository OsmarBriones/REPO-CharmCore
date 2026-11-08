namespace CharmCore.Domain;

/// <summary>
/// The charge of one valuable. The charge belongs to the valuable, not to a pocket: it accumulates only while the valuable is stored
/// in a pocket (any player's), it is kept when the valuable is taken out (it just stops growing), another player who stores it
/// continues from where it was, and it is spent back to zero when the power is used. Game-free so it can be unit tested; the clock is
/// supplied by the caller.
/// </summary>
internal struct ChargeTimer
{
	private float accumulated;
	private float storedSince;
	private bool isStored;

	/// <summary>The valuable was put in a pocket: it charges from now on, on top of what it already had.</summary>
	internal void Store(float now)
	{
		if (!isStored)
		{
			storedSince = now;
			isStored = true;
		}
	}

	/// <summary>The valuable left the pocket: what it charged so far is kept, and it stops charging.</summary>
	internal void Remove(float now)
	{
		if (isStored)
		{
			accumulated += Elapsed(now);
			isStored = false;
		}
	}

	/// <summary>The power was used: the charge goes back to zero (and keeps charging if the valuable is still stored).</summary>
	internal void Spend(float now)
	{
		accumulated = 0f;
		if (isStored)
		{
			storedSince = now;
		}
	}

	/// <summary>Seconds of charge so far.</summary>
	internal float Seconds(float now)
	{
		return accumulated + (isStored ? Elapsed(now) : 0f);
	}

	/// <summary>How charged it is, from 0 to 1, when a full charge takes <paramref name="fullSeconds"/> (0 or less means always full).</summary>
	internal float Fraction(float now, float fullSeconds)
	{
		if (fullSeconds <= 0f)
		{
			return 1f;
		}

		float fraction = Seconds(now) / fullSeconds;
		return fraction >= 1f ? 1f : fraction;
	}

	private float Elapsed(float now)
	{
		float elapsed = now - storedSince;
		return elapsed > 0f ? elapsed : 0f;
	}
}
