using Source.Common;
using Source;
using Game.Shared;
using System.Numerics;
namespace Game.Server;
using FIELD = FIELD<SlideshowDisplay>;
[LinkEntityToClass("vgui_slideshow_display")]
[NetworkName("CSlideshowDisplay")]
public class SlideshowDisplay : BaseEntity
{
	public static readonly SendTable DT_SlideshowDisplay = new(DT_BaseEntity, [
		SendPropBool(FIELD.OF(nameof(Enabled))),
		SendPropString(FIELD.OF(nameof(DisplayText))),
		SendPropString(FIELD.OF(nameof(SlideshowDirectory))),
		SendPropArray3(FIELD.OF_ARRAY(nameof(ChCurrentSlideLists)), SendPropInt(FIELD.OF_ARRAYINDEX(nameof(ChCurrentSlideLists), 0), 8, PropFlags.Unsigned)),
		SendPropFloat(FIELD.OF(nameof(MinSlideTime)), 11, 0, 0.0f, 20.0f),
		SendPropFloat(FIELD.OF(nameof(MaxSlideTime)), 11, 0, 0.0f, 20.0f),
		SendPropInt(FIELD.OF(nameof(CycleType)), 2, PropFlags.Unsigned),
		SendPropBool(FIELD.OF(nameof(NoListRepeats))),
	]);
	public static readonly new ServerClass ServerClass = new ServerClass(DT_SlideshowDisplay);

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
