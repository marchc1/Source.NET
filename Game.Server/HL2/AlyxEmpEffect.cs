using Game.Shared;

using Source.Common;

namespace Game.Server.HL2;
using FIELD = Source.FIELD<AlyxEmpEffect>;
public partial class AlyxEmpEffect : BaseCombatCharacter
{
	public static readonly SendTable DT_AlyxEmpEffect = new(DT_BaseCombatCharacter, [
		SendPropInt(FIELD.OF(nameof(State)), 8, PropFlags.Unsigned),
		SendPropFloat(FIELD.OF(nameof(Duration)), 0, PropFlags.NoScale),
		SendPropFloat(FIELD.OF(nameof(StartTime)), 0, PropFlags.NoScale),
	]);
	public static readonly new ServerClass ServerClass = new ServerClass("AlyxEmpEffect", DT_AlyxEmpEffect).WithManualClassID(StaticClassIndices.CAlyxEmpEffect);

	public int State;
	public TimeUnit_t Duration;
	public TimeUnit_t StartTime;
}
