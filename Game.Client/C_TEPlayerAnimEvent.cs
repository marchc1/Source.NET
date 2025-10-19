using Game.Shared;

using Source.Common;

namespace Game.Client; 
using FIELD = Source.FIELD<C_TEPlayerAnimEvent>;

public class C_TEPlayerAnimEvent : C_BaseTempEntity
{
	public static readonly RecvTable DT_TEPlayerAnimEvent = new([
		RecvPropEHandle(FIELD.OF(nameof(Player))),
		RecvPropInt(FIELD.OF(nameof(Event))),
		RecvPropInt(FIELD.OF(nameof(Data)))
	]);
	public static readonly new ClientClass ClientClass = new ClientClass("TEPlayerAnimEvent", DT_TEPlayerAnimEvent).WithManualClassID(StaticClassIndices.CTEPlayerAnimEvent);

	public readonly EHANDLE Player = new();
	public readonly int Event;
	public readonly int Data;
}
