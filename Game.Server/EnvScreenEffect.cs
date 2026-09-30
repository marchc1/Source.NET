using Source.Common;
using Source;
using Game.Shared;
using System.Numerics;
namespace Game.Server;
using FIELD = FIELD<EnvScreenEffect>;
[NetworkName("CEnvScreenEffect")]
public partial class EnvScreenEffect : BaseEntity
{
	public static readonly SendTable DT_EnvScreenEffect = new(DT_BaseEntity, [
		SendPropFloat(NetworkVarFields.Duration, 0, PropFlags.NoScale),
		SendPropInt(NetworkVarFields.Type, 12, PropFlags.Unsigned),
	]);
	public static readonly new ServerClass ServerClass = new ServerClass(DT_EnvScreenEffect);

	[NetworkName("m_flDuration")]
	[NetworkVar] public partial float Duration { get; set; }
	[NetworkName("m_nType")]
	[NetworkVar] public partial int Type { get; set; }
}
