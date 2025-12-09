using Source.Common;
using Source;
using Game.Shared;
using System.Numerics;
namespace Game.Server;
using FIELD = FIELD<Tesla>;
public class Tesla : BaseEntity
{
	public static readonly SendTable DT_Tesla = new(DT_BaseEntity, [
		SendPropString(FIELD.OF(nameof(SoundName))),
		SendPropString(FIELD.OF(nameof(SpriteName))),
	]);
	public static readonly new ServerClass ServerClass = new ServerClass("Tesla", DT_Tesla).WithManualClassID(StaticClassIndices.CTesla);

	public InlineArray64<char> SoundName;
	public InlineArray256<char> SpriteName;
}
