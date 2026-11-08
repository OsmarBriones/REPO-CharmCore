using ExitGames.Client.Photon;
using Photon.Pun;
using Photon.Realtime;

namespace CharmCore.Net;

/// <summary>
/// Republishes the host's settings when the master changes, so the room always carries the current master's values, and
/// receives the requests that charm owners send to the host.
/// </summary>
internal sealed class CharmRoomEvents : MonoBehaviourPunCallbacks, IOnEventCallback
{
	public override void OnMasterClientSwitched(Player newMasterClient)
	{
		HostSettings.Publish();
	}

	public void OnEvent(EventData photonEvent)
	{
		HostRequests.Receive(photonEvent);
		ChargeEvents.Receive(photonEvent);
	}
}
