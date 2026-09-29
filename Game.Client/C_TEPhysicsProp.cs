using Source.Common;
using Source;
using Game.Shared;
using System.Numerics;
using Source.Common.Mathematics;
namespace Game.Client;
using FIELD = FIELD<C_TEPhysicsProp>;
[NetworkName("CTEPhysicsProp")]
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
	public static readonly new ClientClass ClientClass = new ClientClass(DT_TEPhysicsProp);

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

public static partial class TempEnts
{
	public static void TE_PhysicsProp(IRecipientFilter filter, float delay, int modelIndex, int skin, in Vector3 pos, in QAngle angles, in Vector3 vel, bool breakModel, int effects) {
		throw new NotImplementedException();
	}
}
