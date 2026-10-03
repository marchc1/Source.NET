using Source.Common;
using Source;
using Game.Shared;
using System.Numerics;
namespace Game.Server;
using FIELD = FIELD<PointWorldText>;
[LinkEntityToClass("point_worldtext")]
[NetworkName("CPointWorldText")]
public class PointWorldText : BaseEntity
{
	public static readonly SendTable DT_PointWorldText = new(DT_BaseEntity, [
		SendPropString(FIELD.OF(nameof(SzText))),
		SendPropInt(FIELD.OF(nameof(ColTextColor)), 32, PropFlags.Unsigned),
		SendPropFloat(FIELD.OF(nameof(TextSize)), 0, PropFlags.NoScale),
		SendPropFloat(FIELD.OF(nameof(TextSpacingX)), 0, PropFlags.NoScale),
		SendPropFloat(FIELD.OF(nameof(TextSpacingY)), 0, PropFlags.NoScale),
		SendPropInt(FIELD.OF(nameof(Orientation)), 3, PropFlags.Unsigned),
		SendPropBool(FIELD.OF(nameof(Rainbow))),
		SendPropBool(FIELD.OF(nameof(TextEnabled))),
	]);
	public static readonly new ServerClass ServerClass = new ServerClass(DT_PointWorldText);

	[NetworkName("m_szText")]
	public InlineArray512<char> SzText;
	[NetworkName("m_colTextColor")]
	public int ColTextColor;
	[NetworkName("m_flTextSize")]
	public float TextSize;
	[NetworkName("m_flTextSpacingX")]
	public float TextSpacingX;
	[NetworkName("m_flTextSpacingY")]
	public float TextSpacingY;
	[NetworkName("m_nOrientation")]
	public int Orientation;
	[NetworkName("m_bRainbow")]
	public bool Rainbow;
	[NetworkName("m_bTextEnabled")]
	public bool TextEnabled;
}
