using Source.Common;
using Source;

using Game.Shared;

namespace Game.Server;

using FIELD = FIELD<WaterLODControl>;

[NetworkName("CWaterLODControl")]
public partial class WaterLODControl : BaseEntity
{
	public static readonly SendTable DT_WaterLODControl = new([
		SendPropFloat(NetworkVarFields.CheapWaterStartDistance, 0, PropFlags.NoScale),
		SendPropFloat(NetworkVarFields.CheapWaterEndDistance, 0, PropFlags.NoScale),
	]);
	public static readonly new ServerClass ServerClass = new ServerClass(DT_WaterLODControl);

	[NetworkName("m_flCheapWaterStartDistance")]
	[NetworkVar] public partial float CheapWaterStartDistance { get; set; }
	[NetworkName("m_flCheapWaterEndDistance")]
	[NetworkVar] public partial float CheapWaterEndDistance { get; set; }
}
