using Game.Shared;

using Source;
using Source.Common;

namespace Game.Client;
using FIELD = FIELD<C_Func_Dust>;

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
		RecvPropDataTable("Collision", CollisionProperty.DT_CollisionProperty, 0, RECV_GET_OBJECT_AT_FIELD(FIELD.OF(nameof(Collision))))
	]);
	public static readonly new ClientClass ClientClass = new ClientClass("Func_Dust", DT_Func_Dust).WithManualClassID(StaticClassIndices.CFunc_Dust);

	public Color Color;
	public int SpawnRate;
	public int SpeedMax;
	public float SizeMin;
	public float SizeMax;
	public int DistMax;
	public int LifetimeMin;
	public int LifetimeMax;
	public int DustFlags;
	public float FallSpeed;
	public bool AffectedByWind;
}

