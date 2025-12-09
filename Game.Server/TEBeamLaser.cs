using Source.Common;
using Source;
using Game.Shared;
using System.Numerics;
namespace Game.Server;
using FIELD = FIELD<TEBeamLaser>;
public class TEBeamLaser : BaseBeam
{
	public static readonly SendTable DT_TEBeamLaser = new(DT_BaseBeam, [
		SendPropInt(FIELD.OF(nameof(StartEntity)), 24, PropFlags.Unsigned),
		SendPropInt(FIELD.OF(nameof(EndEntity)), 24, PropFlags.Unsigned),
	]);
	public static readonly new ServerClass ServerClass = new ServerClass("TEBeamLaser", DT_TEBeamLaser).WithManualClassID(StaticClassIndices.CTEBeamLaser);

	public int StartEntity;
	public int EndEntity;
}
