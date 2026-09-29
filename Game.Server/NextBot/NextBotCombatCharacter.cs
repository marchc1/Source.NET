using Game.Server;
using Game.Shared;

using Source.Common;

namespace Game.Server.NextBot;

[NetworkName("NextBotCombatCharacter")]
public class NextBotCombatCharacter : BaseCombatCharacter
{
	public static readonly SendTable DT_NextBot = new(DT_BaseCombatCharacter, []);
	public static readonly new ServerClass ServerClass = new ServerClass(DT_NextBot);
}
