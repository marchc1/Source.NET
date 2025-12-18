using Source.Common;
using Source;
using Game.Shared;
using System.Numerics;
namespace Game.Server;
using FIELD = FIELD<SlideshowDisplay>;
public class SlideshowDisplay : BaseEntity
{
	public static readonly SendTable DT_SlideshowDisplay = new(DT_BaseEntity, [
		SendPropBool(FIELD.OF(nameof(Enabled))),
		SendPropString(FIELD.OF(nameof(DisplayText))),
		SendPropString(FIELD.OF(nameof(SlideshowDirectory))),
		SendPropInt(FIELD.OF(nameof(ChCurrentSlideLists)), 8, PropFlags.ProxyAlwaysYes | PropFlags.Unsigned),
		SendPropFloat(FIELD.OF(nameof(MinSlideTime)), 11, 0),
		SendPropFloat(FIELD.OF(nameof(MaxSlideTime)), 11, 0),
		SendPropInt(FIELD.OF(nameof(CycleType)), 2, PropFlags.Unsigned),
		SendPropBool(FIELD.OF(nameof(NoListRepeats))),
	]);
	public static readonly new ServerClass ServerClass = new ServerClass("SlideshowDisplay", DT_SlideshowDisplay).WithManualClassID(StaticClassIndices.CSlideshowDisplay);

	public bool Enabled;
	public InlineArray128<char> DisplayText;
	public InlineArray128<char> SlideshowDirectory;
	public int ChCurrentSlideLists;
	public float MinSlideTime;
	public float MaxSlideTime;
	public int CycleType;
	public bool NoListRepeats;
}
