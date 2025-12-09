using Source.Common;
using Source;
using Game.Shared;
using System.Numerics;
namespace Game.Client;

using FIELD = FIELD<C_TEAntlionDust>;
public class C_TEAntlionDust : C_TEParticleSystem
{
	public static readonly RecvTable DT_TEAntlionDust = new(DT_TEParticleSystem, [
		RecvPropVector(FIELD.OF(nameof(Origin))),
		RecvPropVector(FIELD.OF(nameof(Angles))),
		RecvPropBool(FIELD.OF(nameof(BlockedSpawner))),
	]);
	public static readonly new ClientClass ClientClass = new ClientClass("TEAntlionDust", DT_TEAntlionDust).WithManualClassID(StaticClassIndices.CTEAntlionDust);

	public Vector3 Angles;
	public bool BlockedSpawner;
}
