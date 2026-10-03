using Source.Common;
using Source;
using Game.Shared;
using System.Numerics;
namespace Game.Server;
using FIELD = FIELD<ColorCorrectionVolume>;
[LinkEntityToClass("color_correction_volume")]
[NetworkName("CColorCorrectionVolume")]
public class ColorCorrectionVolume : BaseEntity
{
	public static readonly SendTable DT_ColorCorrectionVolume = new([
		SendPropBool(FIELD.OF(nameof(Enabled))),
		SendPropFloat(FIELD.OF(nameof(MaxWeight)), 0, PropFlags.NoScale),
		SendPropFloat(FIELD.OF(nameof(FadeDuration)), 0, PropFlags.NoScale),
		SendPropFloat(FIELD.OF(nameof(Weight)), 0, PropFlags.NoScale),
		SendPropString(FIELD.OF(nameof(LookupFilename))),
		SendPropInt(FIELD.OF(nameof(ModelIndex)), 14, 0),
	]);
	public static readonly new ServerClass ServerClass = new ServerClass(DT_ColorCorrectionVolume);

	[NetworkName("m_bEnabled")]
	public bool Enabled;
	[NetworkName("m_MaxWeight")]
	public float MaxWeight;
	[NetworkName("m_FadeDuration")]
	public float FadeDuration;
	[NetworkName("m_Weight")]
	public float Weight;
	[NetworkName("m_lookupFilename")]
	public InlineArrayMaxPath<char> LookupFilename;
}
