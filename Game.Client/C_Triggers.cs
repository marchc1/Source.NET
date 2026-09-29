using Game.Shared;

using Source.Common;
using Source;

namespace Game.Client;
using FIELD_BT = FIELD<C_BaseTrigger>;

// I don't know if this is correct, but they have a datatable, so...

[NetworkName("CBaseTrigger")]
public class C_BaseTrigger : BaseToggle
{
	public static readonly RecvTable DT_BaseTrigger = new(DT_BaseToggle, [
		RecvPropBool(FIELD_BT.OF(nameof(ClientSidePredicted))),
		RecvPropInt(FIELD_BT.OF(nameof(SpawnFlags)))
	]);
	public static readonly new ClientClass ClientClass = new ClientClass(DT_BaseTrigger);
	[NetworkName("m_bClientSidePredicted")]
	public bool ClientSidePredicted;
}
