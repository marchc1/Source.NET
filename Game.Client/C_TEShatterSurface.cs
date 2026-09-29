using Source.Common;
using Source;
using Game.Shared;
using System.Numerics;
using Source.Common.Mathematics;
namespace Game.Client;
using FIELD = FIELD<C_TEShatterSurface>;
[NetworkName("CTEShatterSurface")]
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
	public static readonly new ClientClass ClientClass = new ClientClass(DT_TEShatterSurface);

	[NetworkName("m_vecOrigin")]
	public Vector3 Origin;
	[NetworkName("m_vecAngles")]
	public Vector3 Angles;
	[NetworkName("m_vecForce")]
	public Vector3 Force;
	[NetworkName("m_vecForcePos")]
	public Vector3 ForcePos;
	[NetworkName("m_flWidth")]
	public float Width;
	[NetworkName("m_flHeight")]
	public float Height;
	[NetworkName("m_flShardSize")]
	public float ShardSize;
	[NetworkName("m_nSurfaceType")]
	public int SurfaceType;
	[NetworkName("m_uchFrontColor")]
	public InlineArray3<byte> UchFrontColor;
	[NetworkName("m_uchBackColor")]
	public InlineArray3<byte> UchBackColor;
}

public static partial class TempEnts
{
	public static void TE_ShatterSurface(IRecipientFilter filter, float delay, in Vector3 pos, in QAngle angle, in Vector3 force, in Vector3 forcePos, float width, float height, float shardSize, int surfaceType, int frontR, int frontG, int frontB, int backR, int backG, int backB) {
		throw new NotImplementedException();
	}
}
