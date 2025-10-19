using Source.Common;
using Source;

using Game.Shared;

namespace Game.Server;


using FIELD = FIELD<Func_Dust>;

public class Func_Dust : BaseEntity
{
	public static readonly SendTable DT_Func_Dust = new([
		SendPropInt(FIELD.OF(nameof(Color)), 32, PropFlags.Unsigned),
		SendPropInt(FIELD.OF(nameof(SpawnRate)), 12, PropFlags.Unsigned),
		SendPropInt(FIELD.OF(nameof(SpeedMax)), 12, PropFlags.Unsigned),
		SendPropFloat(FIELD.OF(nameof(SizeMin)), 0, PropFlags.NoScale),
		SendPropFloat(FIELD.OF(nameof(SizeMax)), 0, PropFlags.NoScale),
		SendPropInt(FIELD.OF(nameof(DistMax)), 16, PropFlags.Unsigned),
		SendPropInt(FIELD.OF(nameof(LifetimeMin)), 4, PropFlags.Unsigned),
		SendPropInt(FIELD.OF(nameof(LifetimeMax)), 4, PropFlags.Unsigned),
		SendPropInt(FIELD.OF(nameof(DustFlags)), 4, PropFlags.Unsigned),
		SendPropModelIndex(FIELD.OF(nameof(ModelIndex))),
		SendPropFloat(FIELD.OF(nameof(FallSpeed)), 0, PropFlags.NoScale),
		SendPropBool(FIELD.OF(nameof(AffectedByWind))),
		SendPropDataTable("Collision", CollisionProperty.DT_CollisionProperty)
	]);
	public static readonly new ServerClass ServerClass = new ServerClass("Func_Dust", DT_Func_Dust).WithManualClassID(StaticClassIndices.CFunc_Dust);

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
