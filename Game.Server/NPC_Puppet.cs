using Source.Common;
using Source;
using Game.Shared;
using System.Numerics;
namespace Game.Server;
using FIELD = FIELD<NPC_Puppet>;
[LinkEntityToClass("npc_puppet")]
[NetworkName("CNPC_Puppet")]
public class NPC_Puppet : AI_BaseNPC
{
	public static readonly SendTable DT_NPC_Puppet = new(DT_AI_BaseNPC, [
		SendPropEHandle(FIELD.OF(nameof(AnimationTarget))),
		SendPropInt(FIELD.OF(nameof(TargetAttachment)), 12, 0),
	]);
	public static readonly new ServerClass ServerClass = new ServerClass(DT_NPC_Puppet);

	[NetworkName("m_hAnimationTarget")]
	public EHANDLE AnimationTarget = new();
	[NetworkName("m_nTargetAttachment")]
	public int TargetAttachment;
}
