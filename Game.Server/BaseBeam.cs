using Source.Common;
using Source;
using Game.Shared;
using System.Numerics;
namespace Game.Server;
using FIELD = FIELD<BaseBeam>;
[NetworkName("CTEBaseBeam")]
public class BaseBeam(ReadOnlySpan<char> name) : BaseTempEntity(name)
{
	public static readonly SendTable DT_BaseBeam = new([
		SendPropInt(FIELD.OF(nameof(ModelIndex)), 14, 0),
		SendPropInt(FIELD.OF(nameof(HaloIndex)), 14, 0),
		SendPropInt(FIELD.OF(nameof(StartFrame)), 8, PropFlags.Unsigned),
		SendPropInt(FIELD.OF(nameof(FrameRate)), 8, PropFlags.Unsigned),
		SendPropFloat(FIELD.OF(nameof(Life)), 8, 0, 0.0f, 25.6f),
		SendPropFloat(FIELD.OF(nameof(Width)), 10, 0, 0.0f, 128.0f),
		SendPropFloat(FIELD.OF(nameof(EndWidth)), 10, 0, 0.0f, 128.0f),
		SendPropInt(FIELD.OF(nameof(FadeLength)), 8, PropFlags.Unsigned),
		SendPropFloat(FIELD.OF(nameof(Amplitude)), 8, 0, 0.0f, 64.0f),
		SendPropInt(FIELD.OF(nameof(Speed)), 8, PropFlags.Unsigned),
		SendPropInt(FIELD.OF(nameof(R)), 8, PropFlags.Unsigned),
		SendPropInt(FIELD.OF(nameof(G)), 8, PropFlags.Unsigned),
		SendPropInt(FIELD.OF(nameof(B)), 8, PropFlags.Unsigned),
		SendPropInt(FIELD.OF(nameof(A)), 8, PropFlags.Unsigned),
		SendPropInt(FIELD.OF(nameof(Flags)), 20, PropFlags.Unsigned),
	]);
	public static readonly new ServerClass ServerClass = new ServerClass(DT_BaseBeam);

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
