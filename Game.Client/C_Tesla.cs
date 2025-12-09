using Source.Common;
using Source;
using Game.Shared;
using System.Numerics;
namespace Game.Client;
using FIELD = FIELD<C_Tesla>;
public class C_Tesla : C_BaseEntity
{
	public static readonly RecvTable DT_Tesla = new(DT_BaseEntity, [
		RecvPropString(FIELD.OF(nameof(SoundName))),
		RecvPropString(FIELD.OF(nameof(SpriteName))),
	]);
	public static readonly new ClientClass ClientClass = new ClientClass("Tesla", DT_Tesla).WithManualClassID(StaticClassIndices.CTesla);

	public InlineArray64<char> SoundName;
	public InlineArray256<char> SpriteName;
}
