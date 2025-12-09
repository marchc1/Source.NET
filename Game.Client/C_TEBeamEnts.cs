using Source.Common;
using Source;
using Game.Shared;
using System.Numerics;
namespace Game.Client;
using FIELD = FIELD<C_TEBeamEnts>;
public class C_TEBeamEnts : C_BaseBeam
{
	public static readonly RecvTable DT_TEBeamEnts = new(DT_BaseBeam, [
		RecvPropInt(FIELD.OF(nameof(StartEntity))),
		RecvPropInt(FIELD.OF(nameof(EndEntity))),
	]);
	public static readonly new ClientClass ClientClass = new ClientClass("TEBeamEnts", DT_TEBeamEnts).WithManualClassID(StaticClassIndices.CTEBeamEnts);

	public int StartEntity;
	public int EndEntity;
}
