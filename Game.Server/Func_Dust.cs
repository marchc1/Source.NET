using Source.Common;
using Source;

using Game.Shared;

namespace Game.Server;


using FIELD = FIELD<Func_Dust>;

[NetworkName("CFunc_Dust")]
public partial class Func_Dust : BaseEntity
{
	public static readonly SendTable DT_Func_Dust = new([
		SendPropInt(NetworkVarFields.Color, 32, PropFlags.Unsigned),
		SendPropInt(NetworkVarFields.SpawnRate, 12, PropFlags.Unsigned),
		SendPropInt(NetworkVarFields.SpeedMax, 12, PropFlags.Unsigned),
		SendPropFloat(NetworkVarFields.SizeMin, 0, PropFlags.NoScale),
		SendPropFloat(NetworkVarFields.SizeMax, 0, PropFlags.NoScale),
		SendPropInt(NetworkVarFields.DistMax, 16, PropFlags.Unsigned),
		SendPropInt(NetworkVarFields.LifetimeMin, 4, PropFlags.Unsigned),
		SendPropInt(NetworkVarFields.LifetimeMax, 4, PropFlags.Unsigned),
		SendPropInt(NetworkVarFields.DustFlags, 3, PropFlags.Unsigned),
		SendPropModelIndex(BaseEntity.NetworkVarFields.ModelIndex),
		SendPropFloat(NetworkVarFields.FallSpeed, 0, PropFlags.NoScale),
		SendPropBool(NetworkVarFields.AffectedByWind),
		SendPropDataTable("m_Collision", CollisionProperty.DT_CollisionProperty)
	]);
	public static readonly new ServerClass ServerClass = new ServerClass(DT_Func_Dust);

	[NetworkName("m_Color")]
	[NetworkVar] public partial Color Color { get; set; }
	[NetworkName("m_SpawnRate")]
	[NetworkVar] public partial int SpawnRate { get; set; }
	[NetworkName("m_SpeedMax")]
	[NetworkVar] public partial int SpeedMax { get; set; }
	[NetworkName("m_flSizeMin")]
	[NetworkVar] public partial float SizeMin { get; set; }
	[NetworkName("m_flSizeMax")]
	[NetworkVar] public partial float SizeMax { get; set; }
	[NetworkName("m_DistMax")]
	[NetworkVar] public partial int DistMax { get; set; }
	[NetworkName("m_LifetimeMin")]
	[NetworkVar] public partial int LifetimeMin { get; set; }
	[NetworkName("m_LifetimeMax")]
	[NetworkVar] public partial int LifetimeMax { get; set; }
	[NetworkName("m_DustFlags")]
	[NetworkVar] public partial int DustFlags { get; set; }
	[NetworkName("m_FallSpeed")]
	[NetworkVar] public partial float FallSpeed { get; set; }
	[NetworkName("m_bAffectedByWind")]
	[NetworkVar] public partial bool AffectedByWind { get; set; }
}
