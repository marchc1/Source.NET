using CommunityToolkit.HighPerformance;

using Game.Shared;

using Source.Common.Engine;
using Source.Engine;

using System;
using System.Collections.Generic;
using System.Text;

namespace Game.Shared;

public delegate void CreateGameRulesFn();

public class GameRulesRegister
{
	string ClassName;
	CreateGameRulesFn Fn;
	GameRulesRegister? Next;

	public static GameRulesRegister? Head = null!;

	public GameRulesRegister(ReadOnlySpan<char> classname, CreateGameRulesFn fn) {
		ClassName = new(classname);
		Fn = fn;

		Next = Head;
		Head = this;
	}

	public void CreateGameRules() => Fn();
	public static GameRulesRegister? FindByName(ReadOnlySpan<char> name) {
		for (GameRulesRegister? cur = Head; cur != null; cur = cur.Next)
			if (stricmp(name, cur.ClassName) == 0)
				return cur;
		return null;
	}
	public const string GAMERULES_STRINGTABLE_NAME = "GameRulesCreation";

#if CLIENT_DLL
	static INetworkStringTable? g_StringTableGameRules = null;

	void OnGameRulesCreationStringChanged(object? context, INetworkStringTable stringTable, int stringNumber, ReadOnlySpan<char> newString, ReadOnlySpan<byte> newData) {
		// The server has created a new CGameRules object.
		g_pGameRules = null!;

		ReadOnlySpan<char> className = newData.Cast<byte, char>();
		GameRulesRegister? reg = FindByName(className);
		if (reg == null)
			Error($"OnGameRulesCreationStringChanged: missing gamerules class '{className}' on the client");

		// Create the new game rules object.
		reg.CreateGameRules();

		if (g_pGameRules == null)
			Error($"OnGameRulesCreationStringChanged: game rules entity ({className}) not created");
	}

	// On the client, we respond to string table changes on the server.
	void InstallStringTableCallback_GameRules() {
		if (g_StringTableGameRules == null) {
			g_StringTableGameRules = networkstringtable.FindTable(GAMERULES_STRINGTABLE_NAME);
			g_StringTableGameRules?.SetStringChangedCallback(null, OnGameRulesCreationStringChanged);
		}
	}
#elif GAME_DLL
	static INetworkStringTable? g_StringTableGameRules = null;

	void CreateNetworkStringTables_GameRules() {
		// Create the string tables
		g_StringTableGameRules = networkstringtable.CreateStringTable(GAMERULES_STRINGTABLE_NAME, 1);
	}

	public static void CreateGameRulesObject(ReadOnlySpan<char> className) {
		// Delete the old game rules object.
		g_pGameRules = null!;

		// Create a new game rules object.
		GameRulesRegister? reg = FindByName(className);
		if (reg == null)
			Error($"InitGameRules: missing gamerules class '{className}' on the server");

		reg.CreateGameRules();
		if (g_pGameRules == null)
			Error($"InitGameRules: game rules entity ({className}) not created");

		// Make sure the client gets notification to make a new game rules object.
		Assert(g_StringTableGameRules != null);
		g_StringTableGameRules.AddString(true, "classname", ((int)strlen(className) + 1) * sizeof(char), className.Cast<char, byte>());

		g_pGameRules?.CreateCustomNetworkStringTables();
	}
#endif
}
