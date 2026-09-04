using Source.Common;
using Source;
using Game.Shared;
using System.Numerics;
namespace Game.Client;
using FIELD = FIELD<C_TEBeamRingPoint>;
public class C_TEBeamRingPoint : C_BaseBeam
{
	public static readonly RecvTable DT_TEBeamRingPoint = new(DT_BaseBeam, [
		RecvPropVector(FIELD.OF(nameof(Center))),
		RecvPropFloat(FIELD.OF(nameof(LStartRadius))),
		RecvPropFloat(FIELD.OF(nameof(LEndRadius))),
	]);
	public static readonly new ClientClass ClientClass = new ClientClass("TEBeamRingPoint", DT_TEBeamRingPoint).WithManualClassID(StaticClassIndices.CTEBeamRingPoint);

	public Vector3 Center;
	public float LStartRadius;
	public float LEndRadius;
}

public static partial class TempEnts
{
	public static void TE_BeamRingPoint(IRecipientFilter filter, float delay, in Vector3 center, float startRadius, float endRadius, int modelIndex, int haloIndex, int startFrame, int frameRate, float life, float width, int spread, float amplitude, int r, int g, int b, int a, int speed, int flags = 0) {
		throw new NotImplementedException();
	}
}
