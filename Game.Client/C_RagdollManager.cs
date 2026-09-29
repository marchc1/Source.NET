using Source.Common;
using Source;

namespace Game.Client;

using FIELD = FIELD<C_RagdollManager>;

[NetworkName("CRagdollManager")]
public class C_RagdollManager : C_BaseEntity
{
	[NetworkName("m_iCurrentMaxRagdollCount")]
	public int CurrentMaxRagdollCount;

	public static readonly RecvTable DT_RagdollManager = new([
		RecvPropInt(FIELD.OF(nameof(CurrentMaxRagdollCount))),
	]);
	public static new readonly ClientClass ClientClass = new ClientClass(DT_RagdollManager);
}
