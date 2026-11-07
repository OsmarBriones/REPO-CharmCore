using System.Collections;
using CharmCore.Api;
using Photon.Pun;
using UnityEngine;

namespace CharmCore.Net;

/// <summary>
/// Debug mode helper: places the valuable of every charm that has <see cref="Charm.DebugMode"/> on the floor in front of
/// the player when a level starts, so a charm can be tested without hunting for its valuable. The valuable prefabs live
/// in the game's Resources folder, so they can be spawned in any level; no particular level is needed.
/// </summary>
internal static class DebugSpawner
{
	private const string TinyFolder = "Valuables/01 Tiny/";
	private const float WaitForLevelSeconds = 60f;
	private const float SettleSeconds = 2f;
	private const float SpawnDistance = 2.5f;
	private const float SideStep = 0.5f;

	internal static IEnumerator Run()
	{
		if (!AnyDebugCharm())
		{
			yield break;
		}

		float deadline = Time.time + WaitForLevelSeconds;
		while (Time.time < deadline && !LevelReady())
		{
			yield return null;
		}

		// The player is moved to the start position while the level finishes loading.
		yield return new WaitForSeconds(SettleSeconds);
		if (!LevelReady() || !SemiFunc.RunIsLevel() || !SemiFunc.IsMasterClientOrSingleplayer())
		{
			yield break;
		}

		PlayerAvatar avatar = PlayerAvatar.instance;
		int index = 0;
		foreach (Charm charm in CharmsApi.All)
		{
			if (!charm.DebugMode)
			{
				continue;
			}

			foreach (string prefabName in charm.ValuablePrefabNames)
			{
				Vector3 position = FloorPosition(avatar, index);
				if (Spawn(TinyFolder + prefabName, position, Quaternion.identity))
				{
					charm.LogDebug($"Debug mode: placed {prefabName} at {position}");
					index++;
				}
				else
				{
					CharmCorePlugin.Logger.LogWarning($"Debug mode: could not spawn '{TinyFolder}{prefabName}'.");
				}

				break;
			}
		}
	}

	private static bool AnyDebugCharm()
	{
		foreach (Charm charm in CharmsApi.All)
		{
			if (charm.DebugMode)
			{
				return true;
			}
		}

		return false;
	}

	private static bool LevelReady()
	{
		return LevelGenerator.Instance != null && LevelGenerator.Instance.Generated && PlayerAvatar.instance != null;
	}

	private static Vector3 FloorPosition(PlayerAvatar avatar, int index)
	{
		Transform body = avatar.transform;
		Vector3 flat = Vector3.ProjectOnPlane(body.forward, Vector3.up).normalized;
		Vector3 side = Vector3.Cross(Vector3.up, flat);
		Vector3 target = body.position + flat * SpawnDistance + side * (SideStep * index);

		// Drop it onto the floor under that point.
		if (Physics.Raycast(target + Vector3.up * 2f, Vector3.down, out RaycastHit hit, 6f, SemiFunc.LayerMaskGetVisionObstruct()))
		{
			return hit.point + Vector3.up * 0.2f;
		}

		return target;
	}

	private static bool Spawn(string resourcePath, Vector3 position, Quaternion rotation)
	{
		if (SemiFunc.IsMultiplayer())
		{
			return PhotonNetwork.InstantiateRoomObject(resourcePath, position, rotation, 0) != null;
		}

		GameObject? prefab = Resources.Load<GameObject>(resourcePath);
		if (prefab == null)
		{
			return false;
		}

		Object.Instantiate(prefab, position, rotation);
		return true;
	}
}
