using Source.Common;
using Source;

using Game.Shared;

namespace Game.Server;

using static Source.Common.Networking.SVC_ClassInfo;

using FIELD = FIELD<DynamicLight>;

public class DynamicLight : BaseEntity
{
	public static readonly SendTable DT_DynamicLight = new(DT_BaseEntity, [
		SendPropInt(FIELD.OF(nameof(Flags)), 4, PropFlags.Unsigned),
		SendPropInt(FIELD.OF(nameof(LightStyle)), 4, PropFlags.Unsigned),
		SendPropFloat(FIELD.OF(nameof(Radius)), 0, PropFlags.NoScale),
		SendPropInt(FIELD.OF(nameof(Exponent)), 8),
		SendPropFloat(FIELD.OF(nameof(InnerAngle)), 8, 0, 360),
		SendPropFloat(FIELD.OF(nameof(OuterAngle)), 8, 0, 360),
		SendPropFloat(FIELD.OF(nameof(SpotRadius)), 0, PropFlags.NoScale),
	]);
	public static readonly new ServerClass ServerClass = new ServerClass("DynamicLight", DT_DynamicLight).WithManualClassID(StaticClassIndices.CDynamicLight);

	public int Flags;
	public int LightStyle;
	public float Radius;
	public int Exponent;
	public float InnerAngle;
	public float OuterAngle;
	public float SpotRadius;
}
