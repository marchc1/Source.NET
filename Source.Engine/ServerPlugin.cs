using Source.Common;
using Source.Common.Commands;
using Source.Common.Engine;
using Source.Common.Formats.Keyvalues;

using System;
using System.Collections.Generic;
using System.Text;

namespace Source.Engine;

public class ServerPlugin : IServerPluginHelpers
{
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

	void LevelInit(ReadOnlySpan<char> mapName, ReadOnlySpan<char> maxEntities, ReadOnlySpan<char> oldLevel, ReadOnlySpan<char> landmarkName, bool loadGame, bool background) {
		throw new NotImplementedException();
	}

	void ServerActivate(Edict[] edictList, int edictCount, int clientMax) {
		serverGameDLL.ServerActivate(edictList, edictCount, clientMax);
	}

	public void GameFrame(bool simulating) {
		serverGameDLL.GameFrame(simulating);
	}

	void LevelShutdown() {
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

	public void OnEdictAllocated(Edict edict) {
		// throw new NotImplementedException();
	}

	void OnEdictFreed(Edict edict) {
		throw new NotImplementedException();
	}

	public void CreateMessage(Edict entity, DialogType type, KeyValues data, IServerPluginCallbacks plugin) {
		throw new NotImplementedException();
	}

	public void ClientCommand(Edict entity, ReadOnlySpan<char> cmd) {
		throw new NotImplementedException();
	}
}
