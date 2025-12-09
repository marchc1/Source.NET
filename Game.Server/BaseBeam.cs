using Source.Common;
using Source;
using Game.Shared;
using System.Numerics;
namespace Game.Server;
using FIELD = FIELD<BaseBeam>;
public class BaseBeam : BaseTempEntity
{
	public static readonly SendTable DT_BaseBeam = new([
		SendPropInt(FIELD.OF(nameof(ModelIndex)), 14, 0),
		SendPropInt(FIELD.OF(nameof(HaloIndex)), 14, 0),
		SendPropInt(FIELD.OF(nameof(StartFrame)), 8, PropFlags.Unsigned),
		SendPropInt(FIELD.OF(nameof(FrameRate)), 8, PropFlags.Unsigned),
		SendPropFloat(FIELD.OF(nameof(Life)), 8, 0),
		SendPropFloat(FIELD.OF(nameof(Width)), 10, 0),
		SendPropFloat(FIELD.OF(nameof(EndWidth)), 10, 0),
		SendPropInt(FIELD.OF(nameof(FadeLength)), 8, PropFlags.Unsigned),
		SendPropFloat(FIELD.OF(nameof(Amplitude)), 8, 0),
		SendPropInt(FIELD.OF(nameof(Speed)), 8, PropFlags.Unsigned),
		SendPropInt(FIELD.OF(nameof(R)), 8, PropFlags.Unsigned),
		SendPropInt(FIELD.OF(nameof(G)), 8, PropFlags.Unsigned),
		SendPropInt(FIELD.OF(nameof(B)), 8, PropFlags.Unsigned),
		SendPropInt(FIELD.OF(nameof(A)), 8, PropFlags.Unsigned),
		SendPropInt(FIELD.OF(nameof(Flags)), 32, PropFlags.Unsigned),
	]);
	public static readonly new ServerClass ServerClass = new ServerClass("TEBaseBeam", DT_BaseBeam).WithManualClassID(StaticClassIndices.CTEBaseBeam);

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
