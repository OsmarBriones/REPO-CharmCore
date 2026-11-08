using CharmCore.Api;
using ExitGames.Client.Photon;
using Photon.Pun;
using Photon.Realtime;

namespace CharmCore.Net;

/// <summary>
/// Tells every machine that a charging valuable was spent, so the owner's slot shows the empty charge as well. The host spends a
/// charge (see <c>CharmsApi.ConsumeCharge</c>) and sends the valuable's PhotonView id; each receiver restarts that valuable's charge.
/// Alone there is nothing to tell: the local reset is enough.
/// </summary>
internal static class ChargeEvents
{
	/// <summary>Photon event code (CharmCore uses 183 for host requests; the game itself uses 123 and 124).</summary>
	internal const byte EventCode = 184;

	internal static void Broadcast(Charm charm, ValuableObject valuable)
	{
		if (!SemiFunc.IsMultiplayer() || !PhotonNetwork.IsMasterClient)
		{
			return;
		}

		PhotonView? view = valuable.GetComponent<PhotonView>();
		if (view == null || view.ViewID == 0)
		{
			return;
		}

		object[] content = { charm.Id, view.ViewID };
		PhotonNetwork.RaiseEvent(EventCode, content, new RaiseEventOptions { Receivers = ReceiverGroup.Others }, SendOptions.SendReliable);
	}

	internal static void Receive(EventData photonEvent)
	{
		if (photonEvent.Code != EventCode || photonEvent.CustomData is not object[] content || content.Length < 2 || content[1] is not int viewId)
		{
			return;
		}

		PhotonView? view = PhotonView.Find(viewId);
		ValuableObject? valuable = view != null ? view.GetComponent<ValuableObject>() : null;
		if (valuable != null)
		{
			CharmTracker.StartCharge(valuable);
		}
	}
}
