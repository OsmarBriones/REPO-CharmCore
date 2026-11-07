using BepInEx.Configuration;
using CharmCore.Net;

namespace CharmCore
{
	internal class ConfigurationController
	{
		internal const string EnabledKey = "core.enabled";
		internal const string WearMultiplierKey = "core.wear";

		private static ConfigFile? ConfigFile { get; set; }
		private static ConfigEntry<bool>? Enabled { get; set; }
		private static ConfigEntry<float>? WearMultiplierEntry { get; set; }

		/// <summary>This machine's own setting, which the host publishes to the room.</summary>
		internal static bool LocalCoreEnabled => Enabled?.Value ?? true;

		internal static float LocalWearMultiplier => WearMultiplierEntry?.Value ?? 1f;

		/// <summary>The setting in force: the host's in a room, this machine's otherwise.</summary>
		internal static bool CoreEnabled => HostSettings.GetBool(EnabledKey, LocalCoreEnabled);

		internal static float WearMultiplier => HostSettings.GetFloat(WearMultiplierKey, LocalWearMultiplier);

		internal static void Initialize(ConfigFile config)
		{
			ConfigFile = config;

			Enabled = ConfigFile.Bind("General", nameof(Enabled), true, "Turn all charms on or off. The host's choice applies to everyone.");
			WearMultiplierEntry = ConfigFile.Bind(
				"General",
				"WearMultiplier",
				1f,
				new ConfigDescription("Scales how fast every charm wears its valuable down: 0.5 halves it, 2 doubles it. The host's choice applies to everyone.", new AcceptableValueRange<float>(0.1f, 10f)));

			ConfigFile.SettingChanged += (_, _) => HostSettings.Publish();
			ConfigFile.Save();
		}

		internal static void Reload()
		{
			ConfigFile?.Reload();
			ConfigFile?.Save();
		}
	}
}
