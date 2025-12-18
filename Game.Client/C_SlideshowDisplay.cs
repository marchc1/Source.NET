using Source.Common;
using Source;
using Game.Shared;
using System.Numerics;
namespace Game.Client;
using FIELD = FIELD<C_SlideshowDisplay>;
public class C_SlideshowDisplay : C_BaseEntity
{
	public static readonly RecvTable DT_SlideshowDisplay = new(DT_BaseEntity, [
		RecvPropBool(FIELD.OF(nameof(Enabled))),
		RecvPropString(FIELD.OF(nameof(DisplayText))),
		RecvPropString(FIELD.OF(nameof(SlideshowDirectory))),
		RecvPropInt(FIELD.OF(nameof(ChCurrentSlideLists))),
		RecvPropFloat(FIELD.OF(nameof(MinSlideTime))),
		RecvPropFloat(FIELD.OF(nameof(MaxSlideTime))),
		RecvPropInt(FIELD.OF(nameof(CycleType))),
		RecvPropBool(FIELD.OF(nameof(NoListRepeats))),
	]);
	public static readonly new ClientClass ClientClass = new ClientClass("SlideshowDisplay", DT_SlideshowDisplay).WithManualClassID(StaticClassIndices.CSlideshowDisplay);

	public bool Enabled;
	public InlineArray128<char> DisplayText;
	public InlineArray128<char> SlideshowDirectory;
	public int ChCurrentSlideLists;
	public float MinSlideTime;
	public float MaxSlideTime;
	public int CycleType;
	public bool NoListRepeats;
}
