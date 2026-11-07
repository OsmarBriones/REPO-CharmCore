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
