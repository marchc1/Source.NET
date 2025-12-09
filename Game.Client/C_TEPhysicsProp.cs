using Source.Common;
using Source;
using Game.Shared;
using System.Numerics;
using Source.Common.Mathematics;
namespace Game.Client;
using FIELD = FIELD<C_TEPhysicsProp>;
public class C_TEPhysicsProp : C_BaseTempEntity
{
	public static readonly RecvTable DT_TEPhysicsProp = new(DT_BaseTempEntity, [
		RecvPropVector(FIELD.OF(nameof(Origin))),
		RecvPropFloat(FIELD.OF_VECTORELEM(nameof(Rotation), 0)),
		RecvPropFloat(FIELD.OF_VECTORELEM(nameof(Rotation), 1)),
		RecvPropFloat(FIELD.OF_VECTORELEM(nameof(Rotation), 2)),
		RecvPropVector(FIELD.OF(nameof(Velocity))),
		RecvPropInt(FIELD.OF(nameof(ModelIndex))),
		RecvPropInt(FIELD.OF(nameof(Skin))),
		RecvPropInt(FIELD.OF(nameof(Flags))),
		RecvPropInt(FIELD.OF(nameof(Effects))),
		RecvPropInt(FIELD.OF(nameof(ClrRender))),
		RecvPropFloat(FIELD.OF(nameof(ModelScale))),
	]);
	public static readonly new ClientClass ClientClass = new ClientClass("TEPhysicsProp", DT_TEPhysicsProp).WithManualClassID(StaticClassIndices.CTEPhysicsProp);

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
