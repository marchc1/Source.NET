using Source.Common;
using Source;
using Game.Shared;
using System.Numerics;
namespace Game.Client;
using FIELD = FIELD<C_TELargeFunnel>;
public class C_TELargeFunnel : C_TEParticleSystem
{
	public static readonly RecvTable DT_TELargeFunnel = new(DT_TEParticleSystem, [
		RecvPropInt(FIELD.OF(nameof(ModelIndex))),
		RecvPropInt(FIELD.OF(nameof(Reversed))),
	]);
	public static readonly new ClientClass ClientClass = new ClientClass("TELargeFunnel", DT_TELargeFunnel).WithManualClassID(StaticClassIndices.CTELargeFunnel);

	public int ModelIndex;
	public int Reversed;
}
