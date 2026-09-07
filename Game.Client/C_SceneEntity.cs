using Source.Common;
using Source;

namespace Game.Client;

using FIELD = FIELD<C_SceneEntity>;

public class C_SceneEntity : C_BaseEntity
{
	public const int MAX_ACTORS_IN_SCENE = 16;

	public int SceneStringIndex;
	public bool IsPlayingBack;
	public bool Paused;
	public bool Multiplayer;
	public float ForceClientTime;
	readonly List<EHANDLE> ActorList = [];

	public static readonly RecvTable DT_SceneEntity = new([
		RecvPropInt(FIELD.OF(nameof(SceneStringIndex))),
		RecvPropBool(FIELD.OF(nameof(IsPlayingBack))),
		RecvPropBool(FIELD.OF(nameof(Paused))),
		RecvPropBool(FIELD.OF(nameof(Multiplayer))),
		RecvPropFloat(FIELD.OF(nameof(ForceClientTime))),
		RecvPropList<EHANDLE>(FIELD.OF_LIST(nameof(ActorList), MAX_ACTORS_IN_SCENE), ResizeActorList, RecvPropEHandle()),
	]);
	public static new readonly ClientClass ClientClass = new ClientClass("SceneEntity", DT_SceneEntity).WithManualClassID(Shared.StaticClassIndices.CSceneEntity);

	static void ResizeActorList(object instance, object list, int len) {
		var vec = (List<EHANDLE>)list;
		while (vec.Count < len) vec.Add(default);
		while (vec.Count > len) vec.RemoveAt(vec.Count - 1);
	}
}
