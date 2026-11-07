using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using UnityEngine;

namespace CharmCore
{
	[BepInPlugin(PluginGuid, PluginName, PluginVersion)]
	public class CharmCorePlugin : BaseUnityPlugin
	{
		// Se reemplaza com.osmar por el parámetro AuthorId del template.json
		// y CharmCore por el nombre del proyecto (sourceName).
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
