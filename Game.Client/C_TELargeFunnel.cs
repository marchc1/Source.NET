using Source.Common;
using Source;
using Game.Shared;
using System.Numerics;
namespace Game.Client;
using FIELD = FIELD<C_TELargeFunnel>;
[NetworkName("CTELargeFunnel")]
public class C_TELargeFunnel : C_TEParticleSystem
{
	public static readonly RecvTable DT_TELargeFunnel = new(DT_TEParticleSystem, [
		RecvPropInt(FIELD.OF(nameof(Reversed))),
	]);
	public static readonly new ClientClass ClientClass = new ClientClass(DT_TELargeFunnel);

	public int ModelIndex;
	[NetworkName("m_nReversed")]
	public int Reversed;
}

public static partial class TempEnts
{
	public static void TE_LargeFunnel(IRecipientFilter filter, float delay, in Vector3 pos, int modelIndex, int reversed) {
		throw new NotImplementedException();
	}
}
