using Game.Shared;

using Source.Common;
using Source;


namespace Game.Server;
using FIELD_BT = FIELD<BaseTrigger>;
public class BaseTrigger : BaseToggle
{
	public static readonly SendTable DT_BaseTrigger = new(DT_BaseToggle, [
		SendPropBool(FIELD_BT.OF(nameof(ClientSidePredicted))),
		SendPropFloat(FIELD_BT.OF(nameof(SpawnFlags)), 32, PropFlags.NoScale)
	]);
	public static readonly new ServerClass ServerClass = new ServerClass("BaseTrigger", DT_BaseTrigger).WithManualClassID(StaticClassIndices.CBaseTrigger);
	public bool ClientSidePredicted;
}
