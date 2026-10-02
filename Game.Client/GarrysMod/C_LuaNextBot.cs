using Game.Shared;

using Source;
using Source.Common;

using FIELD = Source.FIELD<Game.Client.NextBot.C_LuaNextBot>;

namespace Game.Client.NextBot;

[LinkEntityToClass("sent_nextbot")]
[NetworkName("CLuaNextBot")]
public class C_LuaNextBot : C_NextBotCombatCharacter
{
	public static readonly RecvTable DT_LuaNextBot = new(DT_NextBot, [
		RecvPropDataTable("m_ScriptedEntity", DT_ScriptedEntity),
		RecvPropInt(FIELD.OF(nameof(LifeState)))
	]);
	public static readonly new ClientClass ClientClass = new ClientClass(DT_LuaNextBot);
}
