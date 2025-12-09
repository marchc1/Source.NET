using Source.Common;
using Source;
using Game.Shared;
using System.Numerics;
namespace Game.Server;
using FIELD = FIELD<TEBeamFollow>;
public class TEBeamFollow : BaseBeam
{
	public static readonly SendTable DT_TEBeamFollow = new(DT_BaseBeam, [
		SendPropInt(FIELD.OF(nameof(EntIndex)), 24, PropFlags.Unsigned),
	]);
	public static readonly new ServerClass ServerClass = new ServerClass("TEBeamFollow", DT_TEBeamFollow).WithManualClassID(StaticClassIndices.CTEBeamFollow);

	public int EntIndex;
}
