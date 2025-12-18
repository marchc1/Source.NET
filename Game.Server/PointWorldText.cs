using Source.Common;
using Source;
using Game.Shared;
using System.Numerics;
namespace Game.Server;
using FIELD = FIELD<PointWorldText>;
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
	public static readonly new ServerClass ServerClass = new ServerClass("PointWorldText", DT_PointWorldText).WithManualClassID(StaticClassIndices.CPointWorldText);

	public InlineArray512<char> SzText;
	public int ColTextColor;
	public float TextSize;
	public float TextSpacingX;
	public float TextSpacingY;
	public int Orientation;
	public bool Rainbow;
	public bool TextEnabled;
}
