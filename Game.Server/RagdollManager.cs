using Source.Common;
using Source;

namespace Game.Server;

using FIELD = FIELD<RagdollManager>;

[NetworkName("CRagdollManager")]
public partial class RagdollManager : BaseEntity
{
	[NetworkName("m_iCurrentMaxRagdollCount")]
	[NetworkVar] public partial int CurrentMaxRagdollCount { get; set; }

	public static readonly SendTable DT_RagdollManager = new([
		SendPropInt(NetworkVarFields.CurrentMaxRagdollCount, 6),
	]);
	public static new readonly ServerClass ServerClass = new ServerClass(DT_RagdollManager);
}
