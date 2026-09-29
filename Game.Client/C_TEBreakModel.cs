using Source.Common;
using Source;
using Game.Shared;
using System.Numerics;
using Source.Common.Mathematics;
namespace Game.Client;
using FIELD = FIELD<C_TEBreakModel>;
[NetworkName("CTEBreakModel")]
public class C_TEBreakModel : C_BaseTempEntity
{
	public static readonly RecvTable DT_TEBreakModel = new(DT_BaseTempEntity, [
		RecvPropVector(FIELD.OF(nameof(Origin))),
		RecvPropFloat(FIELD.OF_VECTORELEM(nameof(Rotation), 0)),
		RecvPropFloat(FIELD.OF_VECTORELEM(nameof(Rotation), 1)),
		RecvPropFloat(FIELD.OF_VECTORELEM(nameof(Rotation), 2)),
		RecvPropVector(FIELD.OF(nameof(Size))),
		RecvPropVector(FIELD.OF(nameof(Velocity))),
		RecvPropInt(FIELD.OF(nameof(ModelIndex))),
		RecvPropInt(FIELD.OF(nameof(Randomization))),
		RecvPropInt(FIELD.OF(nameof(Count))),
		RecvPropFloat(FIELD.OF(nameof(Time))),
		RecvPropInt(FIELD.OF(nameof(Flags))),
	]);
	public static readonly new ClientClass ClientClass = new ClientClass(DT_TEBreakModel);

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

public static partial class TempEnts
{
	public static void TE_BreakModel(IRecipientFilter filter, float delay, in Vector3 pos, in QAngle angles, in Vector3 size, in Vector3 vel, int modelIndex, int randomization, int count, float time, int flags) {
		throw new NotImplementedException();
	}
}
