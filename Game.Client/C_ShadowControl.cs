using Game.Client;
using Game.Shared;

using Source;
using Source.Common;

using System.Numerics;

namespace Game.Server;
using FIELD = Source.FIELD<C_ShadowControl>;

[NetworkName("CShadowControl")]
public class C_ShadowControl : C_BaseEntity
{
	public static readonly RecvTable DT_ShadowControl = new([
		RecvPropVector(FIELD.OF(nameof(ShadowDirection))),
		RecvPropInt(FIELD.OF(nameof(ShadowColor))),
		RecvPropFloat(FIELD.OF(nameof(ShadowMaxDist))),
		RecvPropBool(FIELD.OF(nameof(DisableShadows))),
		RecvPropBool(FIELD.OF(nameof(EnableLocalLightShadows))),
	]);
	public static readonly new ClientClass ClientClass = new ClientClass(DT_ShadowControl);

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
