using Source.Common;
using Source;
using Game.Shared;
using System.Numerics;
namespace Game.Server;
using FIELD = FIELD<Flare>;
public class Flare : BaseCombatCharacter
{
	public static readonly SendTable DT_Flare = new(DT_BaseCombatCharacter, [
		SendPropFloat(FIELD.OF(nameof(TimeBurnOut)), 0, PropFlags.NoScale),
		SendPropFloat(FIELD.OF(nameof(Scale)), 0, PropFlags.NoScale),
		SendPropBool(FIELD.OF(nameof(Light))),
		SendPropBool(FIELD.OF(nameof(Smoke))),
		SendPropBool(FIELD.OF(nameof(PropFlare))),
	]);
	public static readonly new ServerClass ServerClass = new ServerClass("Flare", DT_Flare).WithManualClassID(StaticClassIndices.CFlare);

	public float TimeBurnOut;
	public float Scale;
	public bool Light;
	public bool Smoke;
	public bool PropFlare;
}
