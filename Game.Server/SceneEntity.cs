using Source.Common;
using Source;

namespace Game.Server;

using FIELD = FIELD<SceneEntity>;

public class SceneEntity : BaseEntity
{
	public const int MAX_ACTORS_IN_SCENE = 16;

	public int SceneStringIndex;
	public bool IsPlayingBack;
	public bool Paused;
	public bool Multiplayer;
	public float ForceClientTime;
	readonly List<EHANDLE> ActorList = [];

	public static readonly SendTable DT_SceneEntity = new([
		SendPropInt(FIELD.OF(nameof(SceneStringIndex)), 12, PropFlags.Unsigned),
		SendPropBool(FIELD.OF(nameof(IsPlayingBack))),
		SendPropBool(FIELD.OF(nameof(Paused))),
		SendPropBool(FIELD.OF(nameof(Multiplayer))),
		SendPropFloat(FIELD.OF(nameof(ForceClientTime)), 0, PropFlags.NoScale),
		SendPropList(FIELD.OF(nameof(ActorList)), MAX_ACTORS_IN_SCENE, SendPropEHandle()),
	]);
	public static new readonly ServerClass ServerClass = new ServerClass("SceneEntity", DT_SceneEntity).WithManualClassID(Shared.StaticClassIndices.CSceneEntity);
}
