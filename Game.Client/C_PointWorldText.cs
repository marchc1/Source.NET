using Source.Common;
using Source;
using Game.Shared;
using System.Numerics;
namespace Game.Client;
using FIELD = FIELD<C_PointWorldText>;
[LinkEntityToClass("point_worldtext")]
[NetworkName("CPointWorldText")]
public class C_PointWorldText : C_BaseEntity
{
	public static readonly RecvTable DT_PointWorldText = new(DT_BaseEntity, [
		RecvPropString(FIELD.OF(nameof(SzText))),
		RecvPropInt(FIELD.OF(nameof(ColTextColor))),
		RecvPropFloat(FIELD.OF(nameof(TextSize))),
		RecvPropFloat(FIELD.OF(nameof(TextSpacingX))),
		RecvPropFloat(FIELD.OF(nameof(TextSpacingY))),
		RecvPropInt(FIELD.OF(nameof(Orientation))),
		RecvPropBool(FIELD.OF(nameof(Rainbow))),
		RecvPropBool(FIELD.OF(nameof(TextEnabled))),
	]);
	public static readonly new ClientClass ClientClass = new ClientClass(DT_PointWorldText);

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
