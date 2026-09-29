using Game.Shared;

using Source.Common;

namespace Game.Client; 
using FIELD = Source.FIELD<C_TEPlayerAnimEvent>;

[NetworkName("CTEPlayerAnimEvent")]
public class C_TEPlayerAnimEvent : C_BaseTempEntity
{
	public static readonly RecvTable DT_TEPlayerAnimEvent = new([
		RecvPropEHandle(FIELD.OF(nameof(Player))),
		RecvPropInt(FIELD.OF(nameof(Event))),
		RecvPropInt(FIELD.OF(nameof(Data)))
	]);
	public static readonly new ClientClass ClientClass = new ClientClass(DT_TEPlayerAnimEvent);

	[NetworkName("m_hPlayer")]
	public EHANDLE Player = new();
	[NetworkName("m_iEvent")]
	public readonly int Event;
	[NetworkName("m_nData")]
	public readonly int Data;
}
