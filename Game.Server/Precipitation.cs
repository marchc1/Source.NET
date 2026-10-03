using Source.Common;
using Source;
using Game.Shared;
using System.Numerics;
namespace Game.Server;
using FIELD = FIELD<Precipitation>;
[LinkEntityToClass("func_precipitation")]
[NetworkName("CPrecipitation")]
public class Precipitation : BaseEntity
{
	public static readonly SendTable DT_Precipitation = new(DT_BaseEntity, [
		SendPropInt(FIELD.OF(nameof(PrecipType)), 4, PropFlags.Unsigned),
		SendPropString(FIELD.OF(nameof(ParticleNameClose))),
		SendPropString(FIELD.OF(nameof(ParticleNameInner))),
		SendPropString(FIELD.OF(nameof(ParticleNameOuter))),
		SendPropFloat(FIELD.OF(nameof(ParticleDist)), 0, PropFlags.NoScale),
	]);
	public static readonly new ServerClass ServerClass = new ServerClass(DT_Precipitation);

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
