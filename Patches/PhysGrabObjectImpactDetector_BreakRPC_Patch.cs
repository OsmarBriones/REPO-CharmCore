using CharmCore.Net;
using HarmonyLib;
using Photon.Pun;

namespace CharmCore.Patches;

/// <summary>
/// Quiet wear. The game's break routine (<c>Break</c> then <c>BreakRPC</c>, which every machine runs) plays a break sound, smoke
/// and a floating loss number each time a valuable loses value. That is right for a use of a charm, but a charm that wears
/// continuously (a few times per second) would tick and flash all the time. CharmCore asks for quiet wear by passing the special
/// break level <see cref="CharmWear.QuietBreakLevel"/>; this prefix then applies only what matters, exactly as the game does
/// (subtract the value, destroy the valuable under 15% of its original value), and skips the effects. Destroying the valuable
/// still plays the game's own destruction effects once, as feedback that the charm is worn out.
/// </summary>
[HarmonyPatch(typeof(PhysGrabObjectImpactDetector), "BreakRPC")]
internal static class PhysGrabObjectImpactDetector_BreakRPC_Patch
{
	[HarmonyPrefix]
	private static bool Prefix(PhysGrabObjectImpactDetector __instance, float valueLost, int breakLevel, bool _loseValue, PhotonMessageInfo _info)
	{
		if (breakLevel != CharmWear.QuietBreakLevel)
		{
			return true;
		}

		// Same sender check the game makes at the top of BreakRPC.
		if (!SemiFunc.MasterOnlyRPC(_info))
		{
			return false;
		}

		ValuableObject? valuable = __instance.valuableObject;
		if (_loseValue && valuable != null)
		{
			valuable.dollarValueCurrent -= valueLost;
			if (valuable.dollarValueCurrent < valuable.dollarValueOriginal * 0.15f && !SemiFunc.RunIsTutorial())
			{
				__instance.DestroyObject();
			}
		}

		return false;
	}
}
