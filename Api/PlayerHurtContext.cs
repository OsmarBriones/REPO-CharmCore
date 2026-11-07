namespace CharmCore.Api;

/// <summary>
/// A hit that is about to damage the local player, offered to every handler registered with
/// <see cref="CharmsApi.OnPlayerHurting"/>. It is only created when the game would really apply the damage (the player is
/// not invincible, not dead, and so on), so a handler can wear its charm without wasting a use. Handlers run in priority
/// order; change <see cref="Damage"/> or set <see cref="Cancel"/> to protect the player.
/// </summary>
public sealed class PlayerHurtContext
{
	internal PlayerHurtContext(int damage, int health, int maxHealth, bool savingGrace, int enemyIndex, bool hurtByHeal)
	{
		Damage = damage;
		Health = health;
		MaxHealth = maxHealth;
		SavingGrace = savingGrace;
		EnemyIndex = enemyIndex;
		HurtByHeal = hurtByHeal;
	}

	/// <summary>Damage the player is about to take. Lower it to soften the hit.</summary>
	public int Damage { get; set; }

	/// <summary>Set to true to cancel the hit completely (no damage, no hurt effect, even a lethal one).</summary>
	public bool Cancel { get; set; }

	/// <summary>Seconds of invincibility to give the player right after this hit (0 for none).</summary>
	public float InvincibleSecondsAfter { get; set; }

	/// <summary>The player's health before the hit.</summary>
	public int Health { get; }

	public int MaxHealth { get; }

	/// <summary>The game's "saving grace" flag for this hit (a small hit at low health may leave a few points).</summary>
	public bool SavingGrace { get; }

	/// <summary>Index of the monster that caused the hit, or -1 when it comes from the environment or is unknown.</summary>
	public int EnemyIndex { get; }

	/// <summary>True when the damage comes from a heal-drain effect.</summary>
	public bool HurtByHeal { get; }

	/// <summary>True when the hit, as it stands, would take the player to 0 health.</summary>
	public bool IsLethal => !Cancel && Damage >= Health;
}
