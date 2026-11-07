using CharmCore.Api;
using CharmCore.Domain;
using Photon.Pun;
using Pockets.Api;
using UnityEngine;

namespace CharmCore.Net;

/// <summary>
/// Runtime component added to every valuable on every client. Wearing a valuable down is the master's job (vanilla
/// only accepts <c>BreakRPC</c> from the master), so a client asks the master with <see cref="RequestWearRpc"/> and
/// the master applies the loss through the same break routine the game uses for hits: the value drops, the floating
/// loss number shows, and below 15% of the original value the valuable is destroyed.
/// </summary>
internal sealed class CharmWear : MonoBehaviourPun
{
	private ValuableObject valuable = null!;

	/// <summary>
	/// A break level the game does not use. Wear requested with it is applied quietly (no sound, smoke or floating number) by
	/// <c>PhysGrabObjectImpactDetector_BreakRPC_Patch</c>; the level travels with the game's own break RPC to every machine.
	/// </summary>
	internal const int QuietBreakLevel = -77;

	private float deadSince = -1f;

	private void Awake()
	{
		valuable = GetComponent<ValuableObject>();
	}

	private void Update()
	{
		// A valuable worn out in a pocket is marked dead by the game but may never be destroyed, because it is hidden
		// and deactivated; it would stay in the slot forever. The master finishes the job after a short grace period.
		if (valuable == null || !SemiFunc.IsMasterClientOrSingleplayer())
		{
			return;
		}

		PhysGrabObject? grabObject = valuable.physGrabObject;
		if (grabObject == null || !grabObject.dead || !PocketsApi.IsPocketed(valuable))
		{
			deadSince = -1f;
			return;
		}

		if (deadSince < 0f)
		{
			deadSince = Time.time;
		}
		else if (Time.time - deadSince > 0.5f)
		{
			deadSince = -1f;
			OwnerCharm()?.LogDebug($"{valuable.name} is worn out in a pocket: destroying it");
			grabObject.DestroyPhysGrabObject();
		}
	}

	/// <summary>Asks for <paramref name="percent"/> of the original value to be worn away (the global multiplier is already included).</summary>
	internal void RequestWear(float percent, bool quiet = false)
	{
		Vector3 at = PlayerAvatar.instance != null ? PlayerAvatar.instance.transform.position + Vector3.up : transform.position;
		if (!SemiFunc.IsMultiplayer() || PhotonNetwork.IsMasterClient)
		{
			ApplyWear(percent, at, quiet);
			return;
		}

		photonView.RPC(nameof(RequestWearRpc), RpcTarget.MasterClient, percent, at, quiet);
	}

	[PunRPC]
	private void RequestWearRpc(float percent, Vector3 at, bool quiet)
	{
		if (!PhotonNetwork.IsMasterClient)
		{
			return;
		}

		ApplyWear(Mathf.Clamp(percent, 0f, 100f), at, quiet);
	}

	private void ApplyWear(float percent, Vector3 at, bool quiet)
	{
		if (valuable == null || !PocketsApi.IsPocketed(valuable))
		{
			return;
		}

		PhysGrabObjectImpactDetector? detector = valuable.physGrabObject != null ? valuable.physGrabObject.impactDetector : null;
		if (detector == null)
		{
			return;
		}

		float lost = WearMath.ValueLost(valuable.dollarValueOriginal, valuable.dollarValueCurrent, percent, 1f);
		if (lost <= 0f)
		{
			return;
		}

		WearContext context = new WearContext(valuable, CharmsApi.CarrierOf(valuable), lost);
		CharmsApi.RunWearHandlers(context);
		lost = context.Loss < 0f ? 0f : System.Math.Min(context.Loss, valuable.dollarValueCurrent);
		if (lost <= 0f)
		{
			return;
		}

		OwnerCharm()?.LogDebug($"Wear {valuable.name}: -{lost:0} (original {valuable.dollarValueOriginal:0}, current {valuable.dollarValueCurrent:0}, {percent:0.#}%)");
		detector.Break(lost, at, quiet ? QuietBreakLevel : detector.breakLevelLight, true);
		OwnerCharm()?.LogDebug($"After wear {valuable.name}: current {valuable.dollarValueCurrent:0}, dead {valuable.physGrabObject != null && valuable.physGrabObject.dead}");
	}

	/// <summary>The charm this valuable belongs to, used to route debug log lines to the right charm's debug switch.</summary>
	private Charm? OwnerCharm()
	{
		return valuable != null ? CharmsApi.CharmOf(valuable) : null;
	}
}
