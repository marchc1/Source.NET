using Source.Common;
using Source;
using Game.Shared;
using System.Numerics;
namespace Game.Client;
using FIELD = FIELD<C_Flare>;
[NetworkName("CFlare")]
public class C_Flare : C_BaseCombatCharacter
{
	public static readonly RecvTable DT_Flare = new(DT_BaseCombatCharacter, [
		RecvPropFloat(FIELD.OF(nameof(TimeBurnOut))),
		RecvPropFloat(FIELD.OF(nameof(Scale))),
		RecvPropBool(FIELD.OF(nameof(Light))),
		RecvPropBool(FIELD.OF(nameof(Smoke))),
		RecvPropBool(FIELD.OF(nameof(PropFlare))),
	]);
	public static readonly new ClientClass ClientClass = new ClientClass(DT_Flare);

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
