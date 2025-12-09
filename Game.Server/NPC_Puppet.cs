using Source.Common;
using Source;
using Game.Shared;
using System.Numerics;
namespace Game.Server;
using FIELD = FIELD<NPC_Puppet>;
public class NPC_Puppet : AI_BaseNPC
{
	public static readonly SendTable DT_NPC_Puppet = new(DT_AI_BaseNPC, [
		SendPropEHandle(FIELD.OF(nameof(AnimationTarget))),
		SendPropInt(FIELD.OF(nameof(TargetAttachment)), 32, 0),
	]);
	public static readonly new ServerClass ServerClass = new ServerClass("NPC_Puppet", DT_NPC_Puppet).WithManualClassID(StaticClassIndices.CNPC_Puppet);

	public readonly EHANDLE AnimationTarget = new();
	public int TargetAttachment;
}
