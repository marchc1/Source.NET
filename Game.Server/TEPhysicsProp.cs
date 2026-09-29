using Source.Common;
using Source;
using Game.Shared;
using System.Numerics;
using Source.Common.Mathematics;
namespace Game.Server;
using FIELD = FIELD<TEPhysicsProp>;
[NetworkName("CTEPhysicsProp")]
public class TEPhysicsProp(ReadOnlySpan<char> name) : BaseTempEntity(name)
{
	public static readonly SendTable DT_TEPhysicsProp = new(DT_BaseTempEntity, [
		SendPropVector(FIELD.OF(nameof(Origin)), 0, PropFlags.Coord),
		SendPropAngle(FIELD.OF_VECTORELEM(nameof(Rotation), 0), 13, PropFlags.RoundDown | PropFlags.IsAVectorElem),
		SendPropAngle(FIELD.OF_VECTORELEM(nameof(Rotation), 1), 13, PropFlags.RoundDown | PropFlags.IsAVectorElem),
		SendPropAngle(FIELD.OF_VECTORELEM(nameof(Rotation), 2), 13, PropFlags.RoundDown | PropFlags.IsAVectorElem),
		SendPropVector(FIELD.OF(nameof(Velocity)), 0, PropFlags.Coord),
		SendPropInt(FIELD.OF(nameof(ModelIndex)), 14, 0),
		SendPropInt(FIELD.OF(nameof(Skin)), 10, 0),
		SendPropInt(FIELD.OF(nameof(Flags)), 2, PropFlags.Unsigned),
		SendPropInt(FIELD.OF(nameof(Effects)), 16, PropFlags.Unsigned),
		SendPropInt(FIELD.OF(nameof(ClrRender)), 32, PropFlags.Unsigned),
		SendPropFloat(FIELD.OF(nameof(ModelScale)), 0, PropFlags.NoScale),
	]);
	public static readonly new ServerClass ServerClass = new ServerClass(DT_TEPhysicsProp);

	[NetworkName("m_vecOrigin")]
	public Vector3 Origin;
	[NetworkName("m_angRotation")]
	public QAngle Rotation;
	[NetworkName("m_vecVelocity")]
	public Vector3 Velocity;
	[NetworkName("m_nModelIndex")]
	public int ModelIndex;
	[NetworkName("m_nSkin")]
	public int Skin;
	[NetworkName("m_nFlags")]
	public int Flags;
	[NetworkName("m_nEffects")]
	public int Effects;
	[NetworkName("m_clrRender")]
	public int ClrRender;
	[NetworkName("m_fModelScale")]
	public float ModelScale;
}
