using Source.Common;
using Source;

using Game.Shared;

using System.Numerics;
namespace Game.Client;

using FIELD = FIELD<C_TEBeamPoints>;
public class C_TEBeamPoints : C_BaseBeam
{
	public static readonly RecvTable DT_TEBeamPoints = new(DT_BaseBeam, [
		RecvPropVector(FIELD.OF(nameof(StartPoint))),
		RecvPropVector(FIELD.OF(nameof(EndPoint))),
	]);
	public static readonly new ClientClass ClientClass = new ClientClass("TEBeamPoints", DT_TEBeamPoints).AsEvent<C_TEBeamPoints>().WithManualClassID(StaticClassIndices.CTEBeamPoints);

	public Vector3 StartPoint;
	public Vector3 EndPoint;

	public override void PostDataUpdate(DataUpdateType updateType) {
		beams.CreateBeamPoints(ref StartPoint, ref EndPoint, ModelIndex, HaloIndex, 0.0f, Life, Width, EndWidth, FadeLength, Amplitude, A, 0.1f * Speed, StartFrame, 0.1f * FrameRate, R, G, B);
	}
}

public static partial class TempEnts
{
	public static void TE_BeamPoints(IRecipientFilter filter, float delay, in Vector3 start, in Vector3 end, int modelIndex, int haloIndex, int startFrame, int frameRate, float life, float width, float endWidth, int fadeLength, float amplitude, int r, int g, int b, int a, int speed) {
		throw new NotImplementedException();
	}
}
