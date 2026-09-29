using Source.Common;
using Source;
using Game.Shared;
using System.Numerics;
namespace Game.Client;
using FIELD = FIELD<C_MaterialModifyControl>;
[NetworkName("CMaterialModifyControl")]
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
	public static readonly new ClientClass ClientClass = new ClientClass(DT_MaterialModifyControl);

	[NetworkName("m_szMaterialName")]
	public InlineArray255<char> SzMaterialName;
	[NetworkName("m_szMaterialVar")]
	public InlineArray255<char> SzMaterialVar;
	[NetworkName("m_szMaterialVarValue")]
	public InlineArray255<char> SzMaterialVarValue;
	[NetworkName("m_iFrameStart")]
	public int FrameStart;
	[NetworkName("m_iFrameEnd")]
	public int FrameEnd;
	[NetworkName("m_bWrap")]
	public bool Wrap;
	[NetworkName("m_flFramerate")]
	public float Framerate;
	[NetworkName("m_bNewAnimCommandsSemaphore")]
	public bool NewAnimCommandsSemaphore;
	[NetworkName("m_flFloatLerpStartValue")]
	public float FloatLerpStartValue;
	[NetworkName("m_flFloatLerpEndValue")]
	public float FloatLerpEndValue;
	[NetworkName("m_flFloatLerpTransitionTime")]
	public float FloatLerpTransitionTime;
	[NetworkName("m_nModifyMode")]
	public int ModifyMode;
}
