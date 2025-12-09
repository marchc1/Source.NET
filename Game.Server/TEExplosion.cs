using Source.Common;
using Source;
using Game.Shared;
using System.Numerics;
namespace Game.Server;
using FIELD = FIELD<TEExplosion>;
public class TEExplosion : TEParticleSystem
{
	public static readonly SendTable DT_TEExplosion = new(DT_TEParticleSystem, [
		SendPropInt(FIELD.OF(nameof(ModelIndex)), 14, 0),
		SendPropFloat(FIELD.OF(nameof(Scale)), 9, 0),
		SendPropInt(FIELD.OF(nameof(FrameRate)), 8, PropFlags.Unsigned),
		SendPropInt(FIELD.OF(nameof(Flags)), 8, PropFlags.Unsigned),
		SendPropVector(FIELD.OF(nameof(Normal)), 0, PropFlags.Coord),
		SendPropInt(FIELD.OF(nameof(ChMaterialType)), 8, PropFlags.Unsigned),
		SendPropInt(FIELD.OF(nameof(Radius)), 32, PropFlags.Unsigned),
		SendPropInt(FIELD.OF(nameof(Magnitude)), 32, PropFlags.Unsigned),
	]);
	public static readonly new ServerClass ServerClass = new ServerClass("TEExplosion", DT_TEExplosion).WithManualClassID(StaticClassIndices.CTEExplosion);

	public int ModelIndex;
	public float Scale;
	public int FrameRate;
	public int Flags;
	public Vector3 Normal;
	public int ChMaterialType;
	public int Radius;
	public int Magnitude;
}
