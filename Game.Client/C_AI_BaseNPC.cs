using Source.Common;
using Source;

using Game.Shared;

namespace Game.Client;

using FIELD = FIELD<C_AI_BaseNPC>;

public class C_AI_BaseNPC : C_BaseCombatCharacter
{
	public static readonly RecvTable DT_AI_BaseNPC = new(DT_BaseCombatCharacter, [
		RecvPropInt(FIELD.OF(nameof(LifeState))),
		RecvPropBool(FIELD.OF(nameof(PerformAvoidance))),
		RecvPropBool(FIELD.OF(nameof(IsMoving))),
		RecvPropBool(FIELD.OF(nameof(FadeCorpse))),
		RecvPropInt(FIELD.OF(nameof(DeathPose))),
		RecvPropInt(FIELD.OF(nameof(DeathFrame))),
		RecvPropBool(FIELD.OF(nameof(SpeedModActive))),
		RecvPropInt(FIELD.OF(nameof(SpeedModRadius))),
		RecvPropInt(FIELD.OF(nameof(SpeedModSpeed))),
		RecvPropBool(FIELD.OF(nameof(ImportantRagdoll))),
		RecvPropFloat(FIELD.OF(nameof(TimePingEffect))),
	]);
	public static readonly new ClientClass ClientClass = new ClientClass("AI_BaseNPC", DT_AI_BaseNPC).WithManualClassID(StaticClassIndices.CAI_BaseNPC);

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
