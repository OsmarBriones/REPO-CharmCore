using CharmCore.Api;
using Photon.Pun;
using Hashtable = ExitGames.Client.Photon.Hashtable;

namespace CharmCore.Net;

/// <summary>
/// Makes the host's settings the ones everybody uses. The master writes them as room properties (Photon delivers them
/// to every client, including late joiners) and clients read them on demand; alone, a machine uses its own values.
/// </summary>
internal static class HostSettings
{
	private static bool UseHostValues => PhotonNetwork.InRoom && !PhotonNetwork.IsMasterClient;

	/// <summary>Master only: publishes this machine's settings, and those of every registered charm, to the room.</summary>
	internal static void Publish()
	{
		if (!PhotonNetwork.InRoom || !PhotonNetwork.IsMasterClient)
		{
			return;
		}

		Hashtable properties = new Hashtable
		{
			{ ConfigurationController.EnabledKey, ConfigurationController.LocalCoreEnabled },
			{ ConfigurationController.WearMultiplierKey, ConfigurationController.LocalWearMultiplier },
		};

		foreach (Charm charm in CharmsApi.All)
		{
			properties[charm.Key("enabled")] = charm.LocalEnabled;
			properties[charm.Key("wear")] = charm.LocalWearPercent;
			if (charm.LocalCooldownSeconds.HasValue)
			{
				properties[charm.Key("cooldown")] = charm.LocalCooldownSeconds.Value;
			}

			foreach (CharmSetting setting in charm.Settings)
			{
				properties[setting.Key] = setting.LocalValue;
			}
		}

		PhotonNetwork.CurrentRoom.SetCustomProperties(properties);
	}

	internal static bool GetBool(string key, bool local)
	{
		if (UseHostValues && PhotonNetwork.CurrentRoom.CustomProperties.TryGetValue(key, out object? value) && value is bool flag)
		{
			return flag;
		}

		return local;
	}

	internal static float GetFloat(string key, float local)
	{
		if (UseHostValues && PhotonNetwork.CurrentRoom.CustomProperties.TryGetValue(key, out object? value) && value is float number)
		{
			return number;
		}

		return local;
	}
}
