using Game.Shared;

using Source;
using Source.Common;

using System.Numerics;
namespace Game.Client;
using FIELD = FIELD<C_Func_Dust>;

[NetworkName("CFunc_Dust")]
public class C_Func_Dust : C_BaseEntity
{
	public static readonly RecvTable DT_Func_Dust = new([
		RecvPropInt(FIELD.OF(nameof(Color))),
		RecvPropInt(FIELD.OF(nameof(SpawnRate))),
		RecvPropInt(FIELD.OF(nameof(SpeedMax))),
		RecvPropFloat(FIELD.OF(nameof(SizeMin))),
		RecvPropFloat(FIELD.OF(nameof(SizeMax))),
		RecvPropInt(FIELD.OF(nameof(DistMax))),
		RecvPropInt(FIELD.OF(nameof(LifetimeMin))),
		RecvPropInt(FIELD.OF(nameof(LifetimeMax))),
		RecvPropInt(FIELD.OF(nameof(DustFlags))),
		RecvPropInt(FIELD.OF(nameof(ModelIndex))),
		RecvPropFloat(FIELD.OF(nameof(FallSpeed))),
		RecvPropBool(FIELD.OF(nameof(AffectedByWind))),
		RecvPropDataTable("m_Collision", CollisionProperty.DT_CollisionProperty, 0, RECV_GET_OBJECT_AT_FIELD(FIELD.OF(nameof(Collision))))
	]);
	public static readonly new ClientClass ClientClass = new ClientClass(DT_Func_Dust);

	[NetworkName("m_Color")]
	public Color Color;
	[NetworkName("m_SpawnRate")]
	public int SpawnRate;
	[NetworkName("m_SpeedMax")]
	public int SpeedMax;
	[NetworkName("m_flSizeMin")]
	public float SizeMin;
	[NetworkName("m_flSizeMax")]
	public float SizeMax;
	[NetworkName("m_DistMax")]
	public int DistMax;
	[NetworkName("m_LifetimeMin")]
	public int LifetimeMin;
	[NetworkName("m_LifetimeMax")]
	public int LifetimeMax;
	[NetworkName("m_DustFlags")]
	public int DustFlags;
	[NetworkName("m_FallSpeed")]
	public float FallSpeed;
	[NetworkName("m_bAffectedByWind")]
	public bool AffectedByWind;
}

public static partial class TempEnts
{
	public static void TE_Dust(IRecipientFilter filter, float delay, in Vector3 pos, in Vector3 dir, float size, float speed) {
		throw new NotImplementedException();
	}
}
