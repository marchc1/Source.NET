using Source.Common;
using Source;
using Game.Shared;
using System.Numerics;
namespace Game.Server;
using FIELD = FIELD<TEBeamEnts>;
[NetworkName("CTEBeamEnts")]
public class TEBeamEnts(ReadOnlySpan<char> name) : BaseBeam(name)
{
	public static readonly SendTable DT_TEBeamEnts = new(DT_BaseBeam, [
		SendPropInt(FIELD.OF(nameof(StartEntity)), 24, PropFlags.Unsigned),
		SendPropInt(FIELD.OF(nameof(EndEntity)), 24, PropFlags.Unsigned),
	]);
	public static readonly new ServerClass ServerClass = new ServerClass(DT_TEBeamEnts);

	[NetworkName("m_nStartEntity")]
	public int StartEntity;
	[NetworkName("m_nEndEntity")]
	public int EndEntity;
}
