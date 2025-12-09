using Source.Common;
using Source;
using Game.Shared;
using System.Numerics;
namespace Game.Server;
using FIELD = FIELD<TEBeamEnts>;
public class TEBeamEnts : BaseBeam
{
	public static readonly SendTable DT_TEBeamEnts = new(DT_BaseBeam, [
		SendPropInt(FIELD.OF(nameof(StartEntity)), 24, PropFlags.Unsigned),
		SendPropInt(FIELD.OF(nameof(EndEntity)), 24, PropFlags.Unsigned),
	]);
	public static readonly new ServerClass ServerClass = new ServerClass("TEBeamEnts", DT_TEBeamEnts).WithManualClassID(StaticClassIndices.CTEBeamEnts);

	public int StartEntity;
	public int EndEntity;
}
