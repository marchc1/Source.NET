using Source.Common;
using Source;
using Game.Shared;
using System.Numerics;
namespace Game.Server;
using FIELD = FIELD<NPC_Puppet>;
[NetworkName("CNPC_Puppet")]
public partial class NPC_Puppet : AI_BaseNPC
{
	public static readonly SendTable DT_NPC_Puppet = new(DT_AI_BaseNPC, [
		SendPropEHandle(NPC_Puppet.NetworkVarFields.AnimationTarget),
		SendPropInt(NetworkVarFields.TargetAttachment, 12, 0),
	]);
	public static readonly new ServerClass ServerClass = new ServerClass(DT_NPC_Puppet);

	[NetworkName("m_hAnimationTarget")]
	[NetworkVar] public partial NetworkHandle<BaseEntity> AnimationTarget { get; }
	[NetworkName("m_nTargetAttachment")]
	[NetworkVar] public partial int TargetAttachment { get; set; }
}
