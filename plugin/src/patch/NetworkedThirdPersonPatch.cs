using HarmonyLib;
using Mirror;
using PiUtils.Util;
using TTIK.Network;
using UnityEngine;

namespace TTIK.Patch;

[Harmony]
public class NetworkedThirdPersonPatch
{
	private static PluginLogger Logger = PluginLogger.GetLogger<NetworkedThirdPersonPatch>();

	[HarmonyPatch(typeof(NetworkedPlayer), nameof(NetworkedPlayer.OnStartServer))]
	[HarmonyPostfix]
	public static void OnStartServerPatch(NetworkedPlayer __instance)
	{
		Logger.LogDebug($"Spawning NetworkIkPlayer... {__instance.connection.connectionId}");

		AsyncGameObject.Timeout(() =>
		{
			Logger.LogDebug($"Delayed spawning NetworkIkPlayer for {__instance.connection.connectionId}");
			Logger.LogDebug($"NetId: {__instance.netId}");
			var networkIkPlayer = Object.Instantiate(TTIK.ikPlayerPrefab);
			networkIkPlayer.transform.localPosition = Vector3.zero;
			networkIkPlayer.transform.localRotation = Quaternion.identity;
			NetworkServer.Spawn(networkIkPlayer, __instance.connection);
		}, 5);
	}


}
