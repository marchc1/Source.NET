using Source.Common;
using Source;

using Game.Shared;

namespace Game.Server;

using FIELD = FIELD<WaterLODControl>;

public class WaterLODControl : BaseEntity
{
	public static readonly SendTable DT_WaterLODControl = new([
		SendPropFloat(FIELD.OF(nameof(CheapWaterStartDistance)), 0, PropFlags.NoScale),
		SendPropFloat(FIELD.OF(nameof(CheapWaterEndDistance)), 0, PropFlags.NoScale),
	]);
	public static readonly new ServerClass ServerClass = new ServerClass("WaterLODControl", DT_WaterLODControl).WithManualClassID(StaticClassIndices.CWaterLODControl);

	public float CheapWaterStartDistance;
	public float CheapWaterEndDistance;
}
