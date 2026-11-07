using CharmCore.Api;
using ExitGames.Client.Photon;
using Photon.Pun;
using Photon.Realtime;

namespace CharmCore.Net;

/// <summary>
/// Lets a charm ask the host to do something only the host may do (move a valuable, hurt a monster, spawn an enemy). The
/// owner sends a small message tagged with the charm id; the host checks that the sender really carries that charm and
/// hands the request to <see cref="Charm"/>'s <c>OnHostRequest</c>. Alone or as host the request is handled on the spot.
/// </summary>
internal static class HostRequests
{
	/// <summary>Photon event code used by CharmCore (the game itself uses 123 and 124).</summary>
	internal const byte EventCode = 183;

	internal static void Send(Charm charm, string action, object[] args)
	{
		if (!SemiFunc.IsMultiplayer() || PhotonNetwork.IsMasterClient)
		{
			PlayerAvatar? local = PlayerAvatar.instance;
			if (local != null)
			{
				charm.HandleHostRequest(local, action, args);
			}

			return;
		}

		object[] content = { charm.Id, action, args };
		PhotonNetwork.RaiseEvent(EventCode, content, new RaiseEventOptions { Receivers = ReceiverGroup.MasterClient }, SendOptions.SendReliable);
	}

	internal static void Receive(EventData photonEvent)
	{
		if (photonEvent.Code != EventCode || !PhotonNetwork.IsMasterClient)
		{
			return;
		}

		if (photonEvent.CustomData is not object[] content || content.Length < 3 || content[0] is not string charmId || content[1] is not string action)
		{
			return;
		}

		object[] args = content[2] as object[] ?? new object[0];
		Charm? charm = null;
		foreach (Charm candidate in CharmsApi.All)
		{
			if (candidate.Id == charmId)
			{
				charm = candidate;
				break;
			}
		}

		PlayerAvatar? sender = AvatarOf(photonEvent.Sender);
		if (charm == null || sender == null || !charm.IsEnabled || !ConfigurationController.CoreEnabled || CharmsApi.CarriedBy(sender, charm).Count == 0)
		{
			return;
		}

		charm.HandleHostRequest(sender, action, args);
	}

	private static PlayerAvatar? AvatarOf(int actorNumber)
	{
		foreach (PlayerAvatar avatar in GameDirector.instance.PlayerList)
		{
			if (avatar != null && avatar.photonView != null && avatar.photonView.OwnerActorNr == actorNumber)
			{
				return avatar;
			}
		}

		return null;
	}
}
