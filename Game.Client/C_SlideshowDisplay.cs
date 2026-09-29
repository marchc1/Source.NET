using Source.Common;
using Source;
using Game.Shared;
using System.Numerics;
namespace Game.Client;
using FIELD = FIELD<C_SlideshowDisplay>;
[NetworkName("CSlideshowDisplay")]
public class C_SlideshowDisplay : C_BaseEntity
{
	public static readonly RecvTable DT_SlideshowDisplay = new(DT_BaseEntity, [
		RecvPropBool(FIELD.OF(nameof(Enabled))),
		RecvPropString(FIELD.OF(nameof(DisplayText))),
		RecvPropString(FIELD.OF(nameof(SlideshowDirectory))),
		RecvPropArray3(FIELD.OF_ARRAY(nameof(ChCurrentSlideLists)), RecvPropInt(FIELD.OF_ARRAYINDEX(nameof(ChCurrentSlideLists), 0))),
		RecvPropFloat(FIELD.OF(nameof(MinSlideTime))),
		RecvPropFloat(FIELD.OF(nameof(MaxSlideTime))),
		RecvPropInt(FIELD.OF(nameof(CycleType))),
		RecvPropBool(FIELD.OF(nameof(NoListRepeats))),
	]);
	public static readonly new ClientClass ClientClass = new ClientClass(DT_SlideshowDisplay);

	[NetworkName("m_bEnabled")]
	public bool Enabled;
	[NetworkName("m_szDisplayText")]
	public InlineArray128<char> DisplayText;
	[NetworkName("m_szSlideshowDirectory")]
	public InlineArray128<char> SlideshowDirectory;
	[NetworkName("m_chCurrentSlideLists")]
	public InlineArray16<byte> ChCurrentSlideLists;
	[NetworkName("m_fMinSlideTime")]
	public float MinSlideTime;
	[NetworkName("m_fMaxSlideTime")]
	public float MaxSlideTime;
	[NetworkName("m_iCycleType")]
	public int CycleType;
	[NetworkName("m_bNoListRepeats")]
	public bool NoListRepeats;
}
