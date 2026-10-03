using Source.Common;
using Source;
using Game.Shared;
using System.Numerics;
namespace Game.Server;
using FIELD = FIELD<EnvStarfield>;
[LinkEntityToClass("env_starfield")]
[NetworkName("CEnvStarfield")]
public class EnvStarfield : BaseEntity
{
	public static readonly SendTable DT_EnvStarfield = new(DT_BaseEntity, [
		SendPropBool(FIELD.OF(nameof(On))),
		SendPropFloat(FIELD.OF(nameof(Density)), 0, PropFlags.NoScale),
	]);
	public static readonly new ServerClass ServerClass = new ServerClass(DT_EnvStarfield);

	[NetworkName("m_bOn")]
	public bool On;
	[NetworkName("m_flDensity")]
	public float Density;
}
