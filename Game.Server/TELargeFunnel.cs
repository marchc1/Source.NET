using Source.Common;
using Source;
using Game.Shared;
using System.Numerics;
namespace Game.Server;
using FIELD = FIELD<TELargeFunnel>;
public class TELargeFunnel : TEParticleSystem
{
	public static readonly SendTable DT_TELargeFunnel = new(DT_TEParticleSystem, [
		SendPropInt(FIELD.OF(nameof(ModelIndex)), 14, 0),
		SendPropInt(FIELD.OF(nameof(Reversed)), 2, PropFlags.Unsigned),
	]);
	public static readonly new ServerClass ServerClass = new ServerClass("TELargeFunnel", DT_TELargeFunnel).WithManualClassID(StaticClassIndices.CTELargeFunnel);

	public int ModelIndex;
	public int Reversed;
}
