using Game.Server;
using Game.Shared;

using Source.Common;

namespace Game.Server.NextBot;

public class LuaNextBot : NextBotCombatCharacter
{
	public static readonly SendTable DT_LuaNextBot = new(DT_NextBot, [
		SendPropDataTable("ScriptedEntity", DT_ScriptedEntity)
	]);
	public static readonly new ServerClass ServerClass = new ServerClass("LuaNextBot", DT_LuaNextBot).WithManualClassID(StaticClassIndices.CLuaNextBot);
}
