using Game.Shared;

using Source.Common;
using Source;

namespace Game.Client;
using FIELD_BT = FIELD<C_BaseTrigger>;

// I don't know if this is correct, but they have a datatable, so...

public class C_BaseTrigger : BaseToggle
{
	public static readonly RecvTable DT_BaseTrigger = new(DT_BaseToggle, [
		RecvPropBool(FIELD_BT.OF(nameof(ClientSidePredicted))),
		RecvPropFloat(FIELD_BT.OF(nameof(SpawnFlags)))
	]);
	public static readonly new ClientClass ClientClass = new ClientClass("BaseTrigger", DT_BaseTrigger).WithManualClassID(StaticClassIndices.CBaseTrigger);
	public bool ClientSidePredicted;
}
