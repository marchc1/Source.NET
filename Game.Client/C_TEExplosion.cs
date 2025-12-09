using Source.Common;
using Source;
using Game.Shared;
using System.Numerics;
namespace Game.Client;
using FIELD = FIELD<C_TEExplosion>;
public class C_TEExplosion : C_TEParticleSystem
{
	public static readonly RecvTable DT_TEExplosion = new(DT_TEParticleSystem, [
		RecvPropInt(FIELD.OF(nameof(ModelIndex))),
		RecvPropFloat(FIELD.OF(nameof(Scale))),
		RecvPropInt(FIELD.OF(nameof(FrameRate))),
		RecvPropInt(FIELD.OF(nameof(Flags))),
		RecvPropVector(FIELD.OF(nameof(Normal))),
		RecvPropInt(FIELD.OF(nameof(ChMaterialType))),
		RecvPropInt(FIELD.OF(nameof(Radius))),
		RecvPropInt(FIELD.OF(nameof(Magnitude))),
	]);
	public static readonly new ClientClass ClientClass = new ClientClass("TEExplosion", DT_TEExplosion).WithManualClassID(StaticClassIndices.CTEExplosion);

	public int ModelIndex;
	public float Scale;
	public int FrameRate;
	public int Flags;
	public Vector3 Normal;
	public int ChMaterialType;
	public int Radius;
	public int Magnitude;
}
