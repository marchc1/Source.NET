using Source.Common.Filesystem;
using Source.Common.GarrysMod;
using Source.Common.Lua;
using Source.Common.MaterialSystem;
using Source.Common.Steam;

using Steamworks;

namespace Source.Common.GarrysMod;

/*
	RaphaelIT7: GMod has a custom class called CModuleLoader that it stores inside CGet?

	IDA:
	CModuleLoader<ILuaShared>::CModuleLoader(IFileSystem *,char const*,char const*,bool)
	CModuleLoader<ILuaConVars>::CModuleLoader(IFileSystem *,char const*,char const*,bool)
	CModuleLoader<IMenuSystem>::CModuleLoader(IFileSystem *,char const*,char const*,bool)
	CModuleLoader<IIntroScreen>::CModuleLoader(IFileSystem *,char const*,char const*,bool)
	CModuleLoader<IMaterialSystem>::CModuleLoader(IFileSystem *,char const*,char const*,bool)
	CModuleLoader<IResources>::CModuleLoader(IFileSystem *,char const*,char const*,bool)
	CModuleLoader<IGMod_Audio>::CModuleLoader(IFileSystem *,char const*,char const*,bool)
	CModuleLoader<IServerAddons>::CModuleLoader(IFileSystem *,char const*,char const*,bool)
	CModuleLoader<IGMHTML>::CModuleLoader(IFileSystem *,char const*,char const*,bool)
*/

public sealed class CGet : IGet
{
	private IFileSystem? fileSystem = null;
	// All modules & steam interfaces are defined here now
	private IMotionSensor? motionSensor = null; // defined last in class (has the highest offset in IDA)

	public void Initialize(IFileSystem fileSystem) {
		this.fileSystem = fileSystem;
	}

	public void ShutDown() {
	}

	public void OnLoadFailed(ReadOnlySpan<char> reason) {
		// Some SteamClient stuff
		Error("Startup Failure!");
	}

	public ReadOnlySpan<char> GameDir() => string.Empty;

	public bool IsDedicatedServer() => false;

	public int GetClientCount() => 0; // Returns sv.GetNumClients();

	public IFileSystem? FileSystem() => fileSystem;

	public ILuaShared? LuaShared() => null;

	public Lua.ILuaConVars? LuaConVars() => null;

	public IMenuSystem? MenuSystem() => null;

	public IResources? Resources() => null;

	public IIntroScreen? IntroScreen() => null;

	public IMaterialSystem? Materials() => null;

	public IGMHTML? HTML() => null;

	public IServerAddons? ServerAddons() => null;

	public ISteamHTTP? SteamHTTP() => null;

	public ISteamRemoteStorage? SteamRemoteStorage() => null;

	public ISteamUtils? SteamUtils() => null;

	public ISteamApps? SteamApps() => null;

	public ISteamScreenshots? SteamScreenshots() => null;

	public ISteamUser? SteamUser() => null;

	public ISteamFriends? SteamFriends() => null;

	public ISteamUGC? SteamUGC() => null;

	public ISteamGameServer? SteamGameServer() => null;

	public ISteamNetworking? SteamNetworking() => null;

	public void RunSteamCallbacks() {
	}

	public void ResetSteamAPIs() {
	}

	public void SetMotionSensor(IMotionSensor? motionSensor) {
		this.motionSensor = motionSensor;
	}

	public IMotionSensor? MotionSensor() => motionSensor;

	public int Version() => 1;

	public ReadOnlySpan<char> VersionStr() => "01012026"; // Load garrysmod.ver and cache versionstr line!

	public IGMod_Audio? Audio() => null;

	public ReadOnlySpan<char> VersionTimeStr() => string.Empty;

	public void UpdateRichPresense(ReadOnlySpan<char> status) {
	}

	public void ResetRichPresense() {
	}

	public void FilterText(
		ReadOnlySpan<char> unk1,
		Span<char> unk2,
		ETextFilteringContext unk3,
		CSteamID unk4) {
		unk1.CopyTo(unk2);
	}
}
