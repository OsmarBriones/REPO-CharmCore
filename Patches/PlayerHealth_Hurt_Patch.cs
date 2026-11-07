using System;
using CharmCore.Api;
using HarmonyLib;

namespace CharmCore.Patches;

/// <summary>
/// The single place where charms see damage to the local player. Almost all damage in the game goes through
/// <c>PlayerHealth.Hurt</c>, which only acts on the damaged player's own machine. Several charms (a coin flip, halved damage,
/// a last-chance save) share this one hook instead of each patching the method, so they combine in a defined order.
/// </summary>
[HarmonyPatch(typeof(PlayerHealth), nameof(PlayerHealth.Hurt))]
internal static class PlayerHealth_Hurt_Patch
{
	[HarmonyPrefix]
	private static bool Prefix(PlayerHealth __instance, ref int damage, bool savingGrace, int enemyIndex, bool hurtByHeal, out PlayerHurtContext? __state)
	{
		__state = null;
		if (!CharmsApi.HasHurtHandlers || !ConfigurationController.CoreEnabled || !WouldApplyDamage(__instance, damage, enemyIndex))
		{
			return true;
		}

		PlayerHurtContext context = new PlayerHurtContext(damage, __instance.health, __instance.maxHealth, savingGrace, enemyIndex, hurtByHeal);
		CharmsApi.RunHurtHandlers(context);
		__state = context;
		damage = context.Damage < 0 ? 0 : context.Damage;
		return !context.Cancel;
	}

	[HarmonyPostfix]
	private static void Postfix(PlayerHealth __instance, PlayerHurtContext? __state)
	{
		if (__state != null && __state.InvincibleSecondsAfter > 0f)
		{
			__instance.InvincibleSet(__state.InvincibleSecondsAfter);
		}
	}

	/// <summary>The same checks the game makes at the top of <c>Hurt</c>, so handlers only see hits that would land.</summary>
	private static bool WouldApplyDamage(PlayerHealth health, int damage, int enemyIndex)
	{
		try
		{
			if (health.isMenuAvatar || damage <= 0 || health.godMode || health.invincibleTimer > 0f)
			{
				return false;
			}

			if (SemiFunc.IsMultiplayer() && !health.photonView.IsMine)
			{
				return false;
			}

			PlayerAvatar avatar = health.playerAvatar;
			if (avatar == null || avatar.deadSet || GameDirector.instance == null || GameDirector.instance.currentState != GameDirector.gameState.Main)
			{
				return false;
			}

			return true;
		}
		catch (Exception exception)
		{
			CharmCorePlugin.Logger.LogWarning($"Could not check whether a hit would land: {exception.Message}");
			return false;
		}
	}
}
