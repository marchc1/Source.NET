using Source.Common;
using Source;
using Game.Shared;
using System.Numerics;
namespace Game.Server;
using FIELD = FIELD<MaterialModifyControl>;
[NetworkName("CMaterialModifyControl")]
public partial class MaterialModifyControl : BaseEntity
{
	public static readonly SendTable DT_MaterialModifyControl = new(DT_BaseEntity, [
		SendPropString(FIELD.OF(nameof(SzMaterialName))),
		SendPropString(FIELD.OF(nameof(SzMaterialVar))),
		SendPropString(FIELD.OF(nameof(SzMaterialVarValue))),
		SendPropInt(NetworkVarFields.FrameStart, 8, 0),
		SendPropInt(NetworkVarFields.FrameEnd, 8, 0),
		SendPropBool(NetworkVarFields.Wrap),
		SendPropFloat(NetworkVarFields.Framerate, 0, PropFlags.NoScale),
		SendPropBool(NetworkVarFields.NewAnimCommandsSemaphore),
		SendPropFloat(NetworkVarFields.FloatLerpStartValue, 0, PropFlags.NoScale),
		SendPropFloat(NetworkVarFields.FloatLerpEndValue, 0, PropFlags.NoScale),
		SendPropFloat(NetworkVarFields.FloatLerpTransitionTime, 0, PropFlags.NoScale),
		SendPropInt(NetworkVarFields.ModifyMode, 2, PropFlags.Unsigned),
	]);
	public static readonly new ServerClass ServerClass = new ServerClass(DT_MaterialModifyControl);

	[NetworkName("m_szMaterialName")]
	public InlineArray255<char> SzMaterialName;
	[NetworkName("m_szMaterialVar")]
	public InlineArray255<char> SzMaterialVar;
	[NetworkName("m_szMaterialVarValue")]
	public InlineArray255<char> SzMaterialVarValue;
	[NetworkName("m_iFrameStart")]
	[NetworkVar] public partial int FrameStart { get; set; }
	[NetworkName("m_iFrameEnd")]
	[NetworkVar] public partial int FrameEnd { get; set; }
	[NetworkName("m_bWrap")]
	[NetworkVar] public partial bool Wrap { get; set; }
	[NetworkName("m_flFramerate")]
	[NetworkVar] public partial float Framerate { get; set; }
	[NetworkName("m_bNewAnimCommandsSemaphore")]
	[NetworkVar] public partial bool NewAnimCommandsSemaphore { get; set; }
	[NetworkName("m_flFloatLerpStartValue")]
	[NetworkVar] public partial float FloatLerpStartValue { get; set; }
	[NetworkName("m_flFloatLerpEndValue")]
	[NetworkVar] public partial float FloatLerpEndValue { get; set; }
	[NetworkName("m_flFloatLerpTransitionTime")]
	[NetworkVar] public partial float FloatLerpTransitionTime { get; set; }
	[NetworkName("m_nModifyMode")]
	[NetworkVar] public partial int ModifyMode { get; set; }
}
