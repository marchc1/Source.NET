using Game.Shared;

using Source;
using Source.Common;
using Source.Common.Engine;
using Source.Common.Formats.BSP;

using System.Diagnostics;
using System.Numerics;

namespace Game.Server;

using FIELD = FIELD<AI_BaseNPC>;

public ref struct TriggerTraceEnum(ref Ray ray, in TakeDamageInfo info, in Vector3 dir, Mask mask) : IEntityEnumerator
{
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

[NetworkName("CAI_BaseNPC")]
public partial class AI_BaseNPC : BaseCombatCharacter
{
	public static ReadOnlySpan<char> GetActivityName(Activity actID) {
		if (actID == Activity.ACT_INVALID)
			return "ACT_INVALID";

		string? name = ActivityList.NameForIndex(actID);

		if (name == null)
			Assert(false, "AI_BaseNPC.GetActivityName() returning NULL!");

		return name;
	}

	public static readonly SendTable DT_AI_BaseNPC = new(DT_BaseCombatCharacter, [
		SendPropInt(BaseEntity.NetworkVarFields.LifeState, 3, PropFlags.Unsigned),
		SendPropBool(NetworkVarFields.PerformAvoidance),
		SendPropBool(NetworkVarFields.IsMoving),
		SendPropBool(NetworkVarFields.FadeCorpse),
		SendPropInt(NetworkVarFields.DeathPose, 12),
		SendPropInt(NetworkVarFields.DeathFrame, 5),
		SendPropBool(NetworkVarFields.ImportantRagdoll),
	]);
	public static readonly new ServerClass ServerClass = new ServerClass(DT_AI_BaseNPC);

	[NetworkName("m_bPerformAvoidance")]
	[NetworkVar] public partial bool PerformAvoidance { get; set; }
	[NetworkName("m_bIsMoving")]
	[NetworkVar] public partial bool IsMoving { get; set; }
	[NetworkName("m_bFadeCorpse")]
	[NetworkVar] public partial bool FadeCorpse { get; set; }
	[NetworkName("m_iDeathPose")]
	[NetworkVar] public partial int DeathPose { get; set; }
	[NetworkName("m_iDeathFrame")]
	[NetworkVar] public partial int DeathFrame { get; set; }
	public bool SpeedModActive;
	public int SpeedModRadius;
	public int SpeedModSpeed;
	[NetworkName("m_bImportanRagdoll")]
	[NetworkVar] public partial bool ImportantRagdoll { get; set; }
	public float TimePingEffect;
}
