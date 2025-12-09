using Source.Common;
using Source;
using Game.Shared;
using System.Numerics;
namespace Game.Server;

using FIELD = FIELD<TEShatterSurface>;
public class TEShatterSurface : BaseTempEntity
{
	public static readonly SendTable DT_TEShatterSurface = new(DT_BaseTempEntity, [
		SendPropVector(FIELD.OF(nameof(Origin)), 0, PropFlags.Coord),
		SendPropVector(FIELD.OF(nameof(Angles)), 0, PropFlags.Coord),
		SendPropVector(FIELD.OF(nameof(Force)), 0, PropFlags.Coord),
		SendPropVector(FIELD.OF(nameof(ForcePos)), 0, PropFlags.Coord),
		SendPropFloat(FIELD.OF(nameof(Width)), 0, PropFlags.NoScale),
		SendPropFloat(FIELD.OF(nameof(Height)), 0, PropFlags.NoScale),
		SendPropFloat(FIELD.OF(nameof(ShardSize)), 0, PropFlags.NoScale),
		SendPropInt(FIELD.OF(nameof(SurfaceType)), 2, PropFlags.Unsigned),
		SendPropInt(FIELD.OF_ARRAYINDEX(nameof(UchFrontColor), 0), 8, PropFlags.Unsigned),
		SendPropInt(FIELD.OF_ARRAYINDEX(nameof(UchFrontColor), 1), 8, PropFlags.Unsigned),
		SendPropInt(FIELD.OF_ARRAYINDEX(nameof(UchFrontColor), 2), 8, PropFlags.Unsigned),
		SendPropInt(FIELD.OF_ARRAYINDEX(nameof(UchBackColor), 0), 8, PropFlags.Unsigned),
		SendPropInt(FIELD.OF_ARRAYINDEX(nameof(UchBackColor), 1), 8, PropFlags.Unsigned),
		SendPropInt(FIELD.OF_ARRAYINDEX(nameof(UchBackColor), 2), 8, PropFlags.Unsigned),
	]);
	public static readonly new ServerClass ServerClass = new ServerClass("TEShatterSurface", DT_TEShatterSurface).WithManualClassID(StaticClassIndices.CTEShatterSurface);

	public Vector3 Origin;
	public Vector3 Angles;
	public Vector3 Force;
	public Vector3 ForcePos;
	public float Width;
	public float Height;
	public float ShardSize;
	public int SurfaceType;
	public InlineArray3<byte> UchFrontColor;
	public InlineArray3<byte> UchBackColor;
}
