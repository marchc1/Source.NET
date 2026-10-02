using Game.Server;
using Game.Shared;

using Source.Common;

using FIELD = Source.FIELD<Game.Server.NextBot.LuaNextBot>;
namespace Game.Server.NextBot;

[LinkEntityToClass("sent_nextbot")]
[NetworkName("CLuaNextBot")]
public class LuaNextBot : NextBotCombatCharacter
{
	public static readonly SendTable DT_LuaNextBot = new(DT_NextBot, [
		SendPropDataTable("m_ScriptedEntity", DT_ScriptedEntity),
		SendPropInt(FIELD.OF(nameof(LifeState)), 3, PropFlags.Unsigned)
	]);
	public static readonly new ServerClass ServerClass = new ServerClass(DT_LuaNextBot);
}
