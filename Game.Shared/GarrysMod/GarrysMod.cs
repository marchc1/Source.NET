#if CLIENT_DLL || GAME_DLL
global using static Game.Client.GarrysMod.GarrysModSingletons;

using Game.Shared;

using Microsoft.Extensions.DependencyInjection;

using Source;
using Source.Common;
using Source.Common.GarrysMod;
using Source.Common.MaterialSystem;
using Source.Common.Networking;

using Steamworks;

using System;
using System.Collections.Generic;
using System.Text;

namespace Game.Client.GarrysMod;

public static class GarrysModSingletons
{
	public static readonly GarrysMod garrysmod = new();
}

public class GarrysMod : IGarrysMod
{
	public void DLLInit(IServiceCollection services) {
#if CLIENT_DLL
		services.AddSingleton<IIntroScreen, IntroScreen>();
		services.AddSingleton<IServerAddons, ServerAddons>();
#else

#endif
	}

	public void EndVideoScale(int unk1, int unk2) {
		throw new NotImplementedException();
	}

	public void FireGameEvent(IGameEvent ev) {
		throw new NotImplementedException();
	}

	public ReadOnlySpan<char> GetMapName() {
		throw new NotImplementedException();
	}

	public void InitializeMod(IServiceProvider services) {
		get.IntroScreen()!.Update("Adding Custom Fonts", true);
		// todo: AddCustomFonts
		get.IntroScreen()!.Update("Adding Language Files", true);
		// todo: AddLanguageFiles
		get.IntroScreen()!.Update("Setup Menu System", true);
		// todo: menu system init
		get.IntroScreen()!.Update("Setting Convar Defaults", true);
		// todo: convar defaults
#if CLIENT_DLL
		string absPath = $"{engine.GetGameDirectory()}/cache";
#else // TODO: This is really stupid. Why is server different here in the interface. This deserves deviation.
		Span<char> path = stackalloc char[MAX_PATH];
		engine.GetGameDir(path);
		string absPath = $"{path.SliceNullTerminatedString()}/cache";
#endif
		Directory.CreateDirectory(absPath);
		Directory.CreateDirectory(Path.Combine(absPath, "lua"));
		Directory.CreateDirectory(Path.Combine(absPath, "workshop"));
		filesystem.AddSearchPath(absPath, "CACHE");
	}

#if CLIENT_DLL
	public void LevelInit(ReadOnlySpan<char> mapName) {
		get.Audio()?.StopAllPlayback();

		engine.ClientCmd("net_maxroutable 1260");
		// todo: AddCustomFonts();

		if (gpGlobals.MaxClients > 1) {
			// todo: ParseParticleEffects(true, false);
		}

		string status = (gpGlobals.MaxClients < 2 ? "Singleplayer - " : "Multiplayer - ") + mapName.ToString();

		string? gamemodeName = null; // todo: the client Lua gamemode's name (g_pGamemode)
		if (gamemodeName != null) {
			status += " (";
			IGamemodeSystem.Information info = filesystem.Gamemodes().FindByName(gamemodeName);
			string title;
			if (info.Exists)
				title = info.Title;
			else {
				string name = gamemodeName.Replace("_modded", "");
				info = filesystem.Gamemodes().FindByName(name);
				title = info.Exists ? info.Title : name;
			}
			status += title;
			status += ")";
		}

		get.UpdateRichPresense(status);
	}
#else
	public void LevelInit(ReadOnlySpan<char> mapName, ReadOnlyMemory<byte> mapEntities, ReadOnlySpan<char> oldLevel, ReadOnlySpan<char> landmarkName, bool loadGame, bool background) {
		if (gpGlobals.MaxClients == 1 || !get.IsDedicatedServer())
			engine.ServerCommand("lua_error_url ''\n");

		Lua.Create();
		Lua.OnLoaded();
	}

	static class Lua
	{
		public static bool Kill() {
			// if (g_LuaManager != null) {
			// 	get.LuaShared()!.UnMountLua("lsv");
			// 	LuaManager.Shutdown();
			// 	g_LuaManager = null;
			// }
			// gGM = null;
			// GarrysMod.Lua.Libraries.Timer.Shutdown();
			return true;
		}

		public static void Create() {
			Kill();

			// foreach (ILegacyAddons.Information addon in filesystem.LegacyAddons().GetList()) {
			// 	if (!string.IsNullOrEmpty(addon.LuaPath))
			// 		get.LuaShared()!.MountLuaAdd(addon.LuaPath, "lsv");
			// 	if (!string.IsNullOrEmpty(addon.Placeholder4))
			// 		get.LuaShared()!.MountLuaAdd(addon.Placeholder4, "lsv");
			// }
			// get.LuaShared()!.MountLuaAdd("workshop/lua", "lsv");
			// get.LuaShared()!.MountLuaAdd("workshop/gamemodes", "lsv");
			// get.LuaShared()!.MountLua("lsv");

			// if (g_LuaManager != null)
			// 	Error("New gLUA when old one exists!\n");
			// g_LuaManager = new LuaManager();
			// if (gGM != null)
			// 	Error("New gGM when old one exists!\n");
			// gGM = new CLuaGamemode();
			// todo: init g_LuaManager, then init gGM
			// GModDataPack.BuildSearchPaths();
		}

		public static void OnLoaded() {
			// GarrysMod.Ammo.Refresh();
		}
	}

	static class LuaManager
	{
		public static void Shutdown() {
			// g_LuaID++;
			// ShutdownLuaClasses(g_Lua);
			// get.LuaShared()!.CloseLuaInterface(g_Lua);
			// g_Lua = null;
			// if (g_LuaNetworkedVars == null)
			// 	Error("!g_LuaNetworkedVars");
			// todo: free every entry of g_LuaNetworkedVars
			// g_LuaNetworkedVars = null;
		}
	}
#endif

	public void MD5String(Span<byte> outMD5, ReadOnlySpan<byte> unk1, ReadOnlySpan<byte> unk2, ReadOnlySpan<byte> unk3) {
		throw new NotImplementedException();
	}

	public void PlaySound(ReadOnlySpan<char> sound) {
		throw new NotImplementedException();
	}

	public void RunConsoleCommand(ReadOnlySpan<char> cmd) {
		throw new NotImplementedException();
	}

	public void StartVideoScale(int unk1, int unk2) {
		throw new NotImplementedException();
	}

	public void Think() {

	}
}

public class GModRichPresence : AutoGameSystemPerFrame // callum TODO, remove this once the actual gmod impl is complete
{
	private static readonly GModRichPresence _ = new();

	string LastStatus = "";
	TimeUnit_t LastRun;

	public override ReadOnlySpan<char> Name() => "GModRichPresence";

	public override bool Init() {
		LastStatus = "";
		return true;
	}

	public override void Shutdown() {
		if (SteamAPI.IsSteamRunning())
			SteamFriends.ClearRichPresence();
	}

#if CLIENT_DLL
	public override void Update(TimeUnit_t frametime) {
		if (gpGlobals.RealTime < LastRun + 2.0f) return;
		LastRun = gpGlobals.RealTime;

		if (!SteamAPI.IsSteamRunning())
			return;

		if (!engine.IsConnected()) {
			SetStatus("In Menus");
			return;
		}

		if (!engine.IsInGame()) {
			SetStatus("Joining a server");
			return;
		}

		bool multiplayer = engine.GetMaxClients() > 1;
		string? connect = multiplayer && engine.GetNetChannelInfo() is INetChannel netchan && netchan.GetRemoteAddress() is NetAddress address ? $"+connect {address.ToString(false)}" : null;

		SetStatus($"{(multiplayer ? "Multiplayer" : "Singleplayer")} - {GetMapName()} ({GetGamemodeName()})", connect);
	}
#endif

	void SetStatus(string status, string? connect = null) {
		if (status == LastStatus)
			return;
		LastStatus = status;

		SteamFriends.SetRichPresence("status", status);
		SteamFriends.SetRichPresence("Generic", status);
		SteamFriends.SetRichPresence("steam_display", "#Status_Generic");
		SteamFriends.SetRichPresence("connect", connect);
	}

	static ReadOnlySpan<char> GetGamemodeName() {
		return "GAMEMODE"; // TODO
	}

#if CLIENT_DLL
	static ReadOnlySpan<char> GetMapName() {
		ReadOnlySpan<char> level = engine.GetLevelName().SliceNullTerminatedString();
		if (level.IsEmpty)
			return level;

		int slash = level.LastIndexOfAny('/', '\\');
		if (slash >= 0) level = level[(slash + 1)..];

		int dot = level.LastIndexOf('.');
		if (dot >= 0) level = level[..dot];

		return level;
	}
#endif
}
#endif
