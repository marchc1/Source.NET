using Source.Common;
using Source;

using Game.Shared;

namespace Game.Client;

using FIELD = FIELD<C_WaterLODControl>;

public class C_WaterLODControl : C_BaseEntity
{
	public static readonly RecvTable DT_WaterLODControl = new([
		RecvPropFloat(FIELD.OF(nameof(CheapWaterStartDistance))),
		RecvPropFloat(FIELD.OF(nameof(CheapWaterEndDistance))),
	]);
	public static readonly new ClientClass ClientClass = new ClientClass("WaterLODControl", DT_WaterLODControl).WithManualClassID(StaticClassIndices.CWaterLODControl);

	public float CheapWaterStartDistance;
	public float CheapWaterEndDistance;
}

