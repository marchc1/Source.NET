using Source.Common;
using Source;
using Game.Shared;
using System.Numerics;
namespace Game.Client;
using FIELD = FIELD<C_TEBeamRing>;
public class C_TEBeamRing : C_BaseBeam
{
	public static readonly RecvTable DT_TEBeamRing = new(DT_BaseBeam, [
		RecvPropInt(FIELD.OF(nameof(StartEntity))),
		RecvPropInt(FIELD.OF(nameof(EndEntity))),
	]);
	public static readonly new ClientClass ClientClass = new ClientClass("TEBeamRing", DT_TEBeamRing).WithManualClassID(StaticClassIndices.CTEBeamRing);

	public int StartEntity;
	public int EndEntity;
}
