using Source.Common;
using Source;
using Game.Shared;
using System.Numerics;
namespace Game.Server;
using FIELD = FIELD<EnvStarfield>;
[NetworkName("CEnvStarfield")]
public partial class EnvStarfield : BaseEntity
{
	public static readonly SendTable DT_EnvStarfield = new(DT_BaseEntity, [
		SendPropBool(NetworkVarFields.On),
		SendPropFloat(NetworkVarFields.Density, 0, PropFlags.NoScale),
	]);
	public static readonly new ServerClass ServerClass = new ServerClass(DT_EnvStarfield);

	[NetworkName("m_bOn")]
	[NetworkVar] public partial bool On { get; set; }
	[NetworkName("m_flDensity")]
	[NetworkVar] public partial float Density { get; set; }
}
