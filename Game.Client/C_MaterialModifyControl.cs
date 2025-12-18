using Source.Common;
using Source;
using Game.Shared;
using System.Numerics;
namespace Game.Client;
using FIELD = FIELD<C_MaterialModifyControl>;
public class C_MaterialModifyControl : C_BaseEntity
{
	public static readonly RecvTable DT_MaterialModifyControl = new(DT_BaseEntity, [
		RecvPropString(FIELD.OF(nameof(SzMaterialName))),
		RecvPropString(FIELD.OF(nameof(SzMaterialVar))),
		RecvPropString(FIELD.OF(nameof(SzMaterialVarValue))),
		RecvPropInt(FIELD.OF(nameof(FrameStart))),
		RecvPropInt(FIELD.OF(nameof(FrameEnd))),
		RecvPropBool(FIELD.OF(nameof(Wrap))),
		RecvPropFloat(FIELD.OF(nameof(Framerate))),
		RecvPropBool(FIELD.OF(nameof(NewAnimCommandsSemaphore))),
		RecvPropFloat(FIELD.OF(nameof(FloatLerpStartValue))),
		RecvPropFloat(FIELD.OF(nameof(FloatLerpEndValue))),
		RecvPropFloat(FIELD.OF(nameof(FloatLerpTransitionTime))),
		RecvPropInt(FIELD.OF(nameof(ModifyMode))),
	]);
	public static readonly new ClientClass ClientClass = new ClientClass("MaterialModifyControl", DT_MaterialModifyControl).WithManualClassID(StaticClassIndices.CMaterialModifyControl);

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
