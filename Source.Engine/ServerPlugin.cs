using Source.Common;
using Source.Common.Commands;
using Source.Common.Engine;
using Source.Common.Formats.Keyvalues;

using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;

namespace Source.Engine;

// todo: review this
public class Plugin
{
	public ReadOnlySpan<char> GetName() => ((Span<char>)Name).SliceNullTerminatedString();
	public bool Load(ReadOnlySpan<char> filename) => true;
	public void Unload() { }
	public void Disable(bool state) => Disabled = state;
	public bool IsDisabled() => Disabled;
	public int GetPluginInterfaceVersion() => PluginInterfaceVersion;
	public IServerPluginCallbacks GetCallback() => PluginCallbacks!;

	private void SetName(ReadOnlySpan<char> name) {
		strcpy(Name, name);
	}
	private InlineArray128<char> Name;
	private bool Disabled;
	private IServerPluginCallbacks? PluginCallbacks;
	private int PluginInterfaceVersion;
	private Assembly? PluginAssembly;
}

public class ServerPlugin : IServerPluginHelpers
{
	private readonly List<Plugin> Plugins = [];
	void LoadPlugins() {
		throw new NotImplementedException();
	}

	void UnloadPlugins() {
		throw new NotImplementedException();
	}

	bool UnloadPlugin(int index) {
		throw new NotImplementedException();
	}

	bool LoadPlugin(ReadOnlySpan<char> fileName) {
		throw new NotImplementedException();
	}

	void DisablePlugins() {
		throw new NotImplementedException();
	}

	void EnablePlugins() {
		throw new NotImplementedException();
	}

	void DisablePlugin(int index) {
		throw new NotImplementedException();
	}

	void EnablePlugin(int index) {
		throw new NotImplementedException();
	}

	void PrintDetails() {
		throw new NotImplementedException();
	}

	public void LevelInit(ReadOnlySpan<char> mapName, ReadOnlyMemory<byte> mapEntities, ReadOnlySpan<char> oldLevel, ReadOnlySpan<char> landmarkName, bool loadGame, bool background) {

		serverGameDLL.LevelInit(mapName, mapEntities, oldLevel, landmarkName, loadGame, background);

	}

	public void ServerActivate(Edict[] edictList, int edictCount, int clientMax) {
		serverGameDLL.ServerActivate(edictList, edictCount, clientMax);
	}

	public void GameFrame(bool simulating) {
		serverGameDLL.GameFrame(simulating);
	}

	public void LevelShutdown() {
		serverGameDLL.LevelShutdown();
	}

	public void ClientActive(Edict entity, bool loadGame) {
		SV.ServerGameClients!.ClientActive(entity, loadGame);
	}

	void ClientDisconnect(Edict entity) {
		SV.ServerGameClients!.ClientDisconnect(entity);
	}

	public void ClientPutInServer(Edict entity, ReadOnlySpan<char> playername) {
		SV.ServerGameClients!.ClientPutInServer(entity, playername);
	}

	public void SetCommandClient(int index) {
		SV.ServerGameClients!.SetCommandClient(index);
	}

	public void ClientSettingsChanged(Edict edict) {
		SV.ServerGameClients!.ClientSettingsChanged(edict);
	}

	bool ClientConnect(Edict entity, ReadOnlySpan<char> pszName, ReadOnlySpan<char> pszAddress, ReadOnlySpan<char> reject, int maxrejectlen) {
		throw new NotImplementedException();
	}

	public void ClientCommand(Edict entity, TokenizedCommand args) {
		SV.ServerGameClients!.ClientCommand(entity, args);
	}

	public QueryCvarCookie_t StartQueryCvarValue(Edict entity, ReadOnlySpan<char> cvar) {
		throw new NotImplementedException();
	}

	void NetworkIDValidated(ReadOnlySpan<char> userName, ReadOnlySpan<char> networkID) {
		throw new NotImplementedException();
	}

	void OnQueryCvarValueFinished(QueryCvarCookie_t cookie, Edict playerEntity, QueryCvarValueStatus status, ReadOnlySpan<char> cvar, ReadOnlySpan<char> cvarValue) {
		throw new NotImplementedException();
	}

	public void OnEdictAllocated(Edict? edict) {
		foreach (var p in Plugins)
			if (!p.IsDisabled())
				if (p.GetPluginInterfaceVersion() >= 3)
					p.GetCallback().OnEdictAllocated(edict);
	}

	public void OnEdictFreed(Edict edict) {
		foreach (var p in Plugins)
			if (!p.IsDisabled())
				if (p.GetPluginInterfaceVersion() >= 3)
					p.GetCallback().OnEdictFreed(edict);
	}

	public void CreateMessage(Edict entity, DialogType type, KeyValues data, IServerPluginCallbacks plugin) {
		throw new NotImplementedException();
	}

	public void ClientCommand(Edict entity, ReadOnlySpan<char> cmd) {
		throw new NotImplementedException();
	}
}
