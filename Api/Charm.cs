using System;
using System.Collections.Generic;
using BepInEx.Configuration;
using CharmCore.Domain;
using CharmCore.Net;
using UnityEngine;

namespace CharmCore.Api;

/// <summary>
/// One charm: the power a particular valuable grants while it sits in the local player's pocket.
/// A charm mod derives from this class, registers one instance with <see cref="CharmsApi.Register"/> and then
/// only has to implement its effect: ask <see cref="IsActive"/>, call <see cref="TryUse()"/> when the power
/// triggers, and let CharmCore handle wear, cooldown, display and the host's settings.
/// </summary>
public abstract class Charm
{
	/// <summary>Valid range of the wear setting, in percent of the valuable's original value.</summary>
	internal const float MaxWearPercent = 100f;

	/// <summary>Longest cooldown a setting may ask for, in seconds.</summary>
	internal const float MaxCooldownSeconds = 600f;

	private readonly List<ValuableObject> carried = new List<ValuableObject>();
	private readonly List<CharmSetting> settings = new List<CharmSetting>();
	private CooldownTimer cooldown;

	private ConfigEntry<bool>? enabledEntry;
	private ConfigEntry<float>? wearEntry;
	private ConfigEntry<float>? cooldownEntry;
	private ConfigEntry<bool>? debugEntry;

	/// <param name="id">Short stable id, used in config sections and network keys (for example "CoffeeCup").</param>
	/// <param name="displayName">Name shown in logs.</param>
	/// <param name="defaultWearPercent">Share of the valuable's original value lost on each use (or per second, as the charm decides).</param>
	/// <param name="defaultCooldownSeconds">Default pause between uses; null when the charm has no cooldown.</param>
	/// <param name="valuablePrefabNames">Prefab names of the valuables that grant this charm, for example "Valuable Arctic Coffee Cup".</param>
	protected Charm(string id, string displayName, float defaultWearPercent, float? defaultCooldownSeconds, params string[] valuablePrefabNames)
	{
		Id = id;
		DisplayName = displayName;
		DefaultWearPercent = defaultWearPercent;
		DefaultCooldownSeconds = defaultCooldownSeconds;
		ValuablePrefabNames = valuablePrefabNames;
	}

	public string Id { get; }

	public string DisplayName { get; }

	public IReadOnlyList<string> ValuablePrefabNames { get; }

	public float DefaultWearPercent { get; }

	public float? DefaultCooldownSeconds { get; }

	/// <summary>
	/// Debug mode of this charm (local setting, off by default): the charm's valuable is placed on the floor near the player
	/// when a level starts, and detailed log lines are written. See <see cref="LogDebug"/>.
	/// </summary>
	public bool DebugMode => debugEntry?.Value ?? false;

	/// <summary>Writes a log line that only matters while debugging; it appears only when <see cref="DebugMode"/> is on.</summary>
	public void LogDebug(string message)
	{
		if (DebugMode)
		{
			CharmCorePlugin.Logger.LogInfo($"[{Id}] {message}");
		}
	}

	/// <summary>True when the host (or you, in singleplayer) has this charm switched on.</summary>
	public bool IsEnabled => HostSettings.GetBool(Key("enabled"), enabledEntry?.Value ?? true);

	/// <summary>Wear per use as the host configured it, already multiplied by the global wear setting.</summary>
	public float WearPercent => Mathf.Clamp(HostSettings.GetFloat(Key("wear"), wearEntry?.Value ?? DefaultWearPercent), 0f, MaxWearPercent) * ConfigurationController.WearMultiplier;

	/// <summary>The pause between uses as the host configured it; 0 when the charm has none.</summary>
	public float CooldownSeconds => DefaultCooldownSeconds.HasValue
		? Mathf.Clamp(HostSettings.GetFloat(Key("cooldown"), cooldownEntry?.Value ?? DefaultCooldownSeconds.Value), 0f, MaxCooldownSeconds)
		: 0f;

	/// <summary>True while the local player carries a valuable that grants this charm and everything is switched on.</summary>
	public bool IsActive => carried.Count > 0 && ConfigurationController.CoreEnabled && IsEnabled;

	/// <summary>Seconds left of the cooldown, 0 when ready.</summary>
	public float CooldownRemaining => cooldown.Remaining(Time.time);

	/// <summary>Active and not cooling down.</summary>
	public bool IsReady => IsActive && cooldown.IsReady(Time.time);

	/// <summary>
	/// Uses the charm: when it is ready, starts the cooldown, wears the valuable down by <see cref="WearPercent"/> and returns true.
	/// Call it at the moment the power would trigger, and only apply the effect when it returns true.
	/// </summary>
	public bool TryUse()
	{
		return TryUse(WearPercent);
	}

	/// <summary>Like <see cref="TryUse()"/> with a wear amount of your own (for example a charm that wears per second).</summary>
	public bool TryUse(float wearPercent)
	{
		if (!IsReady)
		{
			return false;
		}

		cooldown.Start(Time.time, CooldownSeconds);
		LogDebug($"Used: wear {wearPercent:0.#}%, cooldown {CooldownSeconds:0.#} s");
		Wear(wearPercent);
		return true;
	}

	/// <summary>Wears the carried valuable down by a share of its original value without touching the cooldown.</summary>
	public void Wear(float wearPercent)
	{
		if (carried.Count == 0 || wearPercent <= 0f)
		{
			return;
		}

		ValuableObject? valuable = FirstCarried();
		if (valuable == null)
		{
			return;
		}

		CharmWear? wear = valuable.GetComponent<CharmWear>();
		if (wear != null)
		{
			wear.RequestWear(wearPercent);
		}
	}

	/// <summary>Declares a numeric setting of this charm; call it in the constructor. It appears in the charm's config section.</summary>
	protected CharmSetting AddSetting(string name, float defaultValue, float min, float max, string description)
	{
		CharmSetting setting = new CharmSetting(this, name, defaultValue, min, max, description);
		settings.Add(setting);
		return setting;
	}

	/// <summary>Called when the local player starts carrying the valuable (first one of this charm).</summary>
	protected virtual void OnActivated()
	{
	}

	/// <summary>Called when the local player no longer carries any valuable of this charm.</summary>
	protected virtual void OnDeactivated()
	{
	}

	internal float CooldownTotal => cooldown.Total;

	internal void Bind(ConfigFile config)
	{
		string section = "Charm." + Id;
		enabledEntry = config.Bind(section, "Enabled", true, "Turn this charm on or off. The host's choice applies to everyone.");
		wearEntry = config.Bind(
			section,
			"WearPercent",
			Mathf.Clamp(DefaultWearPercent, 0f, MaxWearPercent),
			new ConfigDescription("How much of the valuable's original value each use wears away, in percent (0 to 100). The host's choice applies to everyone.", new AcceptableValueRange<float>(0f, MaxWearPercent)));
		if (DefaultCooldownSeconds.HasValue)
		{
			cooldownEntry = config.Bind(
				section,
				"CooldownSeconds",
				Mathf.Clamp(DefaultCooldownSeconds.Value, 0f, MaxCooldownSeconds),
				new ConfigDescription("Pause between uses, in seconds (0 to 600). 0 means no pause. The host's choice applies to everyone.", new AcceptableValueRange<float>(0f, MaxCooldownSeconds)));
		}

		foreach (CharmSetting setting in settings)
		{
			setting.Bind(config, section);
		}

		debugEntry = config.Bind(
			"Debug",
			"DebugMode",
			false,
			"For testing: when a level starts, this charm's valuable appears on the floor in front of you (host or singleplayer only), and detailed log lines are written. Local setting.");

		config.SettingChanged += (_, _) => HostSettings.Publish();
	}

	internal IReadOnlyList<CharmSetting> Settings => settings;

	internal float LocalWearPercent => wearEntry?.Value ?? DefaultWearPercent;

	internal float? LocalCooldownSeconds => cooldownEntry?.Value;

	internal bool LocalEnabled => enabledEntry?.Value ?? true;

	internal string Key(string name) => "charm." + Id + "." + name;

	internal void Carry(ValuableObject valuable)
	{
		if (carried.Contains(valuable))
		{
			return;
		}

		carried.Add(valuable);
		LogDebug($"Carrying {valuable.name} (value {valuable.dollarValueCurrent:0}); active: {IsActive}");
		if (carried.Count == 1)
		{
			Guard(OnActivated, nameof(OnActivated));
		}
	}

	internal void Drop(ValuableObject valuable)
	{
		if (!carried.Remove(valuable))
		{
			return;
		}

		LogDebug($"No longer carrying {valuable.name}");

		if (carried.Count == 0)
		{
			cooldown.Reset();
			Guard(OnDeactivated, nameof(OnDeactivated));
		}
	}

	internal void DropAll()
	{
		if (carried.Count == 0)
		{
			return;
		}

		carried.Clear();
		cooldown.Reset();
		Guard(OnDeactivated, nameof(OnDeactivated));
	}

	private ValuableObject? FirstCarried()
	{
		for (int i = 0; i < carried.Count; i++)
		{
			if (carried[i] != null)
			{
				return carried[i];
			}
		}

		return null;
	}

	private void Guard(Action action, string name)
	{
		try
		{
			action();
		}
		catch (Exception exception)
		{
			CharmCorePlugin.Logger.LogError($"Charm '{Id}' threw in {name}: {exception}");
		}
	}
}
