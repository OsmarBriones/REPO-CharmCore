using System.Collections.Generic;
using CharmCore.Api;
using CharmCore.Domain;
using Photon.Pun;
using Pockets.Api;
using UnityEngine;

namespace CharmCore.Net;

/// <summary>
/// Follows the local player's pockets through the Pockets API and switches the matching charms on and off.
/// Only the local player's pockets matter: a charm is the power its carrier gets.
/// </summary>
internal sealed class CharmTracker : MonoBehaviour
{
	private const int SlotCount = 3;

	private static readonly ValuableObject?[] SlotValuables = new ValuableObject?[SlotCount];
	private static readonly Charm?[] SlotCharms = new Charm?[SlotCount];

	private static CharmTracker? instance;

	/// <summary>
	/// The charge of each charging valuable (by instance id on this machine). It belongs to the valuable: it grows while the valuable is
	/// stored in any player's pocket, is kept when it is taken out and continues for whoever stores it next. Cleared when a level ends.
	/// </summary>
	private static readonly Dictionary<int, ChargeTimer> Charges = new Dictionary<int, ChargeTimer>();

	/// <summary>The valuable was stored in a pocket: it charges on top of what it already had.</summary>
	private static void ChargeStored(ValuableObject valuable)
	{
		int id = valuable.GetInstanceID();
		Charges.TryGetValue(id, out ChargeTimer timer);
		timer.Store(Time.time);
		Charges[id] = timer;
	}

	/// <summary>The valuable left a pocket: its charge is kept and stops growing.</summary>
	private static void ChargeRemoved(ValuableObject valuable)
	{
		int id = valuable.GetInstanceID();
		if (Charges.TryGetValue(id, out ChargeTimer timer))
		{
			timer.Remove(Time.time);
			Charges[id] = timer;
		}
	}

	/// <summary>The power was used: the charge of the valuable goes back to zero.</summary>
	internal static void SpendCharge(ValuableObject valuable)
	{
		int id = valuable.GetInstanceID();
		Charges.TryGetValue(id, out ChargeTimer timer);
		timer.Spend(Time.time);
		Charges[id] = timer;
	}

	/// <summary>
	/// Charge of a carried valuable, 0 to 1. A valuable whose storing was missed (for example a pocket event that arrived before the charm
	/// registered) is treated as stored now.
	/// </summary>
	internal static float ChargeFractionOf(ValuableObject valuable, float seconds)
	{
		if (seconds <= 0f)
		{
			return 1f;
		}

		int id = valuable.GetInstanceID();
		if (!Charges.TryGetValue(id, out ChargeTimer timer))
		{
			timer.Store(Time.time);
			Charges[id] = timer;
		}

		return timer.Fraction(Time.time, seconds);
	}

	/// <summary>The charge shown on a slot, when the charm in it charges and is not full yet.</summary>
	internal static bool TryGetCharge(int slot, out float fraction)
	{
		fraction = 1f;
		if (slot < 0 || slot >= SlotCount || SlotCharms[slot] == null || SlotValuables[slot] == null || !SlotCharms[slot]!.UsesCharge)
		{
			return false;
		}

		fraction = ChargeFractionOf(SlotValuables[slot]!, SlotCharms[slot]!.ChargeSeconds);
		return fraction < 0.9999f;
	}

	/// <summary>Starts the debug spawner for the level that is loading (it does nothing unless some charm has debug mode on).</summary>
	internal static void StartDebugSpawn()
	{
		if (instance != null)
		{
			instance.StartCoroutine(DebugSpawner.Run());
		}
	}

	private void OnEnable()
	{
		instance = this;
		PocketsApi.Stored += OnStored;
		PocketsApi.Removed += OnRemoved;
	}

	private void OnDisable()
	{
		PocketsApi.Stored -= OnStored;
		PocketsApi.Removed -= OnRemoved;
	}

	private float nextSpawnSwitchCheck;
	private float nextLevelCheck;
	private bool wasInLevel;

	private void Update()
	{
		if (Time.unscaledTime >= nextSpawnSwitchCheck)
		{
			nextSpawnSwitchCheck = Time.unscaledTime + 1f;
			DebugSpawner.ServeSpawnSwitches();
		}

		// Leaving a level: forget everything about it now, not when the next level starts, so no static field keeps pointing at the
		// level's objects while the game unloads them.
		if (Time.unscaledTime >= nextLevelCheck)
		{
			nextLevelCheck = Time.unscaledTime + 0.5f;
			bool inLevel = IsInLevel();
			if (wasInLevel && !inLevel)
			{
				Clear();
			}

			wasInLevel = inLevel;
		}

		// A worn-out valuable is destroyed without a pocket event; drop its charm when that happens.
		for (int slot = 0; slot < SlotCount; slot++)
		{
			if (SlotCharms[slot] != null && SlotValuables[slot] == null)
			{
				Release(slot);
			}
		}
	}

	/// <summary>Forgets everything, for example when a new level starts (pockets are emptied then).</summary>
	internal static void Clear()
	{
		for (int slot = 0; slot < SlotCount; slot++)
		{
			Release(slot);
		}

		foreach (Charm charm in CharmsApi.All)
		{
			charm.DropAll();
			charm.NotifyLevelEnded();
		}

		CharmsApi.ClearCache();
		Charges.Clear();
	}

	/// <summary>The cooldown shown on a slot, when the charm in it has one running.</summary>
	internal static bool TryGetCooldown(int slot, out float remaining, out float total)
	{
		remaining = 0f;
		total = 0f;
		if (slot < 0 || slot >= SlotCount || SlotCharms[slot] == null)
		{
			return false;
		}

		Charm charm = SlotCharms[slot]!;
		remaining = charm.CooldownRemaining;
		total = charm.CooldownTotal;
		return remaining > 0f && total > 0f;
	}

	private static bool IsInLevel()
	{
		return RunManager.instance != null && SemiFunc.RunIsLevel();
	}

	private static void OnStored(PocketEvent pocketEvent)
	{
		// Charging starts when any player stores the valuable (and continues from the charge it already had); the host needs it for other players' charms.
		if (pocketEvent.Valuable != null)
		{
			Charm? charging = CharmsApi.CharmOf(pocketEvent.Valuable);
			if (charging != null && charging.UsesCharge)
			{
				ChargeStored(pocketEvent.Valuable);
			}
		}

		if (pocketEvent.PlayerKey != LocalKey() || pocketEvent.Valuable == null || pocketEvent.Slot < 0 || pocketEvent.Slot >= SlotCount)
		{
			return;
		}

		Charm? charm = CharmsApi.CharmOf(pocketEvent.Valuable);
		if (charm == null)
		{
			return;
		}

		Release(pocketEvent.Slot);
		SlotValuables[pocketEvent.Slot] = pocketEvent.Valuable;
		SlotCharms[pocketEvent.Slot] = charm;
		charm.Carry(pocketEvent.Valuable);
	}

	private static void OnRemoved(PocketEvent pocketEvent)
	{
		if (pocketEvent.Valuable != null)
		{
			ChargeRemoved(pocketEvent.Valuable);
		}

		if (pocketEvent.PlayerKey != LocalKey() || pocketEvent.Slot < 0 || pocketEvent.Slot >= SlotCount)
		{
			return;
		}

		Release(pocketEvent.Slot);
	}

	private static void Release(int slot)
	{
		Charm? charm = SlotCharms[slot];
		ValuableObject? valuable = SlotValuables[slot];
		SlotCharms[slot] = null;
		SlotValuables[slot] = null;
		if (charm == null)
		{
			return;
		}

		if (valuable != null)
		{
			charm.Drop(valuable);
		}
		else
		{
			charm.DropAll();
		}
	}

	/// <summary>The local player's key the way Pockets identifies players: avatar view id in multiplayer, -1 alone.</summary>
	private static int LocalKey()
	{
		PlayerAvatar? avatar = PlayerAvatar.instance;
		if (avatar == null)
		{
			return int.MinValue;
		}

		return SemiFunc.IsMultiplayer() ? avatar.photonView.ViewID : -1;
	}
}
