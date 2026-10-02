using Source.Common;
using Source;

using Game.Shared;

namespace Game.Server;

using static Source.Common.Networking.SVC_ClassInfo;

using FIELD = FIELD<DynamicLight>;

[LinkEntityToClass("light_dynamic")]
[NetworkName("CDynamicLight")]
public class DynamicLight : BaseEntity
{
	public static readonly SendTable DT_DynamicLight = new(DT_BaseEntity, [
		SendPropInt(FIELD.OF(nameof(Flags)), 4, PropFlags.Unsigned),
		SendPropInt(FIELD.OF(nameof(LightStyle)), 4, PropFlags.Unsigned),
		SendPropFloat(FIELD.OF(nameof(Radius)), 0, PropFlags.NoScale),
		SendPropInt(FIELD.OF(nameof(Exponent)), 8),
		SendPropFloat(FIELD.OF(nameof(InnerAngle)), 8, 0, 0.0f, 360.0f),
		SendPropFloat(FIELD.OF(nameof(OuterAngle)), 8, 0, 0.0f, 360.0f),
		SendPropFloat(FIELD.OF(nameof(SpotRadius)), 0, PropFlags.NoScale),
	]);
	public static readonly new ServerClass ServerClass = new ServerClass(DT_DynamicLight);

	[NetworkName("m_Flags")]
	public int Flags;
	[NetworkName("m_LightStyle")]
	public int LightStyle;
	[NetworkName("m_Radius")]
	public float Radius;
	[NetworkName("m_Exponent")]
	public int Exponent;
	[NetworkName("m_InnerAngle")]
	public float InnerAngle;
	[NetworkName("m_OuterAngle")]
	public float OuterAngle;
	[NetworkName("m_SpotRadius")]
	public float SpotRadius;
}
