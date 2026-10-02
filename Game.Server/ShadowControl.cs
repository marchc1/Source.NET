using Game.Shared;

using Source;
using Source.Common;

using System.Numerics;

namespace Game.Server;
using FIELD = Source.FIELD<ShadowControl>;

[LinkEntityToClass("shadow_control")]
[NetworkName("CShadowControl")]
public class ShadowControl : BaseEntity
{
	public static readonly SendTable DT_ShadowControl = new([
		SendPropVector(FIELD.OF(nameof(ShadowDirection)), -1, PropFlags.NoScale),
		SendPropInt(FIELD.OF(nameof(ShadowColor)), 32, PropFlags.Unsigned),
		SendPropFloat(FIELD.OF(nameof(ShadowMaxDist)), 0, PropFlags.NoScale),
		SendPropBool(FIELD.OF(nameof(DisableShadows))),
		SendPropBool(FIELD.OF(nameof(EnableLocalLightShadows))),
	]);
	public static readonly new ServerClass ServerClass = new ServerClass(DT_ShadowControl);

	[NetworkName("m_shadowDirection")]
	public Vector3 ShadowDirection;
	[NetworkName("m_shadowColor")]
	public Color ShadowColor;
	[NetworkName("m_flShadowMaxDist")]
	public float ShadowMaxDist;
	[NetworkName("m_bDisableShadows")]
	public bool DisableShadows;
	[NetworkName("m_bEnableLocalLightShadows")]
	public bool EnableLocalLightShadows;
}