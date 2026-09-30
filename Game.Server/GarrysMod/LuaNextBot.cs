using Game.Server;
using Game.Shared;

using Source.Common;

using FIELD = Source.FIELD<Game.Server.NextBot.LuaNextBot>;
namespace Game.Server.NextBot;

[NetworkName("CLuaNextBot")]
public class LuaNextBot : NextBotCombatCharacter
{
	public static readonly SendTable DT_LuaNextBot = new(DT_NextBot, [
		SendPropDataTable("m_ScriptedEntity", DT_ScriptedEntity),
		SendPropInt(BaseEntity.NetworkVarFields.LifeState, 3, PropFlags.Unsigned)
	]);
	public static readonly new ServerClass ServerClass = new ServerClass(DT_LuaNextBot);
}
