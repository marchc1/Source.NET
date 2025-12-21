using Source.Common;
using Source;
using Game.Shared;
using System.Numerics;
namespace Game.Server;
using FIELD = FIELD<ColorCorrectionVolume>;
public class ColorCorrectionVolume : BaseEntity
{
	public static readonly SendTable DT_ColorCorrectionVolume = new([
		SendPropBool(FIELD.OF(nameof(Enabled))),
		SendPropFloat(FIELD.OF(nameof(MaxWeight)), 0, PropFlags.NoScale),
		SendPropFloat(FIELD.OF(nameof(FadeDuration)), 0, PropFlags.NoScale),
		SendPropFloat(FIELD.OF(nameof(Weight)), 0, PropFlags.NoScale),
		SendPropString(FIELD.OF(nameof(LookupFilename))),
		SendPropInt(FIELD.OF(nameof(ModelIndex)), 16, 0),
	]);
	public static readonly new ServerClass ServerClass = new ServerClass("ColorCorrectionVolume", DT_ColorCorrectionVolume).WithManualClassID(StaticClassIndices.CColorCorrectionVolume);

	public bool Enabled;
	public float MaxWeight;
	public float FadeDuration;
	public float Weight;
	public InlineArrayMaxPath<char> LookupFilename;
}
