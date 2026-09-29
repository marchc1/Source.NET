using Source.Common;
using Source;
using Game.Shared;
using System.Numerics;
namespace Game.Server;

using FIELD = FIELD<TEShatterSurface>;
[NetworkName("CTEShatterSurface")]
public class TEShatterSurface(ReadOnlySpan<char> name) : BaseTempEntity(name)
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
	public static readonly new ServerClass ServerClass = new ServerClass(DT_TEShatterSurface);

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
