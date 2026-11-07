using CharmCore.Net;
using HarmonyLib;

namespace CharmCore.Patches;

/// <summary>
/// Runs when a playable level loads (before generation finishes): reloads the configuration, forgets the charms of
/// the previous level (pockets are emptied then) and, on the host, publishes the settings to the room.
/// </summary>
[HarmonyPatch(typeof(EnemyDirector), "Start")]
internal static class EnemyDirector_Start_Patch
{
	[HarmonyPostfix]
	private static void Postfix()
	{
		if (!SemiFunc.RunIsLevel())
		{
			return;
		}

		ConfigurationController.Reload();
		CharmTracker.Clear();
		HostSettings.Publish();
		CharmTracker.StartDebugSpawn();
	}
}
