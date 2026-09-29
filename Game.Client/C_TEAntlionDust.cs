using Source.Common;
using Source;
using Game.Shared;
using System.Numerics;
namespace Game.Client;

using FIELD = FIELD<C_TEAntlionDust>;
[NetworkName("CTEAntlionDust")]
public class C_TEAntlionDust : C_TEParticleSystem
{
	public static readonly RecvTable DT_TEAntlionDust = new(DT_TEParticleSystem, [
		RecvPropVector(FIELD.OF(nameof(Origin))),
		RecvPropVector(FIELD.OF(nameof(Angles))),
		RecvPropBool(FIELD.OF(nameof(BlockedSpawner))),
	]);
	public static readonly new ClientClass ClientClass = new ClientClass(DT_TEAntlionDust);

	[NetworkName("m_vecAngles")]
	public Vector3 Angles;
	[NetworkName("m_bBlockedSpawner")]
	public bool BlockedSpawner;
}
