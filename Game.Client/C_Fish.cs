using Source.Common;
using Source;
using Game.Shared;
using System.Numerics;
namespace Game.Client;
using FIELD = FIELD<C_Fish>;
[NetworkName("CFish")]
public class C_Fish : C_BaseAnimating
{
	public static readonly RecvTable DT_CFish = new([
		RecvPropVector(FIELD.OF(nameof(PoolOrigin))),
		RecvPropFloat(FIELD.OF(nameof(Le))),
		RecvPropFloat(FIELD.OF(nameof(X))),
		RecvPropFloat(FIELD.OF(nameof(Y))),
		RecvPropFloat(FIELD.OF(nameof(Z))),
		RecvPropInt(FIELD.OF(nameof(ModelIndex))),
		RecvPropInt(FIELD.OF(nameof(LifeState))),
		RecvPropFloat(FIELD.OF(nameof(WaterLevel))),
	]);
	public static readonly new ClientClass ClientClass = new ClientClass(DT_CFish);

	[NetworkName("m_poolOrigin")]
	public Vector3 PoolOrigin;
	[NetworkName("m_angle")]
	public float Le;
	[NetworkName("m_x")]
	public float X;
	[NetworkName("m_y")]
	public float Y;
	[NetworkName("m_z")]
	public float Z;
	[NetworkName("m_waterLevel")]
	public new float WaterLevel;
}
