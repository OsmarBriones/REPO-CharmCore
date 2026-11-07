using CharmCore.Ui;
using HarmonyLib;

namespace CharmCore.Patches;

/// <summary>Makes sure every inventory slot has the cooldown overlay component.</summary>
[HarmonyPatch(typeof(InventorySpot), nameof(InventorySpot.UpdateUI))]
internal static class InventorySpot_UI_Patch
{
	[HarmonyPostfix]
	private static void Postfix(InventorySpot __instance)
	{
		CharmSlotView.GetOrAdd(__instance);
	}
}
