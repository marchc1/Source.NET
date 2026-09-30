using Source.Common;
using Source;
using Game.Shared;
using System.Numerics;
namespace Game.Server;
using FIELD = FIELD<PointWorldText>;
[NetworkName("CPointWorldText")]
public partial class PointWorldText : BaseEntity
{
	public static readonly SendTable DT_PointWorldText = new(DT_BaseEntity, [
		SendPropString(FIELD.OF(nameof(SzText))),
		SendPropInt(NetworkVarFields.ColTextColor, 32, PropFlags.Unsigned),
		SendPropFloat(NetworkVarFields.TextSize, 0, PropFlags.NoScale),
		SendPropFloat(NetworkVarFields.TextSpacingX, 0, PropFlags.NoScale),
		SendPropFloat(NetworkVarFields.TextSpacingY, 0, PropFlags.NoScale),
		SendPropInt(NetworkVarFields.Orientation, 3, PropFlags.Unsigned),
		SendPropBool(NetworkVarFields.Rainbow),
		SendPropBool(NetworkVarFields.TextEnabled),
	]);
	public static readonly new ServerClass ServerClass = new ServerClass(DT_PointWorldText);

	[NetworkName("m_szText")]
	public InlineArray512<char> SzText;
	[NetworkName("m_colTextColor")]
	[NetworkVar] public partial int ColTextColor { get; set; }
	[NetworkName("m_flTextSize")]
	[NetworkVar] public partial float TextSize { get; set; }
	[NetworkName("m_flTextSpacingX")]
	[NetworkVar] public partial float TextSpacingX { get; set; }
	[NetworkName("m_flTextSpacingY")]
	[NetworkVar] public partial float TextSpacingY { get; set; }
	[NetworkName("m_nOrientation")]
	[NetworkVar] public partial int Orientation { get; set; }
	[NetworkName("m_bRainbow")]
	[NetworkVar] public partial bool Rainbow { get; set; }
	[NetworkName("m_bTextEnabled")]
	[NetworkVar] public partial bool TextEnabled { get; set; }
}
