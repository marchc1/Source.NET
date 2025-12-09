using Source.Common;
using Source;
using Game.Shared;
using System.Numerics;
namespace Game.Server;

using FIELD = FIELD<TEParticleSystem>;
public class TEParticleSystem : BaseTempEntity
{
	public static readonly SendTable DT_TEParticleSystem = new(DT_BaseTempEntity, [
		SendPropFloat(FIELD.OF_VECTORELEM(nameof(Origin), 0), 0, PropFlags.Coord | PropFlags.NoScale | PropFlags.IsAVectorElem),
		SendPropFloat(FIELD.OF_VECTORELEM(nameof(Origin), 1), 0, PropFlags.Coord | PropFlags.NoScale | PropFlags.IsAVectorElem),
		SendPropFloat(FIELD.OF_VECTORELEM(nameof(Origin), 2), 0, PropFlags.Coord | PropFlags.NoScale | PropFlags.IsAVectorElem),
	]);
	public static readonly new ServerClass ServerClass = new ServerClass("TEParticleSystem", DT_TEParticleSystem).WithManualClassID(StaticClassIndices.CTEParticleSystem);

	public Vector3 Origin;
}
