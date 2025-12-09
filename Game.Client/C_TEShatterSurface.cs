using Source.Common;
using Source;
using Game.Shared;
using System.Numerics;
namespace Game.Client;
using FIELD = FIELD<C_TEShatterSurface>;
public class C_TEShatterSurface : C_BaseTempEntity
{
	public static readonly RecvTable DT_TEShatterSurface = new(DT_BaseTempEntity, [
		RecvPropVector(FIELD.OF(nameof(Origin))),
		RecvPropVector(FIELD.OF(nameof(Angles))),
		RecvPropVector(FIELD.OF(nameof(Force))),
		RecvPropVector(FIELD.OF(nameof(ForcePos))),
		RecvPropFloat(FIELD.OF(nameof(Width))),
		RecvPropFloat(FIELD.OF(nameof(Height))),
		RecvPropFloat(FIELD.OF(nameof(ShardSize))),
		RecvPropInt(FIELD.OF(nameof(SurfaceType))),
		RecvPropInt(FIELD.OF_ARRAYINDEX(nameof(UchFrontColor), 0)),
		RecvPropInt(FIELD.OF_ARRAYINDEX(nameof(UchFrontColor), 1)),
		RecvPropInt(FIELD.OF_ARRAYINDEX(nameof(UchFrontColor), 2)),
		RecvPropInt(FIELD.OF_ARRAYINDEX(nameof(UchBackColor), 0)),
		RecvPropInt(FIELD.OF_ARRAYINDEX(nameof(UchBackColor), 1)),
		RecvPropInt(FIELD.OF_ARRAYINDEX(nameof(UchBackColor), 2)),
	]);
	public static readonly new ClientClass ClientClass = new ClientClass("TEShatterSurface", DT_TEShatterSurface).WithManualClassID(StaticClassIndices.CTEShatterSurface);

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
