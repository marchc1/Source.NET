global using static Source.Engine.Steam.SteamworksExts;

using Steamworks;

using System.Reflection;

namespace Source.Engine.Steam;

// These are some hacks to access the internal pointers for steamclient
// since we need to test if these are available (but don't want to try/catch exceptions,
// for performance reasons)
public static class SteamworksExts
{
	delegate IntPtr GetPtrFn();

	static Assembly Steamworks;
	static GetPtrFn CSteamAPIContext_GetSteamClient;
	static GetPtrFn CSteamGameServerAPIContext_GetSteamClient;

	static SteamworksExts() {
		Steamworks = Assembly.GetAssembly(typeof(SteamClient))!;
		Type CSteamAPIContextT;
		Type CSteamGameServerAPIContextT;
		try {
			CSteamAPIContextT = Steamworks.GetTypes().First(x => x.Name.Equals("CSteamAPIContext", StringComparison.InvariantCultureIgnoreCase))!;
			CSteamGameServerAPIContextT = Steamworks.GetTypes().First(x => x.Name.Equals("CSteamGameServerAPIContext", StringComparison.InvariantCultureIgnoreCase))!;
		}
		// ^^ This won't work because of some stupid thing in the steamworks library
		catch (ReflectionTypeLoadException typeloadex) {
			CSteamAPIContextT = typeloadex.Types.First(x => x.Name.Equals("CSteamAPIContext", StringComparison.InvariantCultureIgnoreCase))!;
			CSteamGameServerAPIContextT = typeloadex.Types.First(x => x.Name.Equals("CSteamGameServerAPIContext", StringComparison.InvariantCultureIgnoreCase))!;
		}

		MethodInfo CSteamAPIContextT_GetSteamClient = CSteamAPIContextT.GetMethod("GetSteamClient", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic)!;
		MethodInfo CSteamGameServerAPIContextT_GetSteamClient = CSteamGameServerAPIContextT.GetMethod("GetSteamClient", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic)!;

		CSteamAPIContext_GetSteamClient = CSteamAPIContextT_GetSteamClient.CreateDelegate<GetPtrFn>();
		CSteamGameServerAPIContext_GetSteamClient = CSteamGameServerAPIContextT_GetSteamClient.CreateDelegate<GetPtrFn>();
	}

	public static bool IsSteamClientNotNull() => CSteamAPIContext_GetSteamClient() != IntPtr.Zero;
	public static bool IsSteamServerNotNull() => CSteamGameServerAPIContext_GetSteamClient() != IntPtr.Zero;
}
