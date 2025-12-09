using Source.Common;
using Source;
using Game.Shared;
using System.Numerics;
namespace Game.Server;
using FIELD = FIELD<TEShowLine>;
public class TEShowLine : TEParticleSystem
{
	public static readonly SendTable DT_TEShowLine = new(DT_TEParticleSystem, [
		SendPropVector(FIELD.OF(nameof(End)), 0, PropFlags.Coord),
	]);
	public static readonly new ServerClass ServerClass = new ServerClass("TEShowLine", DT_TEShowLine).WithManualClassID(StaticClassIndices.CTEShowLine);

	public Vector3 End;
}
