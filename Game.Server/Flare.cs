using Source.Common;
using Source;
using Game.Shared;
using System.Numerics;
namespace Game.Server;
using FIELD = FIELD<Flare>;
[LinkEntityToClass("env_flare")]
[NetworkName("CFlare")]
public class Flare : BaseCombatCharacter
{
	public static readonly SendTable DT_Flare = new(DT_BaseCombatCharacter, [
		SendPropFloat(FIELD.OF(nameof(TimeBurnOut)), 0, PropFlags.NoScale),
		SendPropFloat(FIELD.OF(nameof(Scale)), 0, PropFlags.NoScale),
		SendPropBool(FIELD.OF(nameof(Light))),
		SendPropBool(FIELD.OF(nameof(Smoke))),
		SendPropBool(FIELD.OF(nameof(PropFlare))),
	]);
	public static readonly new ServerClass ServerClass = new ServerClass(DT_Flare);

	[NetworkName("m_flTimeBurnOut")]
	public float TimeBurnOut;
	[NetworkName("m_flScale")]
	public float Scale;
	[NetworkName("m_bLight")]
	public bool Light;
	[NetworkName("m_bSmoke")]
	public bool Smoke;
	[NetworkName("m_bPropFlare")]
	public bool PropFlare;
}
