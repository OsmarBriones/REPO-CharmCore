using System;
using System.Collections.Generic;
using BepInEx.Configuration;

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
			if (stored != null && FindFor(stored.gameObject.name.Replace("(Clone)", string.Empty).Trim()) == charm)
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

		foreach (PlayerAvatar avatar in GameDirector.instance.PlayerList.ToArray())
		{
			if (avatar == null || avatar.deadSet)
			{
				continue;
			}

			IReadOnlyList<ValuableObject> carried = CarriedBy(avatar, charm);
			if (carried.Count > 0)
			{
				action(avatar, carried[0]);
			}
		}
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
