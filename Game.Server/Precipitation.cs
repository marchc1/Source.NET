using Source.Common;
using Source;
using Game.Shared;
using System.Numerics;
namespace Game.Server;
using FIELD = FIELD<Precipitation>;
public class Precipitation : BaseEntity
{
	public static readonly SendTable DT_Precipitation = new(DT_BaseEntity, [
		SendPropInt(FIELD.OF(nameof(PrecipType)), 4, PropFlags.Unsigned),
		SendPropString(FIELD.OF(nameof(ParticleNameClose))),
		SendPropString(FIELD.OF(nameof(ParticleNameInner))),
		SendPropString(FIELD.OF(nameof(ParticleNameOuter))),
		SendPropFloat(FIELD.OF(nameof(ParticleDist)), 0, PropFlags.NoScale),
	]);
	public static readonly new ServerClass ServerClass = new ServerClass("Precipitation", DT_Precipitation).WithManualClassID(StaticClassIndices.CPrecipitation);

	public int PrecipType;
	public InlineArray512<char> ParticleNameClose;
	public InlineArray512<char> ParticleNameInner;
	public InlineArray512<char> ParticleNameOuter;
	public float ParticleDist;
}
