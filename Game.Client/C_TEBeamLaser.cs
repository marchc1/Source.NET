using Source.Common;
using Source;

using Game.Shared;

namespace Game.Client;

using FIELD = FIELD<C_TEBeamLaser>;
public class C_TEBeamLaser : C_BaseBeam
{
	public static readonly RecvTable DT_TEBeamLaser = new(DT_BaseBeam, [
		RecvPropInt(FIELD.OF(nameof(StartEntity))),
		RecvPropInt(FIELD.OF(nameof(EndEntity))),
	]);
	public static readonly new ClientClass ClientClass = new ClientClass("TEBeamLaser", DT_TEBeamLaser).AsEvent<C_TEBeamLaser>().WithManualClassID(StaticClassIndices.CTEBeamLaser);

	public int StartEntity;
	public int EndEntity;

	public override void PostDataUpdate(DataUpdateType updateType) {
		beams.CreateBeamEnts(StartEntity, EndEntity, ModelIndex, HaloIndex, 0.0f, Life, Width, EndWidth, FadeLength, Amplitude, A, 0.1f * Speed, StartFrame, 0.1f * FrameRate, R, G, B, (int)TempEntType.BeamLaser);
	}
}
