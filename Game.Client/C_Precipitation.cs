using Source.Common;
using Source;
using Game.Shared;
using System.Numerics;
namespace Game.Client;
using FIELD = FIELD<C_Precipitation>;
public class C_Precipitation : C_BaseEntity
{
	public static readonly RecvTable DT_Precipitation = new(DT_BaseEntity, [
		RecvPropInt(FIELD.OF(nameof(PrecipType))),
		RecvPropString(FIELD.OF(nameof(ParticleNameClose))),
		RecvPropString(FIELD.OF(nameof(ParticleNameInner))),
		RecvPropString(FIELD.OF(nameof(ParticleNameOuter))),
		RecvPropFloat(FIELD.OF(nameof(ParticleDist))),
	]);
	public static readonly new ClientClass ClientClass = new ClientClass("Precipitation", DT_Precipitation).WithManualClassID(StaticClassIndices.CPrecipitation);

	public int PrecipType;
	public InlineArray512<char> ParticleNameClose;
	public InlineArray512<char> ParticleNameInner;
	public InlineArray512<char> ParticleNameOuter;
	public float ParticleDist;
}
