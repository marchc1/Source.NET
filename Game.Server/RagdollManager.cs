using Source.Common;
using Source;

namespace Game.Server;

using FIELD = FIELD<RagdollManager>;

public class RagdollManager : BaseEntity
{
	public int CurrentMaxRagdollCount;

	public static readonly SendTable DT_RagdollManager = new([
		SendPropInt(FIELD.OF(nameof(CurrentMaxRagdollCount)), 6),
	]);
	public static new readonly ServerClass ServerClass = new ServerClass("RagdollManager", DT_RagdollManager).WithManualClassID(Shared.StaticClassIndices.CRagdollManager);
}
