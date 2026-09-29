using Source.Common;
using Source;

namespace Game.Client;

using FIELD = FIELD<C_SceneEntity>;

[NetworkName("CSceneEntity")]
public class C_SceneEntity : C_BaseEntity
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

	public static readonly RecvTable DT_SceneEntity = new([
		RecvPropInt(FIELD.OF(nameof(SceneStringIndex))),
		RecvPropBool(FIELD.OF(nameof(IsPlayingBack))),
		RecvPropBool(FIELD.OF(nameof(Paused))),
		RecvPropBool(FIELD.OF(nameof(Multiplayer))),
		// todo: RecvProxy_ForcedClientTime
		RecvPropFloat(FIELD.OF(nameof(ForceClientTime))),
		RecvPropList<EHANDLE>(FIELD.OF_LIST(nameof(ActorList), MAX_ACTORS_IN_SCENE), ResizeActorList, RecvPropEHandle()),
	]);
	public static new readonly ClientClass ClientClass = new ClientClass(DT_SceneEntity);

	static void ResizeActorList(object instance, object list, int len) {
		var vec = (List<EHANDLE>)list;
		while (vec.Count < len) vec.Add(default);
		while (vec.Count > len) vec.RemoveAt(vec.Count - 1);
	}
}
