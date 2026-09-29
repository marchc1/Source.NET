using Source.Common;
using Source;
using Game.Shared;
using System.Numerics;
namespace Game.Client;
using FIELD = FIELD<C_TESparks>;
[NetworkName("CTESparks")]
public class C_TESparks : C_TEParticleSystem
{
	public static readonly RecvTable DT_TESparks = new(DT_TEParticleSystem, [
		RecvPropInt(FIELD.OF(nameof(Magnitude))),
		RecvPropInt(FIELD.OF(nameof(TrailLength))),
		RecvPropVector(FIELD.OF(nameof(Dir))),
	]);
	public static readonly new ClientClass ClientClass = new ClientClass(DT_TESparks);

	[NetworkName("m_nMagnitude")]
	public int Magnitude;
	[NetworkName("m_nTrailLength")]
	public int TrailLength;
	[NetworkName("m_vecDir")]
	public Vector3 Dir;
}

public static partial class TempEnts
{
	public static void TE_Sparks(IRecipientFilter filter, float delay, in Vector3 pos, int magnitude, int trailLength, in Vector3 dir) {
		throw new NotImplementedException();
	}
}
