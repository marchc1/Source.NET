using Source.Common;
using Source;
using Game.Shared;
using System.Numerics;
namespace Game.Server;
using FIELD = FIELD<EntityDissolve>;
[NetworkName("CEntityDissolve")]
public partial class EntityDissolve : BaseEntity
{
	public static readonly SendTable DT_EntityDissolve = new(DT_BaseEntity, [
		SendPropFloat(NetworkVarFields.StartTime, 0, PropFlags.NoScale),
		SendPropFloat(NetworkVarFields.FadeInStart, 0, PropFlags.NoScale),
		SendPropFloat(NetworkVarFields.FadeInLength, 0, PropFlags.NoScale),
		SendPropFloat(NetworkVarFields.FadeOutModelStart, 0, PropFlags.NoScale),
		SendPropFloat(NetworkVarFields.FadeOutModelLength, 0, PropFlags.NoScale),
		SendPropFloat(NetworkVarFields.FadeOutStart, 0, PropFlags.NoScale),
		SendPropFloat(NetworkVarFields.FadeOutLength, 0, PropFlags.NoScale),
		SendPropInt(NetworkVarFields.DissolveType, 3, PropFlags.Unsigned),
		SendPropVector(NetworkVarFields.DissolverOrigin, 0, PropFlags.NoScale),
		SendPropInt(NetworkVarFields.Magnitude, 8, PropFlags.Unsigned),
	]);
	public static readonly new ServerClass ServerClass = new ServerClass(DT_EntityDissolve);

	[NetworkName("m_flStartTime")]
	[NetworkVar] public partial float StartTime { get; set; }
	[NetworkName("m_flFadeInStart")]
	[NetworkVar] public partial float FadeInStart { get; set; }
	[NetworkName("m_flFadeInLength")]
	[NetworkVar] public partial float FadeInLength { get; set; }
	[NetworkName("m_flFadeOutModelStart")]
	[NetworkVar] public partial float FadeOutModelStart { get; set; }
	[NetworkName("m_flFadeOutModelLength")]
	[NetworkVar] public partial float FadeOutModelLength { get; set; }
	[NetworkName("m_flFadeOutStart")]
	[NetworkVar] public partial float FadeOutStart { get; set; }
	[NetworkName("m_flFadeOutLength")]
	[NetworkVar] public partial float FadeOutLength { get; set; }
	[NetworkName("m_nDissolveType")]
	[NetworkVar] public partial int DissolveType { get; set; }
	[NetworkName("m_vDissolverOrigin")]
	[NetworkVar] public partial Vector3 DissolverOrigin { get; set; }
	[NetworkName("m_nMagnitude")]
	[NetworkVar] public partial int Magnitude { get; set; }
}
