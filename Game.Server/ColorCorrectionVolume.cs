using Source.Common;
using Source;
using Game.Shared;
using System.Numerics;
namespace Game.Server;
using FIELD = FIELD<ColorCorrectionVolume>;
[NetworkName("CColorCorrectionVolume")]
public partial class ColorCorrectionVolume : BaseEntity
{
	public static readonly SendTable DT_ColorCorrectionVolume = new([
		SendPropBool(NetworkVarFields.Enabled),
		SendPropFloat(NetworkVarFields.MaxWeight, 0, PropFlags.NoScale),
		SendPropFloat(NetworkVarFields.FadeDuration, 0, PropFlags.NoScale),
		SendPropFloat(NetworkVarFields.Weight, 0, PropFlags.NoScale),
		SendPropString(FIELD.OF(nameof(LookupFilename))),
		SendPropInt(BaseEntity.NetworkVarFields.ModelIndex, 14, 0),
	]);
	public static readonly new ServerClass ServerClass = new ServerClass(DT_ColorCorrectionVolume);

	[NetworkName("m_bEnabled")]
	[NetworkVar] public partial bool Enabled { get; set; }
	[NetworkName("m_MaxWeight")]
	[NetworkVar] public partial float MaxWeight { get; set; }
	[NetworkName("m_FadeDuration")]
	[NetworkVar] public partial float FadeDuration { get; set; }
	[NetworkName("m_Weight")]
	[NetworkVar] public partial float Weight { get; set; }
	[NetworkName("m_lookupFilename")]
	public InlineArrayMaxPath<char> LookupFilename;
}
