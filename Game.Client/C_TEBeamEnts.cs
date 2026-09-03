using Source.Common;
using Source;

using Game.Shared;

namespace Game.Client;

using FIELD = FIELD<C_TEBeamEnts>;
public class C_TEBeamEnts : C_BaseBeam
{
	public static readonly RecvTable DT_TEBeamEnts = new(DT_BaseBeam, [
		RecvPropInt(FIELD.OF(nameof(StartEntity))),
		RecvPropInt(FIELD.OF(nameof(EndEntity))),
	]);
	public static readonly new ClientClass ClientClass = new ClientClass("TEBeamEnts", DT_TEBeamEnts).AsEvent<C_TEBeamEnts>().WithManualClassID(StaticClassIndices.CTEBeamEnts);

	public int StartEntity;
	public int EndEntity;

	public override void PostDataUpdate(DataUpdateType updateType) {
		beams.CreateBeamEnts(StartEntity, EndEntity, ModelIndex, HaloIndex, 0.0f, Life, Width, EndWidth, FadeLength, Amplitude, A, 0.1f * Speed, StartFrame, 0.1f * FrameRate, R, G, B);
	}
}
