using Source.Common;
using Source;
using Game.Shared;
using System.Numerics;
namespace Game.Server;
using FIELD = FIELD<MaterialModifyControl>;
public class MaterialModifyControl : BaseEntity
{
	public static readonly SendTable DT_MaterialModifyControl = new(DT_BaseEntity, [
		SendPropString(FIELD.OF(nameof(SzMaterialName))),
		SendPropString(FIELD.OF(nameof(SzMaterialVar))),
		SendPropString(FIELD.OF(nameof(SzMaterialVarValue))),
		SendPropInt(FIELD.OF(nameof(FrameStart)), 8, 0),
		SendPropInt(FIELD.OF(nameof(FrameEnd)), 8, 0),
		SendPropBool(FIELD.OF(nameof(Wrap))),
		SendPropFloat(FIELD.OF(nameof(Framerate)), 0, PropFlags.NoScale),
		SendPropBool(FIELD.OF(nameof(NewAnimCommandsSemaphore))),
		SendPropFloat(FIELD.OF(nameof(FloatLerpStartValue)), 0, PropFlags.NoScale),
		SendPropFloat(FIELD.OF(nameof(FloatLerpEndValue)), 0, PropFlags.NoScale),
		SendPropFloat(FIELD.OF(nameof(FloatLerpTransitionTime)), 0, PropFlags.NoScale),
		SendPropInt(FIELD.OF(nameof(ModifyMode)), 2, PropFlags.Unsigned),
	]);
	public static readonly new ServerClass ServerClass = new ServerClass("MaterialModifyControl", DT_MaterialModifyControl).WithManualClassID(StaticClassIndices.CMaterialModifyControl);

	public InlineArray255<char> SzMaterialName;
	public InlineArray255<char> SzMaterialVar;
	public InlineArray255<char> SzMaterialVarValue;
	public int FrameStart;
	public int FrameEnd;
	public bool Wrap;
	public float Framerate;
	public bool NewAnimCommandsSemaphore;
	public float FloatLerpStartValue;
	public float FloatLerpEndValue;
	public float FloatLerpTransitionTime;
	public int ModifyMode;
}
