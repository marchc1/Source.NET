using Source.Common;
using Source;
using Game.Shared;
using System.Numerics;
namespace Game.Client;
using FIELD = FIELD<C_BaseBeam>;
public class C_BaseBeam : C_BaseTempEntity
{
	public static readonly RecvTable DT_BaseBeam = new([
		RecvPropInt(FIELD.OF(nameof(ModelIndex))),
		RecvPropInt(FIELD.OF(nameof(HaloIndex))),
		RecvPropInt(FIELD.OF(nameof(StartFrame))),
		RecvPropInt(FIELD.OF(nameof(FrameRate))),
		RecvPropFloat(FIELD.OF(nameof(Life))),
		RecvPropFloat(FIELD.OF(nameof(Width))),
		RecvPropFloat(FIELD.OF(nameof(EndWidth))),
		RecvPropInt(FIELD.OF(nameof(FadeLength))),
		RecvPropFloat(FIELD.OF(nameof(Amplitude))),
		RecvPropInt(FIELD.OF(nameof(Speed))),
		RecvPropInt(FIELD.OF(nameof(R))),
		RecvPropInt(FIELD.OF(nameof(G))),
		RecvPropInt(FIELD.OF(nameof(B))),
		RecvPropInt(FIELD.OF(nameof(A))),
		RecvPropInt(FIELD.OF(nameof(Flags))),
	]);
	public static readonly new ClientClass ClientClass = new ClientClass("TEBaseBeam", DT_BaseBeam).WithManualClassID(StaticClassIndices.CTEBaseBeam);

	public int ModelIndex;
	public int HaloIndex;
	public int StartFrame;
	public int FrameRate;
	public float Life;
	public float Width;
	public float EndWidth;
	public int FadeLength;
	public float Amplitude;
	public int Speed;
	public int R;
	public int G;
	public int B;
	public int A;
	public int Flags;
}
