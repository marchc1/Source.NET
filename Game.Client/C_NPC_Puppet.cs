using Source.Common;
using Source;
using Game.Shared;
using System.Numerics;
namespace Game.Client;
using FIELD = FIELD<C_NPC_Puppet>;
public class C_NPC_Puppet : C_AI_BaseNPC
{
	public static readonly RecvTable DT_NPC_Puppet = new(DT_AI_BaseNPC, [
		RecvPropEHandle(FIELD.OF(nameof(AnimationTarget))),
		RecvPropInt(FIELD.OF(nameof(TargetAttachment))),
	]);
	public static readonly new ClientClass ClientClass = new ClientClass("NPC_Puppet", DT_NPC_Puppet).WithManualClassID(StaticClassIndices.CNPC_Puppet);

	public readonly EHANDLE AnimationTarget = new();
	public int TargetAttachment;
}
