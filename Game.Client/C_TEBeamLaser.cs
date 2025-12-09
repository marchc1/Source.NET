using Source.Common;
using Source;
using Game.Shared;
using System.Numerics;
namespace Game.Client;
using FIELD = FIELD<C_TEBeamLaser>;
public class C_TEBeamLaser : C_BaseBeam
{
	public static readonly RecvTable DT_TEBeamLaser = new(DT_BaseBeam, [
		RecvPropInt(FIELD.OF(nameof(StartEntity))),
		RecvPropInt(FIELD.OF(nameof(EndEntity))),
	]);
	public static readonly new ClientClass ClientClass = new ClientClass("TEBeamLaser", DT_TEBeamLaser).WithManualClassID(StaticClassIndices.CTEBeamLaser);

	public int StartEntity;
	public int EndEntity;
}
