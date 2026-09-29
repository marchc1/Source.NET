using Source.Common;
using Source;
using Game.Shared;
using System.Numerics;
namespace Game.Server;
using FIELD = FIELD<TEAntlionDust>;
[NetworkName("CTEAntlionDust")]
public class TEAntlionDust(ReadOnlySpan<char> name) : TEParticleSystem(name)
{
	public static readonly SendTable DT_TEAntlionDust = new(DT_TEParticleSystem, [
		SendPropVector(FIELD.OF(nameof(Origin)), 0, PropFlags.NoScale),
		SendPropVector(FIELD.OF(nameof(Angles)), 0, PropFlags.NoScale),
		SendPropBool(FIELD.OF(nameof(BlockedSpawner))),
	]);
	public static readonly new ServerClass ServerClass = new ServerClass(DT_TEAntlionDust);

	[NetworkName("m_vecAngles")]
	public Vector3 Angles;
	[NetworkName("m_bBlockedSpawner")]
	public bool BlockedSpawner;
}
