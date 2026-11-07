using Photon.Pun;
using Photon.Realtime;

namespace CharmCore.Net;

/// <summary>Republishes the host's settings when the master changes, so the room always carries the current master's values.</summary>
internal sealed class CharmRoomEvents : MonoBehaviourPunCallbacks
{
	public override void OnMasterClientSwitched(Player newMasterClient)
	{
		HostSettings.Publish();
	}
}
