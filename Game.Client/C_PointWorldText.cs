using Source.Common;
using Source;
using Game.Shared;
using System.Numerics;
namespace Game.Client;
using FIELD = FIELD<C_PointWorldText>;
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
	public static readonly new ClientClass ClientClass = new ClientClass("PointWorldText", DT_PointWorldText).WithManualClassID(StaticClassIndices.CPointWorldText);

	public InlineArray512<char> SzText;
	public int ColTextColor;
	public float TextSize;
	public float TextSpacingX;
	public float TextSpacingY;
	public int Orientation;
	public bool Rainbow;
	public bool TextEnabled;
}
