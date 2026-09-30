using Source.Common;
using Source;
using Game.Shared;
using System.Numerics;
namespace Game.Server;
using FIELD = FIELD<SpatialEntity>;
[NetworkName("CSpatialEntity")]
public partial class SpatialEntity : BaseEntity
{
	public static readonly SendTable DT_SpatialEntity = new([
		SendPropVector(BaseEntity.NetworkVarFields.Origin, 0, PropFlags.NoScale),
		SendPropFloat(NetworkVarFields.MinFalloff, 0, PropFlags.NoScale),
		SendPropFloat(NetworkVarFields.MaxFalloff, 0, PropFlags.NoScale),
		SendPropFloat(NetworkVarFields.CurWeight, 0, PropFlags.NoScale),
		SendPropBool(NetworkVarFields.Enabled),
	]);
	public static readonly new ServerClass ServerClass = new ServerClass(DT_SpatialEntity);

	[NetworkName("m_MinFalloff")]
	[NetworkVar] public partial float MinFalloff { get; set; }
	[NetworkName("m_MaxFalloff")]
	[NetworkVar] public partial float MaxFalloff { get; set; }
	[NetworkName("m_flCurWeight")]
	[NetworkVar] public partial float CurWeight { get; set; }
	[NetworkName("m_bEnabled")]
	[NetworkVar] public partial bool Enabled { get; set; }
}
