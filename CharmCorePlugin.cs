using BepInEx;
using CharmCore.Net;
using HarmonyLib;
using Pockets.Api;
using UnityEngine;

namespace CharmCore
{
	[BepInPlugin(PluginGuid, PluginName, PluginVersion)]
	[BepInDependency("com.osmar.Pockets", BepInDependency.DependencyFlags.HardDependency)]
	public class CharmCorePlugin : BaseUnityPlugin
	{
		public const string PluginGuid = "com.osmar.CharmCore";
		public const string PluginName = "CharmCore";
		public const string PluginVersion = "1.0.0";

		internal Harmony? Harmony { get; set; }
		internal static new BepInEx.Logging.ManualLogSource Logger { get; private set; } = null!;

		private void Awake()
		{
			Logger = base.Logger;

			// Prevent the plugin from being deleted
			this.gameObject.transform.parent = null;
			this.gameObject.hideFlags = HideFlags.HideAndDontSave;

			ConfigurationController.Initialize(this.Config);

			// Charms are driven by the value of the valuable, so the slot must always show it.
			PocketsApi.RequireSlotValue(PluginGuid);

			this.gameObject.AddComponent<CharmTracker>();
			this.gameObject.AddComponent<CharmRoomEvents>();

			Harmony = new Harmony(Info.Metadata.GUID);
			Harmony.PatchAll();

			Logger.LogInfo($"{PluginName} {PluginVersion} loaded!");
		}

		internal void Unpatch()
		{
			Harmony?.UnpatchSelf();
		}
	}
}
