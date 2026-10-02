using Source.Common;
using Source;
using Game.Shared;
using System.Numerics;
namespace Game.Server;
using FIELD = FIELD<EnvAmbientLight>;
[LinkEntityToClass("env_ambient_light")]
[NetworkName("CEnvAmbientLight")]
public class EnvAmbientLight : SpatialEntity
{
	public static readonly SendTable DT_EnvAmbientLight = new(DT_SpatialEntity, [
		SendPropVector(FIELD.OF(nameof(Color)), 0, PropFlags.NoScale),
	]);
	public static readonly new ServerClass ServerClass = new ServerClass(DT_EnvAmbientLight);

	[NetworkName("m_vecColor")]
	public Vector3 Color;
}
