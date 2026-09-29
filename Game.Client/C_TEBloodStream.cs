using Source.Common;
using Source;
using Game.Shared;
using System.Numerics;
namespace Game.Client;
using FIELD = FIELD<C_TEBloodStream>;
[NetworkName("CTEBloodStream")]
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
	public static readonly new ClientClass ClientClass = new ClientClass(DT_TEBloodStream);

	[NetworkName("m_vecDirection")]
	public Vector3 Direction;
	[NetworkName("r")]
	public int R;
	[NetworkName("g")]
	public int G;
	[NetworkName("b")]
	public int B;
	[NetworkName("a")]
	public int A;
	[NetworkName("m_nAmount")]
	public int Amount;
}

public static partial class TempEnts
{
	public static void TE_BloodStream(IRecipientFilter filter, float delay, in Vector3 org, in Vector3 dir, int r, int g, int b, int a, int amount) {
		throw new NotImplementedException();
	}
}
