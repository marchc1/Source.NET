using Source.Common;
using Source;
using Game.Shared;
using System.Numerics;
namespace Game.Client;
using FIELD = FIELD<C_TEConcussiveExplosion>;
public class C_TEConcussiveExplosion : C_TEParticleSystem
{
	public static readonly RecvTable DT_TEConcussiveExplosion = new(DT_TEParticleSystem, [
		RecvPropVector(FIELD.OF(nameof(Normal))),
		RecvPropFloat(FIELD.OF(nameof(LScale))),
		RecvPropInt(FIELD.OF(nameof(Radius))),
		RecvPropInt(FIELD.OF(nameof(Magnitude))),
	]);
	public static readonly new ClientClass ClientClass = new ClientClass("TEConcussiveExplosion", DT_TEConcussiveExplosion).WithManualClassID(StaticClassIndices.CTEConcussiveExplosion);

	public Vector3 Normal;
	public float LScale;
	public int Radius;
	public int Magnitude;
}
