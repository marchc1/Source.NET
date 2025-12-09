using Source.Common;
using Source;
using Game.Shared;
using System.Numerics;
namespace Game.Client;
using FIELD = FIELD<C_TESparks>;
public class C_TESparks : C_TEParticleSystem
{
	public static readonly RecvTable DT_TESparks = new(DT_TEParticleSystem, [
		RecvPropInt(FIELD.OF(nameof(Magnitude))),
		RecvPropInt(FIELD.OF(nameof(TrailLength))),
		RecvPropVector(FIELD.OF(nameof(Dir))),
	]);
	public static readonly new ClientClass ClientClass = new ClientClass("TESparks", DT_TESparks).WithManualClassID(StaticClassIndices.CTESparks);

	public int Magnitude;
	public int TrailLength;
	public Vector3 Dir;
}
