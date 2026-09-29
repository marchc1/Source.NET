using Source.Common;
using Source;
using Game.Shared;
using System.Numerics;
namespace Game.Client;
using FIELD = FIELD<C_ColorCorrectionVolume>;
[NetworkName("CColorCorrectionVolume")]
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
	public static readonly new ClientClass ClientClass = new ClientClass(DT_ColorCorrectionVolume);

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
