using Source.Common;
using Source;
using Game.Shared;
using System.Numerics;
namespace Game.Server;
using FIELD = FIELD<TEBeamFollow>;
[NetworkName("CTEBeamFollow")]
public class TEBeamFollow(ReadOnlySpan<char> name) : BaseBeam(name)
{
	public static readonly SendTable DT_TEBeamFollow = new(DT_BaseBeam, [
		SendPropInt(FIELD.OF(nameof(EntIndex)), 24, PropFlags.Unsigned),
	]);
	public static readonly new ServerClass ServerClass = new ServerClass(DT_TEBeamFollow);

	[NetworkName("m_iEntIndex")]
	public int EntIndex;
}
