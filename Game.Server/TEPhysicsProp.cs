using Source.Common;
using Source;
using Game.Shared;
using System.Numerics;
using Source.Common.Mathematics;
namespace Game.Server;
using FIELD = FIELD<TEPhysicsProp>;
public class TEPhysicsProp : BaseTempEntity
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
	public static readonly new ServerClass ServerClass = new ServerClass("TEPhysicsProp", DT_TEPhysicsProp).WithManualClassID(StaticClassIndices.CTEPhysicsProp);

	public Vector3 Origin;
	public QAngle Rotation;
	public Vector3 Velocity;
	public int ModelIndex;
	public int Skin;
	public int Flags;
	public int Effects;
	public int ClrRender;
	public float ModelScale;
}
