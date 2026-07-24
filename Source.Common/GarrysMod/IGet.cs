using Source.Common.Filesystem;
using Source.Common.MaterialSystem;
using Source.Common.Steam;

using Steamworks;

namespace Source.Common.GarrysMod;

public interface IMotionSensor; // todo

public interface IGet
{
	void OnLoadFailed( ReadOnlySpan<char> reason );
	ReadOnlySpan<char> GameDir();
	bool IsDedicatedServer();
	int GetClientCount();
	IFileSystem FileSystem();
	Lua.ILuaShared LuaShared();
	Lua.ILuaConVars LuaConVars();
	IMenuSystem MenuSystem();
	IResources Resources();
	IIntroScreen IntroScreen();
	IMaterialSystem Materials();
	IGMHTML HTML();
	IServerAddons ServerAddons();
	ISteamHTTP SteamHTTP();
	ISteamRemoteStorage SteamRemoteStorage();
	ISteamUtils SteamUtils();
	ISteamApps SteamApps();
	ISteamScreenshots SteamScreenshots();
	ISteamUser SteamUser();
	ISteamFriends SteamFriends();
	ISteamUGC SteamUGC();
	ISteamGameServer SteamGameServer();
	ISteamNetworking SteamNetworking();
	void Initialize(IFileSystem fileSystem);
	void ShutDown();
	void RunSteamCallbacks();
	void ResetSteamAPIs();
	void SetMotionSensor(IMotionSensor? unk1);
	IMotionSensor? MotionSensor();
	int Version();
	ReadOnlySpan<char> VersionStr();
	IGMod_Audio Audio();
	ReadOnlySpan<char> VersionTimeStr();
	// IAnalytics Analytics();
	void UpdateRichPresense( ReadOnlySpan<char> status );
	void ResetRichPresense();
	void FilterText(ReadOnlySpan<char> unk1, Span<char> unk2, /*There was an int here: I am guessing it is unk2's size.*/ ETextFilteringContext unk3, CSteamID unk4);
}
