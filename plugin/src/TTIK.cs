using System.Diagnostics;
using System.IO;
using BepInEx;
using System.Reflection;
using HarmonyLib;
using System;
using PiUtils.Util;
using UnityEngine;
using Mirror;
using System.Collections;
using PiUtils.Assets;
using TTIK.Network;

namespace TTIK;

[BepInPlugin("de.xenira.ttik", MyPluginInfo.PLUGIN_NAME, MyPluginInfo.PLUGIN_VERSION)]
public class TTIK : BaseUnityPlugin
{
	private static PluginLogger Logger;

	private static AssetLoader assetLoader;
	public static GameObject ikPlayerPrefab;
	public static GameObject trackingTargetPrefab;

	public static string gameExePath = Process.GetCurrentProcess().MainModule.FileName;
	public static string gamePath = Path.GetDirectoryName(gameExePath);

	private void Awake()
	{
		// Plugin startup logic
		Logger = PluginLogger.GetLogger<TTIK>();
		Logger.LogInfo($"Loading plugin {MyPluginInfo.PLUGIN_GUID} version {MyPluginInfo.PLUGIN_VERSION}...");

		License.LogLicense(Logger, "xenira", MyPluginInfo.PLUGIN_GUID, MyPluginInfo.PLUGIN_VERSION);

		hideFlags = HideFlags.HideAndDontSave;

		assetLoader = new AssetLoader(Path.Combine(Path.GetDirectoryName(Info.Location), "assets"));

		ModConfig.Init(Config);

		if (!ModConfig.ModEnabled())
		{
			Logger.LogInfo("Mod is disabled, skipping...");
			return;
		}

		Harmony.CreateAndPatchAll(Assembly.GetExecutingAssembly());

		StartCoroutine(registerPrefabs());

		Logger.LogInfo($"Plugin {MyPluginInfo.PLUGIN_GUID} version {MyPluginInfo.PLUGIN_VERSION} is loaded!");
	}

	private static IEnumerator registerPrefabs()
	{
		Logger.LogInfo("Registering dummy prefab...");
		var bundle = assetLoader.LoadBundle("mirror");

		ikPlayerPrefab = bundle.LoadAsset<GameObject>("DummyPrefab.prefab");
		ikPlayerPrefab.AddComponent<NetworkIkPlayer>();
		ikPlayerPrefab.AddComponent<NetworkTransform>().clientAuthority = true;
		var ikPlayerGuid = new Guid("1b739df5-1600-4a8a-ae21-d2db4304f77b");
		NetworkClient.RegisterPrefab(ikPlayerPrefab, ikPlayerGuid);
		bundle.Unload(false);


		bundle = assetLoader.LoadBundle("mirror");
		trackingTargetPrefab = bundle.LoadAsset<GameObject>("DummyPrefab.prefab");
		trackingTargetPrefab.AddComponent<NetworkTransform>().clientAuthority = true;
		var trackingTargetGuid = new Guid("17305811-edd9-46f4-9080-d10c2e85fff7");
		NetworkClient.RegisterPrefab(trackingTargetPrefab, trackingTargetGuid);

		Logger.LogInfo("Dummy prefab registered!");
		yield return null;
	}
}
