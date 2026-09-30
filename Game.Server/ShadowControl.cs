using Game.Shared;

using Source;
using Source.Common;

using System.Numerics;

namespace Game.Server;
using FIELD = Source.FIELD<ShadowControl>;

[NetworkName("CShadowControl")]
public partial class ShadowControl : BaseEntity
{
	public static readonly SendTable DT_ShadowControl = new([
		SendPropVector(NetworkVarFields.ShadowDirection, -1, PropFlags.NoScale),
		SendPropInt(NetworkVarFields.ShadowColor, 32, PropFlags.Unsigned),
		SendPropFloat(NetworkVarFields.ShadowMaxDist, 0, PropFlags.NoScale),
		SendPropBool(NetworkVarFields.DisableShadows),
		SendPropBool(NetworkVarFields.EnableLocalLightShadows),
	]);
	public static readonly new ServerClass ServerClass = new ServerClass(DT_ShadowControl);

	[NetworkName("m_shadowDirection")]
	[NetworkVar] public partial Vector3 ShadowDirection { get; set; }
	[NetworkName("m_shadowColor")]
	[NetworkVar] public partial Color ShadowColor { get; set; }
	[NetworkName("m_flShadowMaxDist")]
	[NetworkVar] public partial float ShadowMaxDist { get; set; }
	[NetworkName("m_bDisableShadows")]
	[NetworkVar] public partial bool DisableShadows { get; set; }
	[NetworkName("m_bEnableLocalLightShadows")]
	[NetworkVar] public partial bool EnableLocalLightShadows { get; set; }
}