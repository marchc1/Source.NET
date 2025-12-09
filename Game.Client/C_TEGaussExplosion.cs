using Source.Common;
using Source;
using Game.Shared;
using System.Numerics;
namespace Game.Client;
using FIELD = FIELD<C_TEGaussExplosion>;
public class C_TEGaussExplosion : C_TEParticleSystem
{
	public static readonly RecvTable DT_TEGaussExplosion = new(DT_TEParticleSystem, [
		RecvPropInt(FIELD.OF(nameof(Type))),
		RecvPropVector(FIELD.OF(nameof(Direction))),
	]);
	public static readonly new ClientClass ClientClass = new ClientClass("TEGaussExplosion", DT_TEGaussExplosion).WithManualClassID(StaticClassIndices.CTEGaussExplosion);

	public int Type;
	public Vector3 Direction;
}
