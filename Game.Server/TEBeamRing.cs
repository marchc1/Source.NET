using Source.Common;
using Source;
using Game.Shared;
using System.Numerics;
namespace Game.Server;
using FIELD = FIELD<TEBeamRing>;
public class TEBeamRing : BaseBeam
{
	public static readonly SendTable DT_TEBeamRing = new(DT_BaseBeam, [
		SendPropInt(FIELD.OF(nameof(StartEntity)), 13, PropFlags.Unsigned),
		SendPropInt(FIELD.OF(nameof(EndEntity)), 13, PropFlags.Unsigned),
	]);
	public static readonly new ServerClass ServerClass = new ServerClass("TEBeamRing", DT_TEBeamRing).WithManualClassID(StaticClassIndices.CTEBeamRing);

	public int StartEntity;
	public int EndEntity;
}
