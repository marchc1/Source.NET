using Source.Common;
using Source;
using Game.Shared;
using System.Numerics;
namespace Game.Server;
using FIELD = FIELD<SlideshowDisplay>;
[NetworkName("CSlideshowDisplay")]
public partial class SlideshowDisplay : BaseEntity
{
	public static readonly SendTable DT_SlideshowDisplay = new(DT_BaseEntity, [
		SendPropBool(NetworkVarFields.Enabled),
		SendPropString(FIELD.OF(nameof(DisplayText))),
		SendPropString(FIELD.OF(nameof(SlideshowDirectory))),
		SendPropArray3(SlideshowDisplay.NetworkVarFields.ChCurrentSlideLists, SendPropInt(SlideshowDisplay.NetworkVarFields.ChCurrentSlideLists.AtIndex(0)!, 8, PropFlags.Unsigned)),
		SendPropFloat(NetworkVarFields.MinSlideTime, 11, 0, 0.0f, 20.0f),
		SendPropFloat(NetworkVarFields.MaxSlideTime, 11, 0, 0.0f, 20.0f),
		SendPropInt(NetworkVarFields.CycleType, 2, PropFlags.Unsigned),
		SendPropBool(NetworkVarFields.NoListRepeats),
	]);
	public static readonly new ServerClass ServerClass = new ServerClass(DT_SlideshowDisplay);

	[NetworkName("m_bEnabled")]
	[NetworkVar] public partial bool Enabled { get; set; }
	[NetworkName("m_szDisplayText")]
	public InlineArray128<char> DisplayText;
	[NetworkName("m_szSlideshowDirectory")]
	public InlineArray128<char> SlideshowDirectory;
	[NetworkName("m_chCurrentSlideLists")]
	[NetworkVar] public partial NetworkArray<InlineArray16<byte>, byte> ChCurrentSlideLists { get; }
	[NetworkName("m_fMinSlideTime")]
	[NetworkVar] public partial float MinSlideTime { get; set; }
	[NetworkName("m_fMaxSlideTime")]
	[NetworkVar] public partial float MaxSlideTime { get; set; }
	[NetworkName("m_iCycleType")]
	[NetworkVar] public partial int CycleType { get; set; }
	[NetworkName("m_bNoListRepeats")]
	[NetworkVar] public partial bool NoListRepeats { get; set; }
}
