using Game.Shared;

using Source.Common;

namespace Game.Client.NextBot;

public class C_NextBotCombatCharacter : C_BaseCombatCharacter
{
	public static readonly RecvTable DT_NextBot = new(DT_BaseCombatCharacter, []);
	public static readonly new ClientClass ClientClass = new ClientClass("NextBotCombatCharacter", DT_NextBot).WithManualClassID(StaticClassIndices.NextBotCombatCharacter);
}
