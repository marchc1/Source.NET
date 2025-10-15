using Game.Shared;

using Source.Common;

namespace Game.Client.HL2;
using FIELD = Source.FIELD<C_AlyxEmpEffect>;

public partial class C_AlyxEmpEffect : C_BaseCombatCharacter
{
	public static readonly RecvTable DT_AlyxEmpEffect = new(DT_BaseCombatCharacter, [
		RecvPropInt(FIELD.OF(nameof(State))),
		RecvPropFloat(FIELD.OF(nameof(Duration))),
		RecvPropFloat(FIELD.OF(nameof(StartTime))),
	]);
	public static readonly new ClientClass ClientClass = new ClientClass("AlyxEmpEffect", DT_AlyxEmpEffect).WithManualClassID(StaticClassIndices.CAlyxEmpEffect);

	public int State;
	public TimeUnit_t Duration;
	public TimeUnit_t StartTime;
}
