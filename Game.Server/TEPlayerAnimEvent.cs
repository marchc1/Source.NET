using Game.Shared;

using Source.Common;

namespace Game.Server;
using FIELD = Source.FIELD<TEPlayerAnimEvent>;

[NetworkName("CTEPlayerAnimEvent")]
public class TEPlayerAnimEvent(ReadOnlySpan<char> name) : BaseTempEntity(name)
{
	public static readonly SendTable DT_TEPlayerAnimEvent = new([
		SendPropEHandle(FIELD.OF(nameof(Player))),
		SendPropInt(FIELD.OF(nameof(Event)), 6, PropFlags.Unsigned),
		SendPropInt(FIELD.OF(nameof(Data)), 32)
	]);
	public static readonly new ServerClass ServerClass = new ServerClass(DT_TEPlayerAnimEvent);

	[NetworkName("m_hPlayer")]
	public EHANDLE Player = new();
	[NetworkName("m_iEvent")]
	public readonly int Event;
	[NetworkName("m_nData")]
	public readonly int Data;
}
