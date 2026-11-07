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

	private void Awake()
	{
		valuable = GetComponent<ValuableObject>();
	}

	/// <summary>Asks for <paramref name="percent"/> of the original value to be worn away (the global multiplier is already included).</summary>
	internal void RequestWear(float percent)
	{
		Vector3 at = PlayerAvatar.instance != null ? PlayerAvatar.instance.transform.position + Vector3.up : transform.position;
		if (!SemiFunc.IsMultiplayer() || PhotonNetwork.IsMasterClient)
		{
			ApplyWear(percent, at);
			return;
		}

		photonView.RPC(nameof(RequestWearRpc), RpcTarget.MasterClient, percent, at);
	}

	[PunRPC]
	private void RequestWearRpc(float percent, Vector3 at)
	{
		if (!PhotonNetwork.IsMasterClient)
		{
			return;
		}

		ApplyWear(Mathf.Clamp(percent, 0f, 100f), at);
	}

	private void ApplyWear(float percent, Vector3 at)
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

		detector.Break(lost, at, detector.breakLevelLight, true);
	}
}
