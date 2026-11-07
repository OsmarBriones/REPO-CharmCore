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
	/// <summary>The size folders of the game's valuables, so a charm may use a valuable of any size.</summary>
	private static readonly string[] SizeFolders =
	{
		"01 Tiny", "02 Small", "03 Medium", "04 Big", "05 Wide", "06 Tall", "07 Very Tall",
	};

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

			// One valuable per charm is enough for testing; the first prefab that can be found is used.
			bool placed = false;
			foreach (string prefabName in charm.ValuablePrefabNames)
			{
				string? path = FindResourcePath(prefabName);
				if (path == null)
				{
					continue;
				}

				Vector3 position = FloorPosition(avatar, index);
				if (Spawn(path, position, Quaternion.identity))
				{
					charm.LogDebug($"Debug mode: placed {prefabName} at {position}");
					index++;
					placed = true;
				}

				break;
			}

			if (!placed)
			{
				CharmCorePlugin.Logger.LogWarning($"Debug mode: could not find a valuable to place for charm '{charm.Id}' ({string.Join(", ", charm.ValuablePrefabNames)}).");
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

	/// <summary>Finds the Resources path of a valuable prefab by name, looking through every size folder.</summary>
	private static string? FindResourcePath(string prefabName)
	{
		foreach (string folder in SizeFolders)
		{
			string path = "Valuables/" + folder + "/" + prefabName;
			if (Resources.Load<GameObject>(path) != null)
			{
				return path;
			}
		}

		return null;
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
