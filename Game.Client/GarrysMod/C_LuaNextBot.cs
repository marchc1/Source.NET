using Game.Shared;

using Source.Common;

namespace Game.Client.NextBot;

public class C_LuaNextBot : C_NextBotCombatCharacter
{
	public static readonly RecvTable DT_LuaNextBot = new(DT_NextBot, [
		RecvPropDataTable("ScriptedEntity", DT_ScriptedEntity)
	]);
	public static readonly new ClientClass ClientClass = new ClientClass("LuaNextBot", DT_LuaNextBot).WithManualClassID(StaticClassIndices.CLuaNextBot);
}
