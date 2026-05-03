using Game.Shared;

using System.Numerics;

using Source.Common;
using Source.Common.Commands;
using Source.Common.Physics;
using Source;

namespace Game.Server.HL2;

using FIELD = Source.FIELD<HL2_Player>;

public class HL2_Player : BasePlayer
{
	public static readonly SendTable DT_HL2_Player = new(DT_BasePlayer, [
		SendPropDataTable(nameof(HL2Local), HL2PlayerLocalData.DT_HL2Local, SendProxy_SendLocalDataTable),
		SendPropBool(FIELD.OF(nameof(_IsSprinting)))
	]);
	public static new readonly ServerClass ServerClass = new ServerClass("HL2_Player", DT_HL2_Player)
															.WithManualClassID(StaticClassIndices.CHL2_Player);

	public readonly HL2PlayerLocalData HL2Local = new();

#if HL2MP
	const int HL2_WALK_SPEED = 150;
	const int HL2_NORM_SPEED = 190;
	const int HL2_SPRINT_SPEED = 320;
#else
#endif

	TimeUnit_t IdleTime;
	TimeUnit_t MoveTime;
	TimeUnit_t LastDamageTime;
	TimeUnit_t TargetFindTime;

	bool SprintEnabled;

	public bool _IsSprinting;
	bool _IsWalking;

	public HL2_Player() => SprintEnabled = true;

	public override void Precache() {
		base.Precache();

		PrecacheScriptSound("HL2Player.SprintNoPower");
		PrecacheScriptSound("HL2Player.SprintStart");
		PrecacheScriptSound("HL2Player.UseDeny");
		PrecacheScriptSound("HL2Player.FlashLightOn");
		PrecacheScriptSound("HL2Player.FlashLightOff");
		PrecacheScriptSound("HL2Player.PickupWeapon");
		PrecacheScriptSound("HL2Player.TrainUse");
		PrecacheScriptSound("HL2Player.Use");
		PrecacheScriptSound("HL2Player.BurnPain");
	}

	void CheckSuitZoom() { }

	void EquipSuit(bool playEffects) { }

	void RemoveSuit() { }

	void HandleSpeedChanges() { }

	void HandleArmorReduction() { }

	void PreThink() { }

	void PostThink() { }

	void StartAdmireGlovesAnimation() { }

	void HandleAdmireGlovesAnimation() { }

	public override void Activate() {
		base.Activate();
		InitSprinting();

#if HL2_EPISODIC
		if (GetActiveWeapon() != null) {
			TimeUnit_t remain = GetActiveWeapon()!.NextPrimaryAttack - gpGlobals.CurTime;

			if (remain < HL2PLAYER_RELOADGAME_ATTACK_DELAY)
				GetActiveWeapon().m_flNextPrimaryAttack = gpGlobals.curtime + HL2PLAYER_RELOADGAME_ATTACK_DELAY;

			remain = GetActiveWeapon()!.NextSecondaryAttack - gpGlobals.CurTime;

			if (remain < HL2PLAYER_RELOADGAME_ATTACK_DELAY)
				GetActiveWeapon()!.NextSecondaryAttack = gpGlobals.CurTime + HL2PLAYER_RELOADGAME_ATTACK_DELAY;
		}

#endif

		// GetPlayerProxy();
	}

	// Class_T Classify() { }

	// bool HandleInteraction(int interactionType, void* data, BaseCombatCharacter sourceEnt) { }

	public override void PlayerRunCommand(UserCmd ucmd, IMoveHelper moveHelper) {
		// if (PhysicsFlags & PFLAG_ONBARNACLE) { // TODO
		// 	ucmd.forwardmove = 0;
		// 	ucmd.sidemove = 0;
		// 	ucmd.upmove = 0;
		// 	ucmd.buttons &= ~IN_USE;
		// }

		if (IsDead())
			ucmd.Buttons &= ~InButtons.Use;

		if ((ucmd.ForwardMove != 0) || (ucmd.SideMove != 0) || (ucmd.UpMove != 0)) {
			IdleTime -= TICK_INTERVAL * 2.0f;

			if (IdleTime < 0.0f)
				IdleTime = 0.0f;

			MoveTime += TICK_INTERVAL;

			if (MoveTime > 4.0f)
				MoveTime = 4.0f;
		}
		else {
			IdleTime += TICK_INTERVAL;

			if (IdleTime > 4.0f)
				IdleTime = 4.0f;

			MoveTime -= TICK_INTERVAL * 2.0f;

			if (MoveTime < 0.0f)
				MoveTime = 0.0f;
		}

		base.PlayerRunCommand(ucmd, moveHelper);
	}

	public override void Spawn() {
#if HL2MP
#if !PORTAL
		SetModel("models/player.mdl");
#endif
#endif

		base.Spawn();

		// if (!IsSuitEquipped())
		StartWalking();

		// SuitPower_SetCharge(100);

		// Local.HideHUD |= HideHudBits.Chat;

		// PlayerAISquad = g_AI_SquadManager.FindCreateSquad(AllocPooledString(PLAYER_SQUADNAME));

		InitSprinting();

#if HL2_EPISODIC
		HL2Local.FlashBattery = 100.0f;
#endif

		// GetPlayerProxy();

		// SetFlashlightPowerDrainScale(1.0f);
	}

	void UpdateLocatorPosition(Vector3 position) { }

	void InitSprinting() => StopSprinting();

	bool CanSprint() {
		throw new NotImplementedException();
	}

	void StartAutoSprint() { }

	void StartSprinting() { }

	void StopSprinting() { }

	void EnableSprint(bool enable) {
		if (!enable && IsSprinting())
			StopSprinting();

		SprintEnabled = enable;
	}

	void StartWalking() {
		SetMaxSpeed(HL2_WALK_SPEED);
		_IsWalking = true;
	}

	void StopWalking() {
		SetMaxSpeed(HL2_NORM_SPEED);
		_IsWalking = false;
	}

	bool IsWalking() => _IsWalking;

	// float GetIdleTime() => IdleTime - MoveTime;

	// float GetMoveTime() => MoveTime - IdleTime;

	// float GetLastDamageTime() => LastDamageTime;

	bool IsDucking() => (GetFlags() & EntityFlags.Ducking) != 0;

	bool CanZoom(BaseEntity requester) {
		throw new NotImplementedException();
	}

	void ToggleZoom() { }

	void StartZooming() { }

	void StopZooming() { }

	bool IsZooming() {
		throw new NotImplementedException();
	}

	void InitVCollision(Vector3 absOrigin, Vector3 absVelocity) { }

	// bool CommanderFindGoal(commandgoal_t goal) { }

	AI_BaseNPC GetSquadCommandRepresentative() {
		throw new NotImplementedException();
	}

	int GetNumSquadCommandables() {
		throw new NotImplementedException();
	}

	int GetNumSquadCommandableMedics() {
		throw new NotImplementedException();
	}

	void CommanderUpdate() { }

	// bool CommanderExecuteOne(AI_BaseNPC npc, commandgoal_t goal, AI_BaseNPC Allies, int numAllies) { }

	// void CommanderExecute(CommanderCommand_t command) { }

	void CommanderMode() { }

	void CheatImpulseCommands(int impulse) { }

	void SetupVisibility(BaseEntity viewEntity, byte pvs, int pvssize) { }

	void SuitPower_Update() { }

	void SuitPower_Initialize() { }

	bool SuitPower_Drain(float power) {
		throw new NotImplementedException();
	}

	void SuitPower_Charge(float power) { }

	// bool SuitPower_IsDeviceActive(SuitPowerDevice device) { }

	// bool SuitPower_AddDevice(SuitPowerDevice device) { }

	// bool SuitPower_RemoveDevice(SuitPowerDevice device) { }

	bool SuitPower_ShouldRecharge() {
		throw new NotImplementedException();
	}

	bool ApplyBattery(float powerMultiplier) {
		throw new NotImplementedException();
	}

	int FlashlightIsOn() {
		throw new NotImplementedException();
	}

	void FlashlightTurnOn() { }

	void FlashlightTurnOff() { }

	bool IsIlluminatedByFlashlight(BaseEntity entity, float returnDot) {
		throw new NotImplementedException();
	}

	void CheckFlashlight() { }

	public override void SetPlayerUnderwater(bool state) {
		// if (state)
		// 	SuitPower_AddDevice(SuitDeviceBreather);
		// else
		// 	SuitPower_RemoveDevice(SuitDeviceBreather);

		base.SetPlayerUnderwater(state);
	}

	// bool PassesDamageFilter(TakeDamageInfo info) { }

	void SetFlashlightEnabled(bool state) { }

	// void InputDisableFlashlight(inputdata_t inputdata ) { }

	// void InputEnableFlashlight(inputdata_t inputdata ) { }

	// void InputIgnoreFallDamage(inputdata_t inputdata ) { }

	// void InputIgnoreFallDamageWithoutReset(inputdata_t inputdata ) { }

	// void OnSquadMemberKilled(inputdata_t data ) { }

	void NotifyFriendsOfDamage(BaseEntity attackerEntity) { }

	// int OnTakeDamage(TakeDamageInfo info) { }

	// int OnTakeDamage_Alive(TakeDamageInfo info) { }

	// void OnDamagedByExplosion(TakeDamageInfo info) { }

	bool ShouldShootMissTarget(BaseCombatCharacter attacker) {
		throw new NotImplementedException();
	}

	void CombineBallSocketed(PropCombineBall combineBall) { }

	// void Event_KilledOther(BaseEntity pVictim, TakeDamageInfo info) { }

	// void Event_Killed(TakeDamageInfo info) { }

	void NotifyScriptsOfDeath() { }

	// void GetAutoaimVector(autoaim_params_t params ) { }

	bool ShouldKeepLockedAutoaimTarget(EHANDLE lockedTarget) {
		throw new NotImplementedException();
	}

	int GiveAmmo(int count, int ammoIndex, bool suppressSound) {
		throw new NotImplementedException();
	}

	bool Weapon_CanUse(BaseCombatWeapon weapon) {
		throw new NotImplementedException();
	}

	void Weapon_Equip(BaseCombatWeapon weapon) { }

	bool BumpWeapon(BaseCombatWeapon weapon) {
		throw new NotImplementedException();
	}

	bool ClientCommand(in TokenizedCommand args) {
		throw new NotImplementedException();
	}

	void PlayerUse() { }

	void UpdateWeaponPosture() { }

	bool Weapon_Lower() {
		throw new NotImplementedException();
	}

	bool Weapon_Ready() {
		throw new NotImplementedException();
	}

	bool Weapon_CanSwitchTo(BaseCombatWeapon weapon) {
		throw new NotImplementedException();
	}

	void PickupObject(BaseEntity Object, bool limitMassAndSize) { }

	bool IsHoldingEntity(BaseEntity ent) {
		throw new NotImplementedException();
	}

	float GetHeldObjectMass(IPhysicsObject heldObject) {
		throw new NotImplementedException();
	}

	void ForceDropOfCarriedPhysObjects(BaseEntity onlyIfHoldingThis) { }

	// void InputForceDropPhysObjects(inputdata_t data ) { }

	void UpdateClientData() { }

	void OnRestore() { }

	Vector3 EyeDirection2D() {
		throw new NotImplementedException();
	}

	Vector3 EyeDirection3D() {
		throw new NotImplementedException();
	}

	bool Weapon_Switch(BaseCombatWeapon weapon, int viewmodelindex) {
		throw new NotImplementedException();
	}

	// WeaponProficiency_t CalcWeaponProficiency(BaseCombatWeapon weapon) { }

	bool TestHitboxes(Ray ray, uint fContentsMask, Trace tr) {
		throw new NotImplementedException();
	}

	void DrawDebugGeometryOverlays() { }

	void ExitLadder() { }

	SurfaceData GetLadderSurface(Vector3 origin) {
		throw new NotImplementedException();
	}

	void PlayUseDenySound() { }

	void ItemPostFrame() { }

	void StartWaterDeathSounds() { }

	void StopWaterDeathSounds() { }

	void MissedAR2AltFire() { }

	void DisplayLadderHudHint() { }

	void StopLoopingSounds() { }

	// void ModifyOrAppendPlayerCriteria(AI_CriteriaSet set ) { }

	//  impactdamagetable_t GetPhysicsImpactDamageTable() { }

	void Splash() { }

	// LogicPlayerProxy GetPlayerProxy() { }

	// void FirePlayerProxyOutput(ReadOnlySpan<char> outputName, variant_t variant, BaseEntity activator, BaseEntity caller) { }

	public bool IsSprinting() => _IsSprinting;
}
