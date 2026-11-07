using CharmCore.Net;
using HarmonyLib;
using Photon.Pun;

namespace CharmCore.Patches;

/// <summary>Adds the wear component to every valuable on every client, and refreshes PUN's RPC cache so its RPC is found.</summary>
[HarmonyPatch(typeof(ValuableObject), "Start")]
internal static class ValuableObject_Start_Patch
{
	[HarmonyPostfix]
	private static void Postfix(ValuableObject __instance)
	{
		if (__instance.GetComponent<CharmWear>() != null)
		{
			return;
		}

		__instance.gameObject.AddComponent<CharmWear>();
		PhotonView? view = __instance.GetComponent<PhotonView>();
		if (view != null)
		{
			view.RefreshRpcMonoBehaviourCache();
		}
	}
}
