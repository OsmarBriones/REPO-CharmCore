using BepInEx.Configuration;
using CharmCore.Net;

namespace CharmCore.Api;

/// <summary>
/// A numeric setting of one charm (for example how much energy Second Wind restores). The host's value applies to
/// everyone; create it with <c>Charm.AddSetting</c> in the charm's constructor.
/// </summary>
public sealed class CharmSetting
{
	private readonly Charm owner;
	private readonly string name;
	private readonly float defaultValue;
	private readonly float min;
	private readonly float max;
	private readonly string description;
	private ConfigEntry<float>? entry;

	internal CharmSetting(Charm owner, string name, float defaultValue, float min, float max, string description)
	{
		this.owner = owner;
		this.name = name;
		this.defaultValue = defaultValue;
		this.min = min;
		this.max = max;
		this.description = description;
	}

	/// <summary>The value in force: the host's inside a room, this machine's otherwise.</summary>
	public float Value => HostSettings.GetFloat(Key, LocalValue);

	internal string Key => owner.Key(name);

	internal float LocalValue => entry?.Value ?? defaultValue;

	internal void Bind(ConfigFile config, string section)
	{
		entry = config.Bind(
			section,
			name,
			defaultValue,
			new ConfigDescription(description + " The host's choice applies to everyone.", new AcceptableValueRange<float>(min, max)));
	}
}
