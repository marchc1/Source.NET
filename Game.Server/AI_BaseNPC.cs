using Game.Shared;

using Source;
using Source.Common;
using Source.Common.Engine;
using Source.Common.Formats.BSP;

using System.Diagnostics;
using System.Numerics;

namespace Game.Server;

using FIELD = FIELD<AI_BaseNPC>;

public ref struct TriggerTraceEnum(ref Ray ray, in TakeDamageInfo info, in Vector3 dir, Mask mask) : IEntityEnumerator {
	Vector3 VecDir = dir;
	Mask ContentsMask = mask;
	ref Ray Ray = ref ray;
	TakeDamageInfo Info = info;

	public bool EnumEntity(IHandleEntity? handleEntity) {
		Trace tr = default;

		BaseEntity? ent = gEntList.GetBaseEntity(handleEntity!.GetRefEHandle());

		// Done to avoid hitting an entity that's both solid & a trigger.
		if (ent!.IsSolid())
			return true;

		enginetrace.ClipRayToEntity(in Ray, ContentsMask, handleEntity, ref tr);
		if (tr.Fraction < 1.0f) {
			ent.DispatchTraceAttack(Info, VecDir, ref tr);
			ApplyMultiDamage();
		}

		return true;
	}
}

public class AI_BaseNPC : BaseCombatCharacter
{
	public static readonly SendTable DT_AI_BaseNPC = new(DT_BaseCombatCharacter, [
		SendPropInt(FIELD.OF(nameof(LifeState)), 3, PropFlags.Unsigned),
		SendPropBool(FIELD.OF(nameof(PerformAvoidance))),
		SendPropBool(FIELD.OF(nameof(IsMoving))),
		SendPropBool(FIELD.OF(nameof(FadeCorpse))),
		SendPropInt(FIELD.OF(nameof(DeathPose)), 12),
		SendPropInt(FIELD.OF(nameof(DeathFrame)), 12),
		SendPropBool(FIELD.OF(nameof(ImportantRagdoll))),
	]);
	public static readonly new ServerClass ServerClass = new ServerClass("AI_BaseNPC", DT_AI_BaseNPC).WithManualClassID(StaticClassIndices.CAI_BaseNPC);

	public bool PerformAvoidance;
	public bool IsMoving;
	public bool FadeCorpse;
	public int DeathPose;
	public int DeathFrame;
	public bool SpeedModActive;
	public int SpeedModRadius;
	public int SpeedModSpeed;
	public bool ImportantRagdoll;
	public float TimePingEffect;
}
