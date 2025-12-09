using Source.Common;
using Source;
using Game.Shared;
using System.Numerics;
namespace Game.Client;
using FIELD = FIELD<C_TEBloodStream>;
public class C_TEBloodStream : C_TEParticleSystem
{
	public static readonly RecvTable DT_TEBloodStream = new(DT_TEParticleSystem, [
		RecvPropVector(FIELD.OF(nameof(Direction))),
		RecvPropInt(FIELD.OF(nameof(R))),
		RecvPropInt(FIELD.OF(nameof(G))),
		RecvPropInt(FIELD.OF(nameof(B))),
		RecvPropInt(FIELD.OF(nameof(A))),
		RecvPropInt(FIELD.OF(nameof(Amount))),
	]);
	public static readonly new ClientClass ClientClass = new ClientClass("TEBloodStream", DT_TEBloodStream).WithManualClassID(StaticClassIndices.CTEBloodStream);

	public Vector3 Direction;
	public int R;
	public int G;
	public int B;
	public int A;
	public int Amount;
}
