using Game.Shared;

using Source.Common;

namespace Game.Server;
using FIELD = Source.FIELD<TEPlayerAnimEvent>;

public class TEPlayerAnimEvent : BaseTempEntity
{
	public static readonly SendTable DT_TEPlayerAnimEvent = new([
		SendPropEHandle(FIELD.OF(nameof(Player))),
		SendPropInt(FIELD.OF(nameof(Event)), 6, PropFlags.Unsigned),
		SendPropInt(FIELD.OF(nameof(Data)), 32)
	]);
	public static readonly new ServerClass ServerClass = new ServerClass("PhysMagnet", DT_TEPlayerAnimEvent).WithManualClassID(StaticClassIndices.CTEPlayerAnimEvent);

	public readonly EHANDLE Player = new();
	public readonly int Event;
	public readonly int Data;
}
