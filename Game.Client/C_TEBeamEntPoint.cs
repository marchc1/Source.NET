using Source.Common;
using Source;

using Game.Shared;

using System.Numerics;
namespace Game.Client;

using FIELD = FIELD<C_TEBeamEntPoint>;
[NetworkName("CTEBeamEntPoint")]
public class C_TEBeamEntPoint : C_BaseBeam
{
	public static readonly RecvTable DT_TEBeamEntPoint = new(DT_BaseBeam, [
		RecvPropInt(FIELD.OF(nameof(StartEntity))),
		RecvPropInt(FIELD.OF(nameof(EndEntity))),
		RecvPropVector(FIELD.OF(nameof(StartPoint))),
		RecvPropVector(FIELD.OF(nameof(EndPoint))),
	]);
	public static readonly new ClientClass ClientClass = new ClientClass(DT_TEBeamEntPoint).AsEvent<C_TEBeamEntPoint>();

	[NetworkName("m_nStartEntity")]
	public int StartEntity;
	[NetworkName("m_nEndEntity")]
	public int EndEntity;
	[NetworkName("m_vecStartPoint")]
	public Vector3 StartPoint;
	[NetworkName("m_vecEndPoint")]
	public Vector3 EndPoint;

	public override void PostDataUpdate(DataUpdateType updateType) {
		beams.CreateBeamEntPoint(StartEntity, in StartPoint, EndEntity, in EndPoint, ModelIndex, HaloIndex, 0.0f, Life, Width, EndWidth, FadeLength, Amplitude, A, 0.1f * Speed, StartFrame, 0.1f * FrameRate, R, G, B);
	}
}

public static partial class TempEnts
{
	public static void TE_BeamEntPoint(IRecipientFilter filter, float delay, int startEntity, in Vector3 start, int endEntity, in Vector3 end, int modelIndex, int haloIndex, int startFrame, int frameRate, float life, float width, float endWidth, int fadeLength, float amplitude, int r, int g, int b, int a, int speed) {
		throw new NotImplementedException();
	}
}
