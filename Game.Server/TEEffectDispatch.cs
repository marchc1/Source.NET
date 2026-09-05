using Game.Shared;

using Source.Common;

namespace Game.Server;
using FIELD = Source.FIELD<TEEffectDispatch>;

public class TEEffectDispatch(ReadOnlySpan<char> name) : BaseTempEntity(name)
{
	public static readonly SendTable DT_TEEffectDispatch = new(DT_BaseTempEntity, [
		SendPropDataTable(nameof(EffectData), FIELD.OF(nameof(EffectData)), EffectData.DT_EffectData),
	]);
	public static readonly new ServerClass ServerClass = new ServerClass("TEEffectDispatch", DT_TEEffectDispatch).WithManualClassID(StaticClassIndices.CTEEffectDispatch);

	public readonly EffectData EffectData = new();
}
