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

public class GModRichPresence : AutoGameSystemPerFrame
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
}
#endif
