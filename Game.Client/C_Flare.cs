using Source.Common;
using Source;
using Game.Shared;
using System.Numerics;
namespace Game.Client;
using FIELD = FIELD<C_Flare>;
public class C_Flare : C_BaseCombatCharacter
{
	public static readonly RecvTable DT_Flare = new(DT_BaseCombatCharacter, [
		RecvPropFloat(FIELD.OF(nameof(TimeBurnOut))),
		RecvPropFloat(FIELD.OF(nameof(Scale))),
		RecvPropBool(FIELD.OF(nameof(Light))),
		RecvPropBool(FIELD.OF(nameof(Smoke))),
		RecvPropBool(FIELD.OF(nameof(PropFlare))),
	]);
	public static readonly new ClientClass ClientClass = new ClientClass("Flare", DT_Flare).WithManualClassID(StaticClassIndices.CFlare);

	public float TimeBurnOut;
	public float Scale;
	public bool Light;
	public bool Smoke;
	public bool PropFlare;
}
