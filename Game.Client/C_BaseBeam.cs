using Source.Common;
using Source;
using Game.Shared;
using System.Numerics;
namespace Game.Client;
using FIELD = FIELD<C_BaseBeam>;
[NetworkName("CTEBaseBeam")]
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
	public static readonly new ClientClass ClientClass = new ClientClass(DT_BaseBeam);

	[NetworkName("m_nModelIndex")]
	public int ModelIndex;
	[NetworkName("m_nHaloIndex")]
	public int HaloIndex;
	[NetworkName("m_nStartFrame")]
	public int StartFrame;
	[NetworkName("m_nFrameRate")]
	public int FrameRate;
	[NetworkName("m_fLife")]
	public float Life;
	[NetworkName("m_fWidth")]
	public float Width;
	[NetworkName("m_fEndWidth")]
	public float EndWidth;
	[NetworkName("m_nFadeLength")]
	public int FadeLength;
	[NetworkName("m_fAmplitude")]
	public float Amplitude;
	[NetworkName("m_nSpeed")]
	public int Speed;
	[NetworkName("r")]
	public int R;
	[NetworkName("g")]
	public int G;
	[NetworkName("b")]
	public int B;
	[NetworkName("a")]
	public int A;
	[NetworkName("m_nFlags")]
	public int Flags;
}
