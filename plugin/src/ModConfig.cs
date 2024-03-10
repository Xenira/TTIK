using BepInEx.Configuration;
using UnityEngine;

namespace TTIK;

public class ModConfig
{
	// General
	private static ConfigEntry<bool> modEnabled;

	public static void Init(ConfigFile config)
	{
		// General
		modEnabled = config.Bind("General", "Enabled", true, "Enable mod");
	}

	public static bool ModEnabled()
	{
		return modEnabled.Value;
	}
}
