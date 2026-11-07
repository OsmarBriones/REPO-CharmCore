namespace CharmCore.Domain;

/// <summary>A cooldown measured against a clock the caller supplies (game-free, unit testable).</summary>
internal struct CooldownTimer
{
	private float endsAt;
	private float total;

	internal float Total => total;

	internal float Remaining(float now)
	{
		float remaining = endsAt - now;
		return remaining > 0f ? remaining : 0f;
	}

	internal bool IsReady(float now)
	{
		return Remaining(now) <= 0f;
	}

	/// <summary>Starts the cooldown; a duration of 0 or less leaves the timer ready.</summary>
	internal void Start(float now, float seconds)
	{
		if (seconds <= 0f)
		{
			endsAt = 0f;
			total = 0f;
			return;
		}

		endsAt = now + seconds;
		total = seconds;
	}

	internal void Reset()
	{
		endsAt = 0f;
		total = 0f;
	}
}
