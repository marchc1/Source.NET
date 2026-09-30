using Source.Common;
using Source;

using Game.Shared;

namespace Game.Server;

using static Source.Common.Networking.SVC_ClassInfo;

using FIELD = FIELD<DynamicLight>;

[NetworkName("CDynamicLight")]
public partial class DynamicLight : BaseEntity
{
	public static readonly SendTable DT_DynamicLight = new(DT_BaseEntity, [
		SendPropInt(NetworkVarFields.Flags, 4, PropFlags.Unsigned),
		SendPropInt(NetworkVarFields.LightStyle, 4, PropFlags.Unsigned),
		SendPropFloat(NetworkVarFields.Radius, 0, PropFlags.NoScale),
		SendPropInt(NetworkVarFields.Exponent, 8),
		SendPropFloat(NetworkVarFields.InnerAngle, 8, 0, 0.0f, 360.0f),
		SendPropFloat(NetworkVarFields.OuterAngle, 8, 0, 0.0f, 360.0f),
		SendPropFloat(NetworkVarFields.SpotRadius, 0, PropFlags.NoScale),
	]);
	public static readonly new ServerClass ServerClass = new ServerClass(DT_DynamicLight);

	[NetworkName("m_Flags")]
	[NetworkVar] public partial int Flags { get; set; }
	[NetworkName("m_LightStyle")]
	[NetworkVar] public partial int LightStyle { get; set; }
	[NetworkName("m_Radius")]
	[NetworkVar] public partial float Radius { get; set; }
	[NetworkName("m_Exponent")]
	[NetworkVar] public partial int Exponent { get; set; }
	[NetworkName("m_InnerAngle")]
	[NetworkVar] public partial float InnerAngle { get; set; }
	[NetworkName("m_OuterAngle")]
	[NetworkVar] public partial float OuterAngle { get; set; }
	[NetworkName("m_SpotRadius")]
	[NetworkVar] public partial float SpotRadius { get; set; }
}
