using Game.Shared;
using Source.Common;
using Source;

namespace Game.Server;

using FIELD = FIELD<SceneEntity>;

[LinkEntityToClass("logic_choreographed_scene")]
[LinkEntityToClass("scripted_scene")]
[NetworkName("CSceneEntity")]
public class SceneEntity : BaseEntity
{
	public const int MAX_ACTORS_IN_SCENE = 16;

	[NetworkName("m_nSceneStringIndex")]
	public int SceneStringIndex;
	[NetworkName("m_bIsPlayingBack")]
	public bool IsPlayingBack;
	[NetworkName("m_bPaused")]
	public bool Paused;
	[NetworkName("m_bMultiplayer")]
	public bool Multiplayer;
	[NetworkName("m_flForceClientTime")]
	public float ForceClientTime;
	[NetworkName("m_hActorList")]
	readonly List<EHANDLE> ActorList = [];

	public static readonly SendTable DT_SceneEntity = new([
		SendPropInt(FIELD.OF(nameof(SceneStringIndex)), 12, PropFlags.Unsigned),
		SendPropBool(FIELD.OF(nameof(IsPlayingBack))),
		SendPropBool(FIELD.OF(nameof(Paused))),
		SendPropBool(FIELD.OF(nameof(Multiplayer))),
		SendPropFloat(FIELD.OF(nameof(ForceClientTime)), 0, PropFlags.NoScale),
		SendPropList(FIELD.OF(nameof(ActorList)), MAX_ACTORS_IN_SCENE, SendPropEHandle()),
	]);
	public static new readonly ServerClass ServerClass = new ServerClass(DT_SceneEntity);
}
