using Game.Shared;

using Source;
using Source.Common;
using Source.Common.Mathematics;

using System.Numerics;
namespace Game.Server;
using FIELD = FIELD<TEBreakModel>;
[NetworkName("CTEBreakModel")]
public class TEBreakModel(ReadOnlySpan<char> name) : BaseTempEntity(name)
{
	public static readonly SendTable DT_TEBreakModel = new(DT_BaseTempEntity, [
		SendPropVector(FIELD.OF(nameof(Origin)), 0, PropFlags.Coord),
		SendPropAngle(FIELD.OF_VECTORELEM(nameof(Rotation), 0), 13, PropFlags.RoundDown),
		SendPropAngle(FIELD.OF_VECTORELEM(nameof(Rotation), 1), 13, PropFlags.RoundDown),
		SendPropAngle(FIELD.OF_VECTORELEM(nameof(Rotation), 2), 13, PropFlags.RoundDown),
		SendPropVector(FIELD.OF(nameof(Size)), 0, PropFlags.Coord),
		SendPropVector(FIELD.OF(nameof(Velocity)), 0, PropFlags.Coord),
		SendPropInt(FIELD.OF(nameof(ModelIndex)), 14, 0),
		SendPropInt(FIELD.OF(nameof(Randomization)), 9, PropFlags.Unsigned),
		SendPropInt(FIELD.OF(nameof(Count)), 8, PropFlags.Unsigned),
		SendPropFloat(FIELD.OF(nameof(Time)), 10, 0, 0, 102.4f),
		SendPropInt(FIELD.OF(nameof(Flags)), 8, PropFlags.Unsigned),
	]);
	public static readonly new ServerClass ServerClass = new ServerClass(DT_TEBreakModel);

	[NetworkName("m_vecOrigin")]
	public Vector3 Origin;
	[NetworkName("m_angRotation")]
	public QAngle Rotation;
	[NetworkName("m_vecSize")]
	public Vector3 Size;
	[NetworkName("m_vecVelocity")]
	public Vector3 Velocity;
	[NetworkName("m_nModelIndex")]
	public int ModelIndex;
	[NetworkName("m_nRandomization")]
	public int Randomization;
	[NetworkName("m_nCount")]
	public int Count;
	[NetworkName("m_fTime")]
	public float Time;
	[NetworkName("m_nFlags")]
	public int Flags;
}
