using Game.Shared;

using Source.Common;

namespace Game.Client; 
using FIELD = Source.FIELD<C_TEEffectDispatch>;

public class C_TEEffectDispatch : C_BaseTempEntity
{
	public static readonly RecvTable DT_TEEffectDispatch = new(DT_BaseTempEntity, [
		RecvPropDataTable(nameof(EffectData), FIELD.OF(nameof(EffectData)), EffectData.DT_EffectData, 0, RECV_GET_OBJECT_AT_FIELD(FIELD.OF(nameof(EffectData)))),
	]);
	public static readonly new ClientClass ClientClass = new ClientClass("TEEffectDispatch", DT_TEEffectDispatch).WithManualClassID(StaticClassIndices.CTEEffectDispatch);

	public readonly EffectData EffectData = new();
}
