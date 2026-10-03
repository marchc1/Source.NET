using Source.Common;
using Source;

using Game.Shared;

namespace Game.Server;

using FIELD = FIELD<WaterLODControl>;

[LinkEntityToClass("water_lod_control")]
[NetworkName("CWaterLODControl")]
public class WaterLODControl : BaseEntity
{
	public static readonly SendTable DT_WaterLODControl = new([
		SendPropFloat(FIELD.OF(nameof(CheapWaterStartDistance)), 0, PropFlags.NoScale),
		SendPropFloat(FIELD.OF(nameof(CheapWaterEndDistance)), 0, PropFlags.NoScale),
	]);
	public static readonly new ServerClass ServerClass = new ServerClass(DT_WaterLODControl);

	[NetworkName("m_flCheapWaterStartDistance")]
	public float CheapWaterStartDistance;
	[NetworkName("m_flCheapWaterEndDistance")]
	public float CheapWaterEndDistance;
}
