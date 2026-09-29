using Source.Common;
using Source;
using Game.Shared;
using System.Numerics;
namespace Game.Client;
using FIELD = FIELD<C_Precipitation>;
[NetworkName("CPrecipitation")]
public class C_Precipitation : C_BaseEntity
{
	public static readonly RecvTable DT_Precipitation = new(DT_BaseEntity, [
		RecvPropInt(FIELD.OF(nameof(PrecipType))),
		RecvPropString(FIELD.OF(nameof(ParticleNameClose))),
		RecvPropString(FIELD.OF(nameof(ParticleNameInner))),
		RecvPropString(FIELD.OF(nameof(ParticleNameOuter))),
		RecvPropFloat(FIELD.OF(nameof(ParticleDist))),
	]);
	public static readonly new ClientClass ClientClass = new ClientClass(DT_Precipitation);

	[NetworkName("m_nPrecipType")]
	public int PrecipType;
	[NetworkName("m_sParticleNameClose")]
	public InlineArray512<char> ParticleNameClose;
	[NetworkName("m_sParticleNameInner")]
	public InlineArray512<char> ParticleNameInner;
	[NetworkName("m_sParticleNameOuter")]
	public InlineArray512<char> ParticleNameOuter;
	[NetworkName("m_flParticleDist")]
	public float ParticleDist;
}
