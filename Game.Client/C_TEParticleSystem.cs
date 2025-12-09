using Source.Common;
using Source;
using Game.Shared;
using System.Numerics;
namespace Game.Client;

using FIELD = FIELD<C_TEParticleSystem>;
public class C_TEParticleSystem : C_BaseTempEntity
{
	public static readonly RecvTable DT_TEParticleSystem = new(DT_BaseTempEntity, [
		RecvPropFloat(FIELD.OF_VECTORELEM(nameof(Origin), 0)),
		RecvPropFloat(FIELD.OF_VECTORELEM(nameof(Origin), 1)),
		RecvPropFloat(FIELD.OF_VECTORELEM(nameof(Origin), 2)),
	]);
	public static readonly new ClientClass ClientClass = new ClientClass("TEParticleSystem", DT_TEParticleSystem).WithManualClassID(StaticClassIndices.CTEParticleSystem);

	public Vector3 Origin;
}
