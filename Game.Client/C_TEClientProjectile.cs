using Source.Common;
using Source;
using Game.Shared;
using System.Numerics;
namespace Game.Client;
using FIELD = FIELD<C_TEClientProjectile>;
[NetworkName("CTEClientProjectile")]
public class C_TEClientProjectile : C_BaseTempEntity
{
	public static readonly RecvTable DT_TEClientProjectile = new(DT_BaseTempEntity, [
		RecvPropVector(FIELD.OF(nameof(Origin))),
		RecvPropVector(FIELD.OF(nameof(Velocity))),
		RecvPropInt(FIELD.OF(nameof(ModelIndex))),
		RecvPropInt(FIELD.OF(nameof(LifeTime))),
		RecvPropInt(FIELD.OF(nameof(HOwner))),
	]);
	public static readonly new ClientClass ClientClass = new ClientClass(DT_TEClientProjectile);

	[NetworkName("m_vecOrigin")]
	public Vector3 Origin;
	[NetworkName("m_vecVelocity")]
	public Vector3 Velocity;
	[NetworkName("m_nModelIndex")]
	public int ModelIndex;
	[NetworkName("m_nLifeTime")]
	public int LifeTime;
	[NetworkName("m_hOwner")]
	public int HOwner;
}

public static partial class TempEnts
{
	public static void TE_ClientProjectile(IRecipientFilter filter, float delay, in Vector3 origin, in Vector3 velocity, int modelIndex, int lifetime, C_BaseEntity? owner) {
		throw new NotImplementedException();
	}
}
