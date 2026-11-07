using CharmCore.Api;
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
		if (pocketEvent.PlayerKey != LocalKey() || pocketEvent.Valuable == null || pocketEvent.Slot < 0 || pocketEvent.Slot >= SlotCount)
		{
			return;
		}

		string prefabName = pocketEvent.Valuable.gameObject.name.Replace("(Clone)", string.Empty).Trim();
		Charm? charm = CharmsApi.FindFor(prefabName);
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
