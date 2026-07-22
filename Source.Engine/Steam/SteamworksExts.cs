global using static Source.Engine.Steam.SteamworksDotNetBoasts100PercentCoverageOfTheNativeSteamworksAPIAcrossAllInterfaces;
global using static Source.Engine.Steam.SteamworksExts;

using Steamworks;

using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Source.Engine.Steam;

// I am very annoyed
class SteamworksDotNetBoasts100PercentCoverageOfTheNativeSteamworksAPIAcrossAllInterfaces
{
	[DllImport("steam_api64", CallingConvention = CallingConvention.Cdecl)] public static extern void SteamGameServer_RunCallbacks();
	[DllImport("steam_api64", CallingConvention = CallingConvention.Cdecl)] public static extern void SteamAPI_SetBreakpadAppID(uint unAppID);
}

// These are some hacks to access the internal pointers for steamclient
// since we need to test if these are available (but don't want to try/catch exceptions,
// for performance reasons)
class SteamworksExts
{
	delegate IntPtr GetPtrFn();

	static Assembly Steamworks;
	static Dictionary<string, Type> TypeLookup;
	static GetPtrFn CSteamAPIContext_GetSteamClient;
	static GetPtrFn CSteamAPIContext_GetSteamUser;
	static GetPtrFn CSteamGameServerAPIContext_GetSteamClient;

	static GetPtrFn RetrievePtrFn(string searchClass, string searchMethod) {
		Type typeSearch;
		typeSearch = TypeLookup[searchClass];
		MethodInfo methodThatReturnsPtr = typeSearch.GetMethod(searchMethod, BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic)!;
		return methodThatReturnsPtr.CreateDelegate<GetPtrFn>();
	}

	static SteamworksExts() {
		Steamworks = Assembly.GetAssembly(typeof(SteamClient))!;
		TypeLookup = [];
		try {
			TypeLookup = Steamworks.GetTypes().ToDictionary(x => x.Name);
		}
		// ^^ This won't work because of some stupid thing in the steamworks library
		catch (ReflectionTypeLoadException typeloadex) {
			foreach (var t in typeloadex.Types)
				if (t != null)
					TypeLookup[t.Name] = t;
		}
		// Load these pointers
		CSteamAPIContext_GetSteamClient = RetrievePtrFn("CSteamAPIContext", "GetSteamClient");
		CSteamAPIContext_GetSteamUser = RetrievePtrFn("CSteamAPIContext", "GetSteamUser");
		CSteamGameServerAPIContext_GetSteamClient = RetrievePtrFn("CSteamGameServerAPIContext", "GetSteamClient");
	}

	public static IntPtr GetSteamUser() => CSteamAPIContext_GetSteamUser();
	public static bool HasSteamClient() => CSteamAPIContext_GetSteamClient() != IntPtr.Zero;
	public static bool HasSteamGameServer() => CSteamGameServerAPIContext_GetSteamClient() != IntPtr.Zero;
}


// Interfaces to the static classes
// (yes, this is a weird way to do it)
// but this was easiest to do while still using Steamworks.NET and not reinventing the whole wheel

public interface ISteamClient;
public interface ISteamUser;
public interface ISteamFriends;
public interface ISteamUtils{
#if SWDS
	public EUniverse GetConnectedUniverse() => SteamGameServerUtils.GetConnectedUniverse();
	#else
	public EUniverse GetConnectedUniverse() => SteamUtils.GetConnectedUniverse();
	#endif
}
public interface ISteamMatchmaking;
public interface ISteamGameSearch;
public interface ISteamUserStats;
public interface ISteamApps;
public interface ISteamMatchmakingServers;
public interface ISteamNetworking;
public interface ISteamRemoteStorage;
public interface ISteamScreenshots;
public interface ISteamHTTP;
public interface ISteamController;
public interface ISteamUGC;
public interface ISteamAppList;
public interface ISteamMusic;
public interface ISteamMusicRemote;
public interface ISteamHTMLSurface;
public interface ISteamInventory;
public interface ISteamVideo;
public interface ISteamParentalSettings;
public interface ISteamInput;
public interface ISteamGameServer{
	public void SetProduct(ReadOnlySpan<char> pszProduct) => SteamGameServer.SetProduct(new(pszProduct));
	public void SetGameDescription(ReadOnlySpan<char> pszGameDescription) => SteamGameServer.SetGameDescription(new(pszGameDescription));
	public void SetModDir(ReadOnlySpan<char> pszModDir) => SteamGameServer.SetModDir(new(pszModDir));
	public void SetDedicatedServer(bool bDedicated) => SteamGameServer.SetDedicatedServer(bDedicated);
	public void LogOn(ReadOnlySpan<char> pszToken) => SteamGameServer.LogOn(new(pszToken));
	public void LogOnAnonymous() => SteamGameServer.LogOnAnonymous();
	public void LogOff() => SteamGameServer.LogOff();
	public bool BLoggedOn() => SteamGameServer.BLoggedOn();
	public bool BSecure() => SteamGameServer.BSecure();
	public CSteamID GetSteamID() => SteamGameServer.GetSteamID();
	public bool WasRestartRequested() => SteamGameServer.WasRestartRequested();
	public void SetMaxPlayerCount(int cPlayersMax) => SteamGameServer.SetMaxPlayerCount(cPlayersMax);
	public void SetBotPlayerCount(int cBotplayers) => SteamGameServer.SetBotPlayerCount(cBotplayers);
	public void SetServerName(ReadOnlySpan<char> pszServerName) => SteamGameServer.SetServerName(new(pszServerName));
	public void SetMapName(ReadOnlySpan<char> pszMapName) => SteamGameServer.SetMapName(new(pszMapName));
	public void SetPasswordProtected(bool bPasswordProtected) => SteamGameServer.SetPasswordProtected(bPasswordProtected);
	public void SetSpectatorPort(ushort unSpectatorPort) => SteamGameServer.SetSpectatorPort(unSpectatorPort);
	public void SetSpectatorServerName(ReadOnlySpan<char> pszSpectatorServerName) => SteamGameServer.SetSpectatorServerName(new(pszSpectatorServerName));
	public void ClearAllKeyValues() => SteamGameServer.ClearAllKeyValues();
	public void SetKeyValue(ReadOnlySpan<char> pKey, ReadOnlySpan<char> pValue) => SteamGameServer.SetKeyValue(new(pKey), new(pValue));
	public void SetGameTags(ReadOnlySpan<char> pchGameTags) => SteamGameServer.SetGameTags(new(pchGameTags));
	public void SetGameData(ReadOnlySpan<char> pchGameData) => SteamGameServer.SetGameData(new(pchGameData));
	public void SetRegion(ReadOnlySpan<char> pszRegion) => SteamGameServer.SetRegion(new(pszRegion));
	public void SetAdvertiseServerActive(bool bActive) => SteamGameServer.SetAdvertiseServerActive(bActive);
	public HAuthTicket GetAuthSessionTicket(byte[] pTicket, int cbMaxTicket, out uint pcbTicket) => SteamGameServer.GetAuthSessionTicket(pTicket, cbMaxTicket, out pcbTicket);
	public EBeginAuthSessionResult BeginAuthSession(byte[] pAuthTicket, int cbAuthTicket, CSteamID steamID) => SteamGameServer.BeginAuthSession(pAuthTicket, cbAuthTicket, steamID);
	public void EndAuthSession(CSteamID steamID) => SteamGameServer.EndAuthSession(steamID);
	public void CancelAuthTicket(HAuthTicket hAuthTicket) => SteamGameServer.CancelAuthTicket(hAuthTicket);
	public EUserHasLicenseForAppResult UserHasLicenseForApp(CSteamID steamID, AppId_t appID) => SteamGameServer.UserHasLicenseForApp(steamID, appID);
	public bool RequestUserGroupStatus(CSteamID steamIDUser, CSteamID steamIDGroup) => SteamGameServer.RequestUserGroupStatus(steamIDUser, steamIDGroup);
	public void GetGameplayStats() => SteamGameServer.GetGameplayStats();
	public SteamAPICall_t GetServerReputation() => SteamGameServer.GetServerReputation();
	public SteamIPAddress_t GetPublicIP() => SteamGameServer.GetPublicIP();
	public bool HandleIncomingPacket(byte[] pData, int cbData, uint srcIP, ushort srcPort) => SteamGameServer.HandleIncomingPacket(pData, cbData, srcIP, srcPort);
	public int GetNextOutgoingPacket(byte[] pOut, int cbMaxOut, out uint pNetAdr, out ushort pPort) => SteamGameServer.GetNextOutgoingPacket(pOut, cbMaxOut, out pNetAdr, out pPort);
	public SteamAPICall_t AssociateWithClan(CSteamID steamIDClan) => SteamGameServer.AssociateWithClan(steamIDClan);
	public SteamAPICall_t ComputeNewPlayerCompatibility(CSteamID steamIDNewPlayer) => SteamGameServer.ComputeNewPlayerCompatibility(steamIDNewPlayer);
	public bool SendUserConnectAndAuthenticate_DEPRECATED(uint unIPClient, byte[] pvAuthBlob, uint cubAuthBlobSize, out CSteamID pSteamIDUser) => SteamGameServer.SendUserConnectAndAuthenticate_DEPRECATED(unIPClient, pvAuthBlob, cubAuthBlobSize, out pSteamIDUser);
	public CSteamID CreateUnauthenticatedUserConnection() => SteamGameServer.CreateUnauthenticatedUserConnection();
	public void SendUserDisconnect_DEPRECATED(CSteamID steamIDUser) => SteamGameServer.SendUserDisconnect_DEPRECATED(steamIDUser);
	public bool BUpdateUserData(CSteamID steamIDUser, ReadOnlySpan<char> pchPlayerName, uint uScore) => SteamGameServer.BUpdateUserData(steamIDUser, new(pchPlayerName), uScore);
}
public interface ISteamGameServerStats;
public class ImplSteamClient : ISteamClient;
public class ImplSteamUser : ISteamUser;
public class ImplSteamFriends : ISteamFriends;
public class ImplSteamUtils : ISteamUtils;
public class ImplSteamMatchmaking : ISteamMatchmaking;
public class ImplSteamGameSearch : ISteamGameSearch;
public class ImplSteamUserStats : ISteamUserStats;
public class ImplSteamApps : ISteamApps;
public class ImplSteamMatchmakingServers : ISteamMatchmakingServers;
public class ImplSteamNetworking : ISteamNetworking;
public class ImplSteamRemoteStorage : ISteamRemoteStorage;
public class ImplSteamScreenshots : ISteamScreenshots;
public class ImplSteamHTTP : ISteamHTTP;
public class ImplSteamController : ISteamController;
public class ImplSteamUGC : ISteamUGC;
public class ImplSteamAppList : ISteamAppList;
public class ImplSteamMusic : ISteamMusic;
public class ImplSteamMusicRemote : ISteamMusicRemote;
public class ImplSteamHTMLSurface : ISteamHTMLSurface;
public class ImplSteamInventory : ISteamInventory;
public class ImplSteamVideo : ISteamVideo;
public class ImplSteamParentalSettings : ISteamParentalSettings;
public class ImplSteamInput : ISteamInput;
public class ImplSteamGameServer : ISteamGameServer;
public class ImplSteamGameServerStats : ISteamGameServerStats;
