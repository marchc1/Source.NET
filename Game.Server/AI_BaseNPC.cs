using Source.Common;
using Source;

using Game.Shared;

namespace Game.Server;

using FIELD = FIELD<AI_BaseNPC>;

public class AI_BaseNPC : BaseCombatCharacter
{
	public static readonly SendTable DT_AI_BaseNPC = new(DT_BaseCombatCharacter, [
		SendPropInt(FIELD.OF(nameof(LifeState)), 3, PropFlags.Unsigned),
		SendPropBool(FIELD.OF(nameof(PerformAvoidance))),
		SendPropBool(FIELD.OF(nameof(IsMoving))),
		SendPropBool(FIELD.OF(nameof(FadeCorpse))),
		SendPropInt(FIELD.OF(nameof(DeathPose)), 12),
		SendPropInt(FIELD.OF(nameof(DeathFrame)), 12),
		SendPropBool(FIELD.OF(nameof(SpeedModActive))),
		SendPropInt(FIELD.OF(nameof(SpeedModRadius)), 32),
		SendPropInt(FIELD.OF(nameof(SpeedModSpeed)), 32),
		SendPropBool(FIELD.OF(nameof(ImportantRagdoll))),
		SendPropFloat(FIELD.OF(nameof(TimePingEffect)), 0, PropFlags.NoScale),
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
