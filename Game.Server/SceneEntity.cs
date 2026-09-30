using Source.Common;
using Source;

namespace Game.Server;

using FIELD = FIELD<SceneEntity>;

[NetworkName("CSceneEntity")]
public partial class SceneEntity : BaseEntity
{
	public const int MAX_ACTORS_IN_SCENE = 16;

	[NetworkName("m_nSceneStringIndex")]
	[NetworkVar] public partial int SceneStringIndex { get; set; }
	[NetworkName("m_bIsPlayingBack")]
	[NetworkVar] public partial bool IsPlayingBack { get; set; }
	[NetworkName("m_bPaused")]
	[NetworkVar] public partial bool Paused { get; set; }
	[NetworkName("m_bMultiplayer")]
	[NetworkVar] public partial bool Multiplayer { get; set; }
	[NetworkName("m_flForceClientTime")]
	[NetworkVar] public partial float ForceClientTime { get; set; }
	[NetworkName("m_hActorList")]
	readonly List<EHANDLE> ActorList = [];

	public static readonly SendTable DT_SceneEntity = new([
		SendPropInt(NetworkVarFields.SceneStringIndex, 12, PropFlags.Unsigned),
		SendPropBool(NetworkVarFields.IsPlayingBack),
		SendPropBool(NetworkVarFields.Paused),
		SendPropBool(NetworkVarFields.Multiplayer),
		SendPropFloat(NetworkVarFields.ForceClientTime, 0, PropFlags.NoScale),
		SendPropList(FIELD.OF(nameof(ActorList)), MAX_ACTORS_IN_SCENE, SendPropEHandle()),
	]);
	public static new readonly ServerClass ServerClass = new ServerClass(DT_SceneEntity);
}
