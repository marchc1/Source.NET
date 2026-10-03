using Game.Shared;
using Source.Common;
using Source;

namespace Game.Server;

using FIELD = FIELD<RagdollManager>;

[LinkEntityToClass("game_ragdoll_manager")]
[NetworkName("CRagdollManager")]
public class RagdollManager : BaseEntity
{
	[NetworkName("m_iCurrentMaxRagdollCount")]
	public int CurrentMaxRagdollCount;

	public static readonly SendTable DT_RagdollManager = new([
		SendPropInt(FIELD.OF(nameof(CurrentMaxRagdollCount)), 6),
	]);
	public static new readonly ServerClass ServerClass = new ServerClass(DT_RagdollManager);
}
