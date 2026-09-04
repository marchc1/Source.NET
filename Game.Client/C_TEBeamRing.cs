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

public static partial class TempEnts
{
	public static void TE_BeamRing(IRecipientFilter filter, float delay, int start, int end, int modelIndex, int haloIndex, int startFrame, int frameRate, float life, float width, int spread, float amplitude, int r, int g, int b, int a, int speed, int flags = 0) {
		throw new NotImplementedException();
	}
}
