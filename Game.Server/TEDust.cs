using Source.Common;
using Source;
using Game.Shared;
using System.Numerics;
namespace Game.Server;
using FIELD = FIELD<TEDust>;
public class TEDust : TEParticleSystem
{
	public static readonly SendTable DT_TEDust = new(DT_TEParticleSystem, [
		SendPropFloat(FIELD.OF(nameof(LSize)), 0, PropFlags.Coord | PropFlags.NoScale),
		SendPropFloat(FIELD.OF(nameof(LSpeed)), 0, PropFlags.Coord | PropFlags.NoScale),
		SendPropVector(FIELD.OF(nameof(Direction)), 4, 0),
	]);
	public static readonly new ServerClass ServerClass = new ServerClass("TEDust", DT_TEDust).WithManualClassID(StaticClassIndices.CTEDust);

	public float LSize;
	public float LSpeed;
	public Vector3 Direction;
}
