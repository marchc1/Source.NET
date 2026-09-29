using Source.Common;
using Source;
using Game.Shared;
using System.Numerics;
namespace Game.Client;
using FIELD = FIELD<C_TEBloodSprite>;
[NetworkName("CTEBloodSprite")]
public class C_TEBloodSprite
{
	public static readonly RecvTable DT_TEBloodSprite = new([
		RecvPropVector(FIELD.OF(nameof(Origin))),
		RecvPropVector(FIELD.OF(nameof(Direction))),
		RecvPropInt(FIELD.OF(nameof(R))),
		RecvPropInt(FIELD.OF(nameof(G))),
		RecvPropInt(FIELD.OF(nameof(B))),
		RecvPropInt(FIELD.OF(nameof(A))),
		RecvPropInt(FIELD.OF(nameof(Size))),
	]);
	public static readonly new ClientClass ClientClass = new ClientClass(DT_TEBloodSprite);

	[NetworkName("m_vecOrigin")]
	public Vector3 Origin;
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
	public int SprayModel;
	public int DropModel;
	[NetworkName("m_nSize")]
	public int Size;
}

public static partial class TempEnts
{
	public static void TE_BloodSprite(IRecipientFilter filter, float delay, in Vector3 org, in Vector3 dir, int r, int g, int b, int a, int size) {
		throw new NotImplementedException();
	}
}
