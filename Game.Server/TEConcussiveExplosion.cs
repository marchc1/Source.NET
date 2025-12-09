using Source.Common;
using Source;
using Game.Shared;
using System.Numerics;
namespace Game.Server;
using FIELD = FIELD<TEConcussiveExplosion>;
public class TEConcussiveExplosion : TEParticleSystem
{
	public static readonly SendTable DT_TEConcussiveExplosion = new(DT_TEParticleSystem, [
		SendPropVector(FIELD.OF(nameof(Normal)), 0, PropFlags.Coord),
		SendPropFloat(FIELD.OF(nameof(Scale)), 0, PropFlags.NoScale),
		SendPropInt(FIELD.OF(nameof(Radius)), 32, PropFlags.Unsigned),
		SendPropInt(FIELD.OF(nameof(Magnitude)), 32, PropFlags.Unsigned),
	]);
	public static readonly new ServerClass ServerClass = new ServerClass("TEConcussiveExplosion", DT_TEConcussiveExplosion).WithManualClassID(StaticClassIndices.CTEConcussiveExplosion);

	public Vector3 Normal;
	public float Scale;
	public int Radius;
	public int Magnitude;
}
