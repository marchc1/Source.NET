using Source.Common;
using Source;
using Game.Shared;
using System.Numerics;
namespace Game.Server;
using FIELD = FIELD<MaterialModifyControl>;
[LinkEntityToClass("material_modify_control")]
[NetworkName("CMaterialModifyControl")]
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
	public static readonly new ServerClass ServerClass = new ServerClass(DT_MaterialModifyControl);

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
