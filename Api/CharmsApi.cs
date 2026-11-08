using System;
using System.Collections.Generic;
using BepInEx.Configuration;
using CharmCore.Net;

namespace CharmCore.Api;

/// <summary>Entry point for charm mods: register your <see cref="Charm"/> once at startup.</summary>
public static class CharmsApi
{
	private static readonly List<Charm> Charms = new List<Charm>();

	/// <summary>Registers a charm and binds its settings (Enabled, WearPercent, CooldownSeconds) in the given config file.</summary>
	public static void Register(Charm charm, ConfigFile config)
	{
		if (charm == null)
		{
			throw new ArgumentNullException(nameof(charm));
		}

		if (config == null)
		{
			throw new ArgumentNullException(nameof(config));
		}

		foreach (Charm existing in Charms)
		{
			if (existing.Id == charm.Id)
			{
				CharmCorePlugin.Logger.LogWarning($"A charm with id '{charm.Id}' is already registered; ignoring the duplicate.");
				return;
			}
		}

		charm.Bind(config);
		Charms.Add(charm);
		CharmOfCache.Clear();
		CharmCorePlugin.Logger.LogInfo($"Charm registered: {charm.DisplayName} ({string.Join(", ", charm.ValuablePrefabNames)})");
	}

	private static readonly List<KeyValuePair<int, Action<PlayerHurtContext>>> HurtHandlers = new List<KeyValuePair<int, Action<PlayerHurtContext>>>();

	/// <summary>
	/// Registers a handler that sees every hit about to damage the local player (only hits the game would really apply).
	/// Handlers run from the lowest <paramref name="priority"/> to the highest; equal priorities run in registration order.
	/// Suggested priorities: 0 for cancelling effects (a coin flip), 10 for last-chance saves, 20 for damage reduction.
	/// </summary>
	public static void OnPlayerHurting(Action<PlayerHurtContext> handler, int priority = 10)
	{
		if (handler == null)
		{
			throw new ArgumentNullException(nameof(handler));
		}

		int index = HurtHandlers.Count;
		while (index > 0 && HurtHandlers[index - 1].Key > priority)
		{
			index--;
		}

		HurtHandlers.Insert(index, new KeyValuePair<int, Action<PlayerHurtContext>>(priority, handler));
	}

	internal static bool HasHurtHandlers => HurtHandlers.Count > 0;

	internal static void RunHurtHandlers(PlayerHurtContext context)
	{
		foreach (KeyValuePair<int, Action<PlayerHurtContext>> entry in HurtHandlers.ToArray())
		{
			if (context.Cancel)
			{
				return;
			}

			try
			{
				entry.Value(context);
			}
			catch (Exception exception)
			{
				CharmCorePlugin.Logger.LogError($"A player-hurting handler threw: {exception}");
			}
		}
	}

	private static readonly List<Action<WearContext>> WearHandlers = new List<Action<WearContext>>();

	/// <summary>Registers a handler that can change how much value a pocketed valuable loses in a wear step. Runs on the host.</summary>
	public static void OnWearing(Action<WearContext> handler)
	{
		if (handler == null)
		{
			throw new ArgumentNullException(nameof(handler));
		}

		WearHandlers.Add(handler);
	}

	internal static void RunWearHandlers(WearContext context)
	{
		foreach (Action<WearContext> handler in WearHandlers.ToArray())
		{
			try
			{
				handler(context);
			}
			catch (Exception exception)
			{
				CharmCorePlugin.Logger.LogError($"A wear handler threw: {exception}");
			}
		}
	}

	private static readonly Dictionary<int, Charm?> CharmOfCache = new Dictionary<int, Charm?>();

	/// <summary>
	/// The charm a valuable belongs to (by its prefab name), or null. Cached by instance id because the name lookup allocates strings
	/// and this is asked often; the cache is dropped when a level ends (<see cref="ClearCache"/>).
	/// </summary>
	public static Charm? CharmOf(ValuableObject valuable)
	{
		if (valuable == null)
		{
			return null;
		}

		int id = valuable.GetInstanceID();
		if (!CharmOfCache.TryGetValue(id, out Charm? charm))
		{
			charm = FindFor(valuable.gameObject.name.Replace("(Clone)", string.Empty).Trim());
			CharmOfCache[id] = charm;
		}

		return charm;
	}

	/// <summary>Forgets the cached charm of each valuable (called when a level ends; its valuables are gone).</summary>
	internal static void ClearCache()
	{
		CharmOfCache.Clear();
	}

	/// <summary>The player who carries <paramref name="valuable"/> in a pocket, or null when nobody does.</summary>
	public static PlayerAvatar? CarrierOf(ValuableObject valuable)
	{
		if (valuable == null || GameDirector.instance == null)
		{
			return null;
		}

		foreach (PlayerAvatar avatar in GameDirector.instance.PlayerList.ToArray())
		{
			if (avatar == null)
			{
				continue;
			}

			foreach (ValuableObject? stored in Pockets.Api.PocketsApi.GetStored(KeyOf(avatar)))
			{
				if (stored == valuable)
				{
					return avatar;
				}
			}
		}

		return null;
	}

	/// <summary>The valuables a player carries in their pockets that belong to <paramref name="charm"/> (works on every machine).</summary>
	public static IReadOnlyList<ValuableObject> CarriedBy(PlayerAvatar avatar, Charm charm)
	{
		List<ValuableObject> result = new List<ValuableObject>();
		if (avatar == null)
		{
			return result;
		}

		foreach (ValuableObject? stored in Pockets.Api.PocketsApi.GetStored(KeyOf(avatar)))
		{
			if (stored != null && CharmOf(stored) == charm)
			{
				result.Add(stored);
			}
		}

		return result;
	}

	/// <summary>Calls <paramref name="action"/> for every player (and the valuable) that carries <paramref name="charm"/> and has it switched on. For effects the host applies for everybody.</summary>
	public static void ForEachCarrier(Charm charm, Action<PlayerAvatar, ValuableObject> action)
	{
		if (charm == null || action == null || GameDirector.instance == null || !ConfigurationController.CoreEnabled || !charm.IsEnabled)
		{
			return;
		}

		List<PlayerAvatar> players = GameDirector.instance.PlayerList;
		for (int index = 0; index < players.Count; index++)
		{
			PlayerAvatar avatar = players[index];
			if (avatar == null || avatar.deadSet)
			{
				continue;
			}

			ValuableObject? carried = FirstCarried(avatar, charm);
			if (carried != null)
			{
				action(avatar, carried);
			}
		}
	}

	/// <summary>
	/// How charged the charm a player carries is, from 0 to 1 (0 when they do not carry it, 1 for a charm that does not charge).
	/// Works for any player on any machine, because the time each charging valuable was stored is tracked from the pocket events
	/// every machine receives. Meant for effects the host applies for other players.
	/// </summary>
	public static float ChargeOf(PlayerAvatar avatar, Charm charm)
	{
		if (charm == null || !charm.UsesCharge)
		{
			return 1f;
		}

		ValuableObject? valuable = FirstCarried(avatar, charm);
		return valuable == null ? 0f : CharmTracker.ChargeFractionOf(valuable, charm.ChargeSeconds);
	}

	/// <summary>
	/// Spends the charge of a valuable (back to 0, and it starts charging again). Call it on the host right after a charging power
	/// was used; every machine is told so that the owner's slot shows the empty charge too.
	/// </summary>
	public static void ConsumeCharge(Charm charm, ValuableObject valuable)
	{
		if (charm == null || valuable == null || !charm.UsesCharge)
		{
			return;
		}

		CharmTracker.SpendCharge(valuable);
		ChargeEvents.Broadcast(charm, valuable);
	}

	/// <summary>The first valuable of <paramref name="charm"/> in the player's pockets, or null. Allocates nothing but Pockets' small copy of the slots.</summary>
	internal static ValuableObject? FirstCarried(PlayerAvatar avatar, Charm charm)
	{
		if (avatar == null)
		{
			return null;
		}

		IReadOnlyList<ValuableObject?> stored = Pockets.Api.PocketsApi.GetStored(KeyOf(avatar));
		for (int slot = 0; slot < stored.Count; slot++)
		{
			ValuableObject? valuable = stored[slot];
			if (valuable != null && CharmOf(valuable) == charm)
			{
				return valuable;
			}
		}

		return null;
	}

	/// <summary>The key Pockets uses for a player: the avatar's view id in multiplayer, -1 alone.</summary>
	internal static int KeyOf(PlayerAvatar avatar)
	{
		return SemiFunc.IsMultiplayer() ? avatar.photonView.ViewID : -1;
	}

	internal static IReadOnlyList<Charm> All => Charms;

	internal static Charm? FindFor(string prefabName)
	{
		foreach (Charm charm in Charms)
		{
			foreach (string name in charm.ValuablePrefabNames)
			{
				if (string.Equals(name, prefabName, StringComparison.OrdinalIgnoreCase))
				{
					return charm;
				}
			}
		}

		return null;
	}
}
