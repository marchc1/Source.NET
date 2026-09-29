using Game.Shared;

using Source.Common;

using System.Numerics;
namespace Game.Client; 
using FIELD = Source.FIELD<C_TEEffectDispatch>;

[NetworkName("CTEEffectDispatch")]
public class C_TEEffectDispatch : C_BaseTempEntity
{
	public static readonly RecvTable DT_TEEffectDispatch = new(DT_BaseTempEntity, [
		RecvPropDataTable("m_EffectData", FIELD.OF(nameof(EffectData)), EffectData.DT_EffectData, 0, RECV_GET_OBJECT_AT_FIELD(FIELD.OF(nameof(EffectData)))),
	]);
	public static readonly new ClientClass ClientClass = new ClientClass(DT_TEEffectDispatch);

	[NetworkName("m_EffectData")]
	public readonly EffectData EffectData = new();
}

public static partial class TempEnts
{
	public static void TE_DispatchEffect(IRecipientFilter filter, float delay, in Vector3 pos, ReadOnlySpan<char> name, EffectData data) {
		throw new NotImplementedException();
	}
}
