using Game.Server.HL2;
using Game.Shared;

using Source;
using Source.Common;
using Source.Common.Engine;
using Source.Common.Commands;
using Source.Common.Mathematics;

using System.Numerics;

namespace Game.Server.HL2MP;

using FIELD = FIELD<HL2MP_Player>;
using FIELD_RD = FIELD<HL2MPRagdoll>;

[LinkEntityToClass("player")]
public partial class HL2MP_Player : HL2_Player
{
	public static readonly SendTable DT_HL2MPLocalPlayerExclusive = new([
		SendPropVector(FIELD.OF(nameof(Origin)), 0, PropFlags.NoScale|PropFlags.ChangesOften, 0.0f, Constants.HIGH_DEFAULT),

		SendPropFloat(FIELD.OF_VECTORELEM(nameof(AngEyeAngles), 0), 11, PropFlags.ChangesOften | PropFlags.RoundDown, 0, 360f ),
		SendPropAngle(FIELD.OF_VECTORELEM(nameof(AngEyeAngles), 1), 11, PropFlags.ChangesOften | PropFlags.RoundDown, 0, 360f ),
	]); public static readonly ServerClass SC_HL2MPLocalPlayerExclusive = new ServerClass("HL2MPLocalPlayerExclusive", DT_HL2MPLocalPlayerExclusive);

	public static readonly SendTable DT_HL2MPNonLocalPlayerExclusive = new([
		SendPropVector(FIELD.OF(nameof(Origin)), 0, PropFlags.CoordMPLowPrecision|PropFlags.ChangesOften, 0.0f, Constants.HIGH_DEFAULT),

		SendPropFloat(FIELD.OF_VECTORELEM(nameof(AngEyeAngles), 0), 11, PropFlags.ChangesOften | PropFlags.RoundDown, 0, 360f),
		SendPropAngle(FIELD.OF_VECTORELEM(nameof(AngEyeAngles), 1), 11, PropFlags.ChangesOften | PropFlags.RoundDown, 0, 360f),

	]); public static readonly ServerClass SC_HL2MPNonLocalPlayerExclusive = new ServerClass("HL2MPNonLocalPlayerExclusive", DT_HL2MPNonLocalPlayerExclusive);

	public static readonly SendTable DT_HL2MP_Player = new(DT_HL2_Player, [
		SendPropExclude(nameof(DT_BaseAnimating), nameof(PoseParameter)),
		SendPropExclude(nameof(DT_BaseAnimating), nameof(PlaybackRate)),
		SendPropExclude(nameof(DT_BaseAnimating), nameof(Sequence)),
		SendPropExclude(nameof(DT_BaseEntity), nameof(Rotation)),
		SendPropExclude(nameof(DT_BaseAnimatingOverlay), "overlay_vars"),

		SendPropExclude(nameof(DT_BaseEntity), nameof(Origin)),
		SendPropExclude(nameof(DT_ServerAnimationData), nameof(Cycle)),
		SendPropExclude(nameof(DT_AnimTimeMustBeFirst), nameof(AnimTime)),
		SendPropExclude(nameof(DT_BaseFlex), nameof(FlexWeight)),
		SendPropExclude(nameof(DT_BaseFlex), nameof(BlinkToggle)),
		SendPropExclude(nameof(DT_BaseFlex), nameof(ViewTarget)),

		SendPropDataTable("hl2mplocaldata", DT_HL2MPLocalPlayerExclusive, SendProxy_SendLocalDataTable ),
		SendPropDataTable("hl2mpnonlocaldata", DT_HL2MPNonLocalPlayerExclusive, SendProxy_SendNonLocalDataTable ),

		SendPropEHandle(FIELD.OF(nameof(Ragdoll))),
		SendPropInt(FIELD.OF(nameof(SpawnInterpCounter)), 4),
		SendPropBool(FIELD.OF(nameof(IsWalking))),

		SendPropExclude(nameof(DT_BaseAnimating), nameof(PoseParameter)),
		SendPropExclude(nameof(DT_BaseFlex), nameof(ViewTarget)),
	]);
	public static new readonly ServerClass ServerClass = new ServerClass("HL2MP_Player", DT_HL2MP_Player)
															.WithManualClassID(StaticClassIndices.CHL2MP_Player);

	public QAngle AngEyeAngles;
	public EHANDLE Ragdoll = new();
	public int SpawnInterpCounter;
	public int PlayerSoundType;
	public bool IsWalking;

	public readonly PlayerAnimState PlayerAnimState;

	TimeUnit_t NextModelChangeTime;
	TimeUnit_t NextTeamChangeTime;
	TimeUnit_t SlamProtectTime;

	public HL2MP_Player() {
		PlayerAnimState = new(this);
		AngEyeAngles.Init();

		// base.ChangeTeam(0);
	}

	public override void UpdateOnRemove() {
		if (Ragdoll.Get() != null) {
			Util.RemoveImmediate(Ragdoll.Get());
			Ragdoll.Set(null);
		}

		base.UpdateOnRemove();
	}

	public override void Precache() {
		base.Precache();

		// todo
	}

	void GiveAllItems() { }

	void GiveDefaultItems() { }

	void PickDefaultSpawnTeam() { }

	public override void Spawn() {
		NextModelChangeTime = 0;
		NextTeamChangeTime = 0;

		PickDefaultSpawnTeam();

		base.Spawn();

		if (!IsObserver()) {
			pl.DeadFlag = false;
			// RemoveSolidFlags(SolidFlags.NotSolid);
			RemoveEffects(EntityEffects.NoDraw);
			GiveDefaultItems();
		}

		SetNumAnimOverlays(3);
		ResetAnimation();

		RenderFX = (byte)Source.RenderMode.Normal;

		Local.HideHUD = false;

		AddFlag(EntityFlags.OnGround);

		// ImpactEnergyScale todo

		// if (HL2MPRules().IsIntermission()) {} else {
		RemoveFlag(EntityFlags.Frozen);

		SpawnInterpCounter = (SpawnInterpCounter + 1) % 8;

		Local.Ducked = false;

		SetPlayerUnderwater(false);

		// Ready = true;
	}

	void PickupObject(BaseEntity obj, bool limitMassAndSize) {
		throw new NotImplementedException();
	}

	bool ValidatePlayerModel(ReadOnlySpan<char> model) {
		throw new NotImplementedException();
	}

	void SetPlayerTeamModel() { }

	void SetPlayerModel() { }

	void SetupPlayerSoundsByModel(char modelName) { }

	void ResetAnimation() {
		if (IsAlive()) {
			SetSequence(-1);
			// SetActivity(Activity.ACT_INVALID);

			if (GetAbsVelocity().X == 0 && GetAbsVelocity().Y == 0)
				SetAnimation(PlayerAnim.Idle);
			else if ((GetAbsVelocity().X != 0 || GetAbsVelocity().Y != 0) && (GetFlags() & EntityFlags.OnGround) != 0)
				SetAnimation(PlayerAnim.Walk);
			else if (GetWaterLevel() > Shared.WaterLevel.Feet)
				SetAnimation(PlayerAnim.Walk);
		}
	}

	public override bool Weapon_Switch(BaseCombatWeapon? weapon, int viewmodelindex = 0) {
		throw new NotImplementedException();
	}

	public override void PreThink() {
		QAngle oldAngles = GetLocalAngles();
		QAngle tempAngles = EyeAngles();

		if (tempAngles[PITCH] > 180)
			tempAngles[PITCH] -= 360;

		SetLocalAngles(tempAngles);

		base.PreThink();
		State_PreThink();

		// TotalBulletForce = vec3_origin;
		SetLocalAngles(oldAngles);
	}

	public override void PostThink() {
		base.PostThink();

		if ((GetFlags() & EntityFlags.Ducking) != 0) {
			// collision bounds todo
		}

		PlayerAnimState.Update();

		AngEyeAngles = EyeAngles();

		QAngle angles = GetLocalAngles();
		angles[PITCH] = 0;
		SetLocalAngles(angles);
	}

	void PlayerDeathThink() {
		// if (!IsObserver())
		// 	base.PlayerDeathThink();
	}

	void FireBullets(FireBulletsInfo info) { }

	void NoteWeaponFired() { }

	bool WantsLagCompensationOnEntity(BasePlayer pPlayer, UserCmd pCmd, MaxEdictsBitVec entityTransmitBits) {
		throw new NotImplementedException();
	}

	static Activity TranslateTeamActivity(Activity actToTranslate) {
		// if (ModelType == TEAM_COMBINE)
		// 	return actToTranslate; // todo

		if (actToTranslate == Activity.ACT_RUN)
			return Activity.ACT_RUN_AIM_AGITATED;

		if (actToTranslate == Activity.ACT_IDLE)
			return Activity.ACT_IDLE_AIM_AGITATED;

		if (actToTranslate == Activity.ACT_WALK)
			return Activity.ACT_WALK_AIM_AGITATED;

		return actToTranslate;
	}

	public override void SetAnimation(PlayerAnim playerAnim) {
		int animDesired;
		float speed = GetAbsVelocity().Length();

		if ((GetFlags() & (EntityFlags.Frozen | EntityFlags.AtControls)) != 0) {
			speed = 0;
			playerAnim = PlayerAnim.Idle;
		}

		Activity idealActivity = Activity.ACT_HL2MP_RUN;
		if (playerAnim == PlayerAnim.Jump)
			idealActivity = Activity.ACT_HL2MP_JUMP;
		else if (playerAnim == PlayerAnim.Die) {
			if (LifeState == 0)
				return;
		}
		else if (playerAnim == PlayerAnim.Attack1) {
			if (GetActivity() == Activity.ACT_HOVER ||
					GetActivity() == Activity.ACT_SWIM ||
					GetActivity() == Activity.ACT_HOP ||
					GetActivity() == Activity.ACT_LEAP ||
					GetActivity() == Activity.ACT_DIESIMPLE) {
				idealActivity = GetActivity();
			}
			else
				idealActivity = Activity.ACT_HL2MP_GESTURE_RANGE_ATTACK;
		}
		else if (playerAnim == PlayerAnim.Reload) {
			idealActivity = Activity.ACT_HL2MP_GESTURE_RELOAD;
		}
		else if (playerAnim == PlayerAnim.Idle || playerAnim == PlayerAnim.Walk) {
			if ((GetFlags() & EntityFlags.OnGround) == 0 && GetActivity() == Activity.ACT_HL2MP_JUMP) // Still jumping
				idealActivity = GetActivity();
			else {
				if ((GetFlags() & EntityFlags.Ducking) != 0) {
					if (speed > 0)
						idealActivity = Activity.ACT_HL2MP_WALK_CROUCH;
					else
						idealActivity = Activity.ACT_HL2MP_IDLE_CROUCH;
				}
				else {
					if (speed > 0)
						idealActivity = Activity.ACT_HL2MP_RUN;
					else
						idealActivity = Activity.ACT_HL2MP_IDLE;
				}
			}

			idealActivity = TranslateTeamActivity(idealActivity);
		}

		if (idealActivity == Activity.ACT_HL2MP_GESTURE_RANGE_ATTACK) {
			// RestartGesture(Weapon_TranslateActivity(idealActivity));
			// Weapon_SetActivity(Weapon_TranslateActivity(Activity.ACT_RANGE_ATTACK1), 0);
			return;
		}
		else if (idealActivity == Activity.ACT_HL2MP_GESTURE_RELOAD) {
			// RestartGesture(Weapon_TranslateActivity(idealActivity));
			return;
		}
		else {
			// SetActivity(idealActivity);

			animDesired = 0;//SelectWeightedSequence(Weapon_TranslateActivity(idealActivity));

			if (animDesired == -1) {
				animDesired = SelectWeightedSequence(idealActivity);

				if (animDesired == -1)
					animDesired = 0;
			}

			if (GetSequence() == animDesired)
				return;

			PlaybackRate = 1.0;
			ResetSequence(animDesired);
			SetCycle(0);
			return;
		}

		if (GetSequence() == animDesired)
			return;

		ResetSequence(animDesired);
		SetCycle(0);
	}

	bool BumpWeapon(BaseCombatWeapon weapon) {
		throw new NotImplementedException();
	}

	void ChangeTeam(int team) { }

	bool HandleCommand_JoinTeam(int team) {
		throw new NotImplementedException();
	}

	bool ClientCommand(in TokenizedCommand args) {
		throw new NotImplementedException();
	}

	void CheatImpulseCommands(int impulse) { }

	bool ShouldRunRateLimitedCommand(in TokenizedCommand args) {
		throw new NotImplementedException();
	}

	void CreateViewModel(int index = 0) {
		Assert(index >= 0 && index < MAX_VIEWMODELS);

		if (GetViewModel(index) != null)
			return;

		PredictedViewModel? vm = (PredictedViewModel?)CreateEntityByName("predicted_viewmodel");
		if (vm != null) {
			vm.SetAbsOrigin(GetAbsOrigin());
			// vm.SetOwner(this);
			// vm.SetIndex(index);
			Util.DispatchSpawn(vm);
			vm.FollowEntity(this, false);
			// VieweModel.Set(index, vm);
		}
	}

	bool BecomeRagdollOnClient(Vector3 force) {
		throw new NotImplementedException();
	}

	void CreateRagdollEntity() { }

	int FlashlightIsOn() {
		throw new NotImplementedException();
	}

	void FlashlightTurnOn() { }

	void FlashlightTurnOff() { }

	void Weapon_Drop(BaseCombatWeapon weapon, Vector3 target, Vector3 velocity) { }

	void DetonateTripmines() { }

	// void Event_Killed(TakeDamageInfo info) {
	// 	throw new NotImplementedException();
	// }

	// int OnTakeDamage(TakeDamageInfo inputInfo) {
	// 	throw new NotImplementedException();
	// }

	// void DeathSound(TakeDamageInfo info) {
	// 	throw new NotImplementedException();
	// }

	public override BaseEntity EntSelectSpawnPoint() {
		BaseEntity? spot = null;
		BaseEntity? lastSpawnPoint = g_LastSpawn;
		Edict player = Edict();
		ReadOnlySpan<char> spawnpointName = "info_player_deathmatch";

		if (false /*HL2MPRules().IsTeamplay() == true*/) {
			// if (GetTeamNumber() == TEAM_COMBINE) {
			// 	spawnpointName = "info_player_combine";
			// 	lastSpawnPoint = LastCombineSpawn;
			// }
			// else if (GetTeamNumber() == TEAM_REBELS) {
			// 	spawnpointName = "info_player_rebel";
			// 	lastSpawnPoint = LastRebelSpawn;
			// }

			if (gEntList.FindEntityByClassname(null, spawnpointName) == null) {
				spawnpointName = "info_player_deathmatch";
				lastSpawnPoint = g_LastSpawn;
			}
		}

		spot = lastSpawnPoint;

		for (int i = random.RandomInt(1, 5); i > 0; i--)
			spot = gEntList.FindEntityByClassname(spot, spawnpointName);
		if (spot == null)
			spot = gEntList.FindEntityByClassname(spot, spawnpointName);

		BaseEntity? firstSpot = spot;

		do {
			if (spot != null) {
				// if (GameRules.IsSpawnPointValid(spot, this)) {
				// 	if (spot.GetLocalOrigin() == vec3_origin) {
				// 		spot = gEntList.FindEntityByClassname(spot, spawnpointName);
				// 		continue;
				// 	}

				goto ReturnSpot;
				// }
			}
			spot = gEntList.FindEntityByClassname(spot, spawnpointName);
		} while (spot != firstSpot);

		if (spot != null) {
			BaseEntity? ent = null;
			// for (EntitySphereQuery sphere(spot.GetAbsOrigin(), 128); (ent = sphere.GetCurrentEntity()) != null; sphere.NextEntity()) {
			// 	if (ent.IsPlayer() && !(ent.edict() == player))
			// 		ent.TakeDamage(CTakeDamageInfo(GetContainingEntity(INDEXENT(0)), GetContainingEntity(INDEXENT(0)), 300, DMG_GENERIC));
			// }
			goto ReturnSpot;
		}

		if (spot == null) {
			spot = gEntList.FindEntityByClassname(spot, "info_player_start");

			if (spot != null)
				goto ReturnSpot;
		}

	ReturnSpot:

		// if (HL2MPRules().IsTeamplay() == true) {
		// 	if (GetTeamNumber() == TEAM_COMBINE)
		// 		LastCombineSpawn = spot;
		// 	else if (GetTeamNumber() == TEAM_REBELS)
		// 		LastRebelSpawn = spot;
		// }

		g_LastSpawn = spot;

		SlamProtectTime = gpGlobals.CurTime + 0.5;

		return spot;
	}

	void Reset() { }

	bool IsReady() {
		throw new NotImplementedException();
	}

	void SetReady(bool ready) { }

	void CheckChatText(ReadOnlySpan<char> text) { }

	// void State_Transition(HL2MPPlayerState newState) {
	// 	throw new NotImplementedException();
	// }

	// void State_Enter(HL2MPPlayerState newState) {
	// 	throw new NotImplementedException();
	// }

	void State_Leave() { }

	void State_PreThink() {
		// pfnPreThink todo
	}

	// CHL2MPPlayerStateInfo State_LookupInfo(HL2MPPlayerState state) {
	// 	throw new NotImplementedException();
	// }

	bool StartObserverMode(int mode) {
		throw new NotImplementedException();
	}

	void StopObserverMode() { }

	void State_Enter_OBSERVER_MODE() { }

	void State_PreThink_OBSERVER_MODE() { }

	void State_Enter_ACTIVE() { }

	void State_PreThink_ACTIVE() { }

	bool CanHearAndReadChatFrom(BasePlayer player) => player != null;
}

public class HL2MPRagdoll : BaseAnimatingOverlay
{
	public static readonly SendTable DT_HL2MPRagdoll = new([
		SendPropVector(FIELD_RD.OF(nameof(RagdollOrigin)), 0, PropFlags.Coord),
		SendPropEHandle(FIELD_RD.OF(nameof(Player))),
		SendPropInt(FIELD_RD.OF(nameof(ModelIndex)), 14),
		SendPropInt(FIELD_RD.OF(nameof(ForceBone)), 8),
		SendPropVector(FIELD_RD.OF(nameof(Force)), 0, PropFlags.NoScale),
		SendPropVector(FIELD_RD.OF(nameof(RagdollVelocity)), 0, PropFlags.NoScale)
	]);
	public static readonly new ServerClass ServerClass = new ServerClass("HL2MPRagdoll", DT_HL2MPRagdoll).WithManualClassID(StaticClassIndices.CHL2MPRagdoll);

	public Vector3 RagdollOrigin;
	public EHANDLE Player = new();
	public Vector3 RagdollVelocity;
}
