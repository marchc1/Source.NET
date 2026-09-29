using Source.Common;
using Source;

using Game.Shared;

namespace Game.Server;


using FIELD = FIELD<Func_Dust>;

[NetworkName("CFunc_Dust")]
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
		SendPropInt(FIELD.OF(nameof(DustFlags)), 3, PropFlags.Unsigned),
		SendPropModelIndex(FIELD.OF(nameof(ModelIndex))),
		SendPropFloat(FIELD.OF(nameof(FallSpeed)), 0, PropFlags.NoScale),
		SendPropBool(FIELD.OF(nameof(AffectedByWind))),
		SendPropDataTable("m_Collision", CollisionProperty.DT_CollisionProperty)
	]);
	public static readonly new ServerClass ServerClass = new ServerClass(DT_Func_Dust);

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
