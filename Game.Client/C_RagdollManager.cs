using Source.Common;
using Source;

namespace Game.Client;

using FIELD = FIELD<C_RagdollManager>;

public class C_RagdollManager : C_BaseEntity
{
	public int CurrentMaxRagdollCount;

	public static readonly RecvTable DT_RagdollManager = new([
		RecvPropInt(FIELD.OF(nameof(CurrentMaxRagdollCount))),
	]);
	public static new readonly ClientClass ClientClass = new ClientClass("RagdollManager", DT_RagdollManager).WithManualClassID(Shared.StaticClassIndices.CRagdollManager);
}
