using BepInEx.Configuration;
using UnityEngine;

namespace TTIK;

public class ModConfig
{
	// General
	private static ConfigEntry<bool> modEnabled;

	// Sync
	public static ConfigEntry<float> fingerSyncDeadzone;

	public static void Init(ConfigFile config)
	{
		// General
		modEnabled = config.Bind("General", "Enabled", true, "Enable mod");

		// Sync
		fingerSyncDeadzone = config.Bind("Sync", "Finger Sync Deadzone", 0.01f, "Deadzone for finger sync. If the difference between the local and remote finger position is less than this value, the remote finger will be set to the local finger position.");
	}

	public static bool ModEnabled()
	{
		return modEnabled.Value;
	}
}
