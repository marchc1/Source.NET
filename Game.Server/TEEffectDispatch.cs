using Game.Shared;

using Source.Common;

namespace Game.Server;
using FIELD = Source.FIELD<TEEffectDispatch>;

[NetworkName("CTEEffectDispatch")]
public class TEEffectDispatch(ReadOnlySpan<char> name) : BaseTempEntity(name)
{
	public static readonly SendTable DT_TEEffectDispatch = new(DT_BaseTempEntity, [
		SendPropDataTable("m_EffectData", FIELD.OF(nameof(EffectData)), EffectData.DT_EffectData),
	]);
	public static readonly new ServerClass ServerClass = new ServerClass(DT_TEEffectDispatch);

	[NetworkName("m_EffectData")]
	public readonly EffectData EffectData = new();
}
