using Source.Common;
using Source;
using Game.Shared;
using System.Numerics;
using Source.Common.Formats.Keyvalues;
namespace Game.Client;
using FIELD = FIELD<C_TEConcussiveExplosion>;
[NetworkName("CTEConcussiveExplosion")]
public class C_TEConcussiveExplosion : C_TEParticleSystem
{
	public static readonly RecvTable DT_TEConcussiveExplosion = new(DT_TEParticleSystem, [
		RecvPropVector(FIELD.OF(nameof(Normal))),
		RecvPropFloat(FIELD.OF(nameof(Scale))),
		RecvPropInt(FIELD.OF(nameof(Radius))),
		RecvPropInt(FIELD.OF(nameof(Magnitude))),
	]);
	public static readonly new ClientClass ClientClass = new ClientClass(DT_TEConcussiveExplosion);

	[NetworkName("m_vecNormal")]
	public Vector3 Normal;
	[NetworkName("m_flScale")]
	public float Scale;
	[NetworkName("m_nRadius")]
	public int Radius;
	[NetworkName("m_nMagnitude")]
	public int Magnitude;
}

public static partial class TempEnts
{
	public static void TE_ConcussiveExplosion(IRecipientFilter filter, float delay, KeyValues keyValues) {
		throw new NotImplementedException();
	}
}
