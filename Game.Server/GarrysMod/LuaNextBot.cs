using Game.Server;
using Game.Shared;

using Source.Common;

using FIELD = Source.FIELD<Game.Server.NextBot.LuaNextBot>;
namespace Game.Server.NextBot;

public class LuaNextBot : NextBotCombatCharacter
{
	public static readonly SendTable DT_LuaNextBot = new(DT_NextBot, [
		SendPropDataTable("ScriptedEntity", DT_ScriptedEntity),
		SendPropInt(FIELD.OF(nameof(LifeState)), 3, PropFlags.Unsigned)
	]);
	public static readonly new ServerClass ServerClass = new ServerClass("LuaNextBot", DT_LuaNextBot).WithManualClassID(StaticClassIndices.CLuaNextBot);
}
