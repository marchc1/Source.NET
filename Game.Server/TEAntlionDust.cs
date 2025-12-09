using Source.Common;
using Source;
using Game.Shared;
using System.Numerics;
namespace Game.Server;
using FIELD = FIELD<TEAntlionDust>;
public class TEAntlionDust : TEParticleSystem
{
	public static readonly SendTable DT_TEAntlionDust = new(DT_TEParticleSystem, [
		SendPropVector(FIELD.OF(nameof(Origin)), 0, PropFlags.NoScale),
		SendPropVector(FIELD.OF(nameof(Angles)), 0, PropFlags.NoScale),
		SendPropBool(FIELD.OF(nameof(BlockedSpawner))),
	]);
	public static readonly new ServerClass ServerClass = new ServerClass("TEAntlionDust", DT_TEAntlionDust).WithManualClassID(StaticClassIndices.CTEAntlionDust);

	public Vector3 Angles;
	public bool BlockedSpawner;
}
