using Source.Common;
using Source;
using Game.Shared;
using System.Numerics;
namespace Game.Client;
using FIELD = FIELD<C_ColorCorrectionVolume>;
public class C_ColorCorrectionVolume : C_BaseEntity
{
	public static readonly RecvTable DT_ColorCorrectionVolume = new([
		RecvPropBool(FIELD.OF(nameof(Enabled))),
		RecvPropFloat(FIELD.OF(nameof(MaxWeight))),
		RecvPropFloat(FIELD.OF(nameof(FadeDuration))),
		RecvPropFloat(FIELD.OF(nameof(Weight))),
		RecvPropString(FIELD.OF(nameof(LookupFilename))),
		RecvPropInt(FIELD.OF(nameof(ModelIndex))),
	]);
	public static readonly new ClientClass ClientClass = new ClientClass("ColorCorrectionVolume", DT_ColorCorrectionVolume).WithManualClassID(StaticClassIndices.CColorCorrectionVolume);

	public bool Enabled;
	public float MaxWeight;
	public float FadeDuration;
	public float Weight;
	public InlineArrayMaxPath<char> LookupFilename;
}
