using Source.Common;
using Source;
using Game.Shared;
using System.Numerics;
namespace Game.Server;
using FIELD = FIELD<TESparks>;
public class TESparks : TEParticleSystem
{
	public static readonly SendTable DT_TESparks = new(DT_TEParticleSystem, [
		SendPropInt(FIELD.OF(nameof(Magnitude)), 4, PropFlags.Unsigned),
		SendPropInt(FIELD.OF(nameof(TrailLength)), 4, PropFlags.Unsigned),
		SendPropVector(FIELD.OF(nameof(Dir)), 0, PropFlags.Coord),
	]);
	public static readonly new ServerClass ServerClass = new ServerClass("TESparks", DT_TESparks).WithManualClassID(StaticClassIndices.CTESparks);

	public int Magnitude;
	public int TrailLength;
	public Vector3 Dir;
}
