using Game.Shared;

using Source;
using Source.Common;
using Source.Common.Bitbuffers;
using Source.Common.Client;
using Source.Common.Commands;
using Source.Common.Mathematics;
using Source.Common.Physics;
using Source.Engine;

using System;
using System.Numerics;
using System.Runtime.CompilerServices;

using FIELD = Source.FIELD<Game.Client.C_BasePlayer>;

namespace Game.Client;

public class C_CommandContext
{
	public bool NeedsProcessing;
	public UserCmd Cmd;
	public int CommandNumber;

	public static AnonymousSafeFieldPointer<UserCmd> SafeCmdPointer(C_CommandContext ctx) => new(ctx, FetchCmdRef);
	static ref UserCmd FetchCmdRef(object owner) => ref ((C_CommandContext)owner).Cmd;
}

public struct C_PredictionError
{
	public TimeUnit_t Time;
	public Vector3 Error;
}



[LinkEntityToClass("player")]
public partial class C_BasePlayer : C_BaseCombatCharacter, IGameEventListener2
{
	public static readonly RecvTable DT_PlayerState = new([
		RecvPropInt(FIELD.OF(nameof(DeadFlag)))
	]); public static readonly ClientClass CC_PlayerState = new("PlayerState", null, null, DT_PlayerState);

	public override bool IsPlayer() => true;
	public TimeUnit_t GetFinalPredictedTime() => FinalPredictedTick * TICK_INTERVAL;
	public bool IsLocalPlayer() => GetLocalPlayer() == this;
	public static bool ShouldDrawLocalPlayer() {
		return false; // todo
	}

	public InButtons AfButtonLast;
	public InButtons AfButtonPressed;
	public InButtons AfButtonReleased;
	public InButtons Buttons;
	public AnonymousSafeFieldPointer<UserCmd> CurrentCommand;
	public Vector3 WaterJumpVel;
	public float WaterJumpTime;
	public float SwimSoundTime;
	public Vector3 LadderNormal;
	public int Impulse;

	bool FiredWeapon;
	public bool HasFiredWeapon() => FiredWeapon;
	public void SetFiredWeapon(bool flag) => FiredWeapon = flag;
	public bool IsObserver() => GetObserverMode() != Shared.ObserverMode.None;

	public static readonly RecvTable DT_LocalPlayerExclusive = new([
		RecvPropDataTable(nameof(Local), FIELD.OF(nameof(Local)), PlayerLocalData.DT_Local, 0, DataTableRecvProxy_PointerDataTable),
		RecvPropFloat(FIELD.OF(nameof(Friction))),
		RecvPropArray3(FIELD.OF_ARRAY(nameof(Ammo)), RecvPropInt( FIELD.OF_ARRAYINDEX(nameof(Ammo)))),
		RecvPropInt(FIELD.OF(nameof(OnTarget))),
		RecvPropInt(FIELD.OF(nameof(TickBase))),
		RecvPropInt(FIELD.OF(nameof(NextThinkTick))),
		RecvPropEHandle(FIELD.OF(nameof(LastWeapon))),
		RecvPropEHandle(FIELD.OF(nameof(GroundEntity))),
		RecvPropVector(FIELD.OF(nameof(BaseVelocity))),
		RecvPropEHandle(FIELD.OF(nameof(ConstraintEntity))),
		RecvPropVector(FIELD.OF(nameof(ConstraintCenter))),
		RecvPropFloat(FIELD.OF(nameof(ConstraintRadius))),
		RecvPropFloat(FIELD.OF(nameof(ConstraintWidth))),
		RecvPropFloat(FIELD.OF(nameof(ConstraintSpeedFactor))),
		RecvPropFloat(FIELD.OF(nameof(DeathTime))),
		RecvPropFloat(FIELD.OF(nameof(LaggedMovementValue))),
		RecvPropEHandle(FIELD.OF(nameof(TonemapController))),
		RecvPropEHandle(FIELD.OF(nameof(ViewEntity))),
		RecvPropBool(FIELD.OF(nameof(DisableWorldClicking))),
	]);

	public virtual void ItemPreFrame() { }

	public virtual void UpdateClientData() {
		for (int i = 0; i < WeaponCount(); i++) {
			if (GetWeapon(i) != null)  // each item updates it's successors
				GetWeapon(i).UpdateClientData(this);
		}
	}
	public virtual void UpdateUnderwaterState() { }
	public virtual void UpdateFogController() { }
	public virtual void PreThink() {
		ItemPreFrame();

		UpdateClientData();

		UpdateUnderwaterState();

		UpdateFogController();

		if (LifeState >= (int)Source.LifeState.Dying)
			return;

		// If we're not on the ground, we're falling. Update our falling velocity.
		if (0 == (GetFlags() & EntityFlags.OnGround))
			Local.FallVelocity = -GetAbsVelocity().Z;
	}
	public C_BaseEntity? GetVehicleEntity() => Vehicle.Get();
	public bool IsInAVehicle() => Vehicle.Get() != null;
	public virtual void SetAnimation(PlayerAnim playerAnim) { } // todo

	public virtual Vector3 GetAutoaimVector(float scale){
		MathLib.AngleVectors(GetAbsAngles(), out Vector3 forward, out _, out _);
		return forward;
	}
	public virtual void SetSuitUpdate(ReadOnlySpan<char> name, int fgroup, int noRepeat) { }

	[MethodImpl(MethodImplOptions.AggressiveInlining)] public void SetSuitUpdate(ReadOnlySpan<char> name, bool fgroup, int noRepeat) => SetSuitUpdate(name, fgroup ? 1 : 0, noRepeat);
	[MethodImpl(MethodImplOptions.AggressiveInlining)] public void SetSuitUpdate(ReadOnlySpan<char> name, int fgroup, bool noRepeat) => SetSuitUpdate(name, fgroup, noRepeat ? 1 : 0);
	[MethodImpl(MethodImplOptions.AggressiveInlining)] public void SetSuitUpdate(ReadOnlySpan<char> name, bool fgroup, bool noRepeat) => SetSuitUpdate(name, fgroup ? 1 : 0, noRepeat ? 1 : 0);
	
	public virtual void PostThink() {
		if (IsAlive()) {
			// Need to do this on the client to avoid prediction errors
			if ((GetFlags() & EntityFlags.Ducking) != 0)
				SetCollisionBounds(VEC_DUCK_HULL_MIN, VEC_DUCK_HULL_MAX);
			else
				SetCollisionBounds(VEC_HULL_MIN, VEC_HULL_MAX);

			// if (!CommentaryModeShouldSwallowInput(this)) {
			// 	// do weapon stuff
			ItemPostFrame();
			// }

			if ((GetFlags() & EntityFlags.OnGround) != 0)
				Local.FallVelocity = 0;

			// Don't allow bogus sequence on player
			if (GetSequence() == -1)
				SetSequence(0);

			StudioFrameAdvance();
		}

		// Even if dead simulate entities
		SimulatePlayerSimulatedEntities();
	}

	InlineArrayMaxAmmoSlots<int> OldAmmo;
	public int StuckLast;
	public virtual bool IsOverridingViewmodel() => false;
	public virtual int DrawOverriddenViewmodel(C_BaseViewModel viewmodel, StudioFlags flags) => 0;
	public override void OnDataChanged(DataUpdateType updateType) {
		if (IsLocalPlayer())
			SetPredictionEligible(true);

		base.OnDataChanged(updateType);

		// Only care about this for local player
		if (IsLocalPlayer()) {
			// Reset engine areabits pointer (TODO)

			// Check for Ammo pickups.
			for (int i = 0; i < MAX_AMMO_TYPES; i++) {
				if (GetAmmoCount(i) > OldAmmo[i]) {
					// Don't add to ammo pickup if the ammo doesn't do it
					FileWeaponInfo? pWeaponData = gWR.GetWeaponFromAmmo(i);

					if (pWeaponData == null || (pWeaponData.Flags & WeaponFlags.NoAmmoPickups) == 0) {
						// We got more ammo for this ammo index. Add it to the ammo history
						HudHistoryResource? pHudHR = GET_HUDELEMENT<HudHistoryResource>();
						// if (pHudHR != null) 
						//pHudHR.AddToHistory(HISTSLOT_AMMO, i, abs(GetAmmoCount(i) - m_iOldAmmo[i]));

					}
				}
			}

			// Soundscape_Update(m_Local.m_audio);
			// ^^ todo

			// todo
			// if (OldFogController != Local.PlayerFog.Ctrl) 
			// 	FogControllerChanged(updateType == DataUpdateType.Created);
		}
	}
	public void SetViewAngles(in QAngle angles) {
		SetLocalAngles(angles);
		SetNetworkAngles(angles);
	}

	readonly List<Handle<SharedBaseEntity>> SimulatedByThisPlayer = [];

	public override void ReceiveMessage(int classID, bf_read msg) {
		if (classID != GetClientClass().ClassID) {
			// message is for subclass

			base.ReceiveMessage(classID, msg);
			return;
		}

		int messageType = msg.ReadByte();

		switch (messageType) {
			case PLAY_PLAYER_JINGLE:
				PlayPlayerJingle();
				break;
		}
	}

	private void PlayPlayerJingle() {

	}

	public static readonly ClientClass CC_LocalPlayerExclusive = new ClientClass("LocalPlayerExclusive", null, null, DT_LocalPlayerExclusive);

	public static readonly RecvTable DT_BasePlayer = new(DT_BaseCombatCharacter, [
		RecvPropDataTable(nameof(pl), FIELD.OF(nameof(pl)), DT_PlayerState),
		RecvPropEHandle(FIELD.OF(nameof(Vehicle))),
		RecvPropEHandle(FIELD.OF(nameof(UseEntity))),
		RecvPropInt(FIELD.OF(nameof(LifeState))),
		RecvPropEHandle(FIELD.OF(nameof(ColorCorrectionCtrl))), // << gmod specific
		RecvPropFloat(FIELD.OF(nameof(Maxspeed))),
		RecvPropInt(FIELD.OF(nameof(Flags))),
		RecvPropInt(FIELD.OF(nameof(ObserverMode))),
		RecvPropEHandle(FIELD.OF(nameof(ObserverTarget))),
		RecvPropInt(FIELD.OF(nameof(FOV))),
		RecvPropInt(FIELD.OF(nameof(FOVStart))),
		RecvPropFloat(FIELD.OF(nameof(FOVTime))),
		RecvPropFloat(FIELD.OF(nameof(DefaultFOV))),
		RecvPropEHandle(FIELD.OF(nameof(ZoomOwner))),

		RecvPropEHandle(FIELD.OF_ARRAYINDEX(nameof(ViewModel), 0)),
		RecvPropArray(FIELD.OF_ARRAY(nameof(ViewModel))),

		RecvPropString(FIELD.OF(nameof(LastPlaceName))),
		RecvPropBool(FIELD.OF(nameof(UseWeaponsInVehicle))),
		RecvPropDataTable("localdata", DT_LocalPlayerExclusive),
	]); public static readonly new ClientClass ClientClass = new ClientClass("BasePlayer", null, null, DT_BasePlayer).WithManualClassID(StaticClassIndices.CBasePlayer);


	static C_BasePlayer? localPlayer;
	public static C_BasePlayer? GetLocalPlayer() => localPlayer;

	public void FireGameEvent(IGameEvent ev) {
		throw new NotImplementedException();
	}

	public float MaxSpeed() => Maxspeed;
	public void SetMaxSpeed(float maxSpeed) => Maxspeed = maxSpeed;

	public override bool ShouldPredict() {
		return IsLocalPlayer();
	}

	public int SurfaceProps;
	public SurfaceData_ptr? SurfaceData;
	public float SurfaceFriction;

	public override void PhysicsSimulate() {
		SharedBaseEntity? pMoveParent = GetMoveParent();
		if (pMoveParent != null)
			pMoveParent.PhysicsSimulate();

		// Make sure not to simulate this guy twice per frame
		if (SimulationTick == gpGlobals.TickCount)
			return;

		SimulationTick = gpGlobals.TickCount;

		if (!IsLocalPlayer())
			return;

		C_CommandContext ctx = GetCommandContext();
		Assert(ctx.NeedsProcessing);
		if (!ctx.NeedsProcessing)
			return;

		ctx.NeedsProcessing = false;

		// Handle FL_FROZEN.
		if ((GetFlags() & EntityFlags.Frozen) != 0) {
			ctx.Cmd.ForwardMove = 0;
			ctx.Cmd.SideMove = 0;
			ctx.Cmd.UpMove = 0;
			ctx.Cmd.Buttons = 0;
			ctx.Cmd.Impulse = 0;
			//VectorCopy ( pl.v_angle, ctx->cmd.viewangles );
		}

		// Run the next command
		prediction.RunCommand(
			this,
			C_CommandContext.SafeCmdPointer(ctx),
			MoveHelper());
	}

	readonly C_CommandContext CommandContext = new();

	public C_CommandContext GetCommandContext() {
		return CommandContext;
	}

	public override void Dispose() {
		base.Dispose();
		if (this == localPlayer) {
			localPlayer = null;
		}
	}

	float OldPlayerZ;

	public override void PostDataUpdate(DataUpdateType updateType) {
		if (updateType == DataUpdateType.Created) {
			int localPlayerIndex = engine.GetLocalPlayer();

			if (localPlayerIndex == Index) {
				localPlayer = this;
			}
		}

		bool forceEFNoInterp = IsNoInterpolationFrame();

		if (IsLocalPlayer())
			SetSimulatedEveryTick(true);
		else {
			SetSimulatedEveryTick(false);

			// estimate velocity for non local players
			TimeUnit_t flTimeDelta = SimulationTime - OldSimulationTime;
			if (flTimeDelta > 0 && !(IsNoInterpolationFrame() || forceEFNoInterp)) {
				Vector3 newVelo = (GetNetworkOrigin() - GetOldOrigin()) / (float)flTimeDelta;
				SetAbsVelocity(newVelo);
			}
		}

		base.PostDataUpdate(updateType);

		if (IsLocalPlayer()) {
			QAngle angles;
			engine.GetViewAngles(out angles);
			if (updateType == DataUpdateType.Created) {
				SetLocalViewAngles(angles);
				OldPlayerZ = GetLocalOrigin().Z;
			}

			SetLocalAngles(angles);
		}

		// If we are updated while paused, allow the player origin to be snapped by the
		//  server if we receive a packet from the server
		if (engine.IsPaused() || forceEFNoInterp)
			ResetLatched();
	}

	public void SetLocalViewAngles(in QAngle angles) {
		pl.ViewingAngle = angles;
	}

	bool DeadFlag;
	internal readonly PlayerState pl = new();
	public readonly PlayerLocalData Local = new();
	readonly EHANDLE Vehicle = new();
	readonly EHANDLE UseEntity = new();
	readonly EHANDLE ObserverTarget = new();
	readonly EHANDLE ZoomOwner = new();
	public readonly EHANDLE ConstraintEntity = new();
	readonly EHANDLE TonemapController = new();
	readonly EHANDLE ViewEntity = new();
	InlineArrayNewMaxViewmodels<Handle<C_BaseViewModel>> ViewModel = new();
	bool DisableWorldClicking;
	public float Maxspeed;
	public int Flags;
	public int ObserverMode;
	public int FOV;
	public int FOVStart;
	public float FOVTime;
	public float DefaultFOV;
	public Vector3 ConstraintCenter;
	public float ConstraintRadius;
	public float ConstraintWidth;
	public float ConstraintSpeedFactor;
	InlineArray18<char> LastPlaceName;
	readonly EHANDLE ColorCorrectionCtrl = new();
	bool UseWeaponsInVehicle;
	public bool OnTarget;
	public double DeathTime;
	public double LaggedMovementValue;
	public int TickBase;
	public long FinalPredictedTick;
	InlineArray32<char> AnimExtension;

	public int GetHealth() => Health;
	public bool IsSuitEquipped() => Local.WearingSuit;

	public TimeUnit_t GetLaggedMovementValue() => LaggedMovementValue;

	public C_BaseViewModel? GetViewModel(int index = 0, bool observerOK = true) {
		C_BaseViewModel? vm = ViewModel[index].Get();
		// TODO: Observer OK

		return vm;
	}

	public IClientVehicle? GetVehicle() {
		C_BaseEntity? vehicleEnt = Vehicle.Get();
		return vehicleEnt?.GetClientVehicle();
	}
	public BaseCombatWeapon? GetLastWeapon() => LastWeapon.Get();

	public static readonly ConVar cl_customsounds = new("cl_customsounds", "0", 0, "Enable customized player sound playback");
	public static readonly ConVar spec_track = new("spec_track", "0", 0, "Tracks an entity in spec mode");
	public static readonly ConVar cl_smooth = new("cl_smooth", "1", 0, "Smooth view/eye origin after prediction errors");
	public static readonly ConVar cl_smoothtime = new(
	"cl_smoothtime",
		"0.1",
		0,
		"Smooth client's view after prediction error over this many seconds",
		0.01, // min/max is 0.01/2.0
		2.0
	 );

	public virtual bool CreateMove(TimeUnit_t inputSampleTime, ref UserCmd cmd) {
		return true;
	}
	public void GetPredictionErrorSmoothingVector(out Vector3 offset) {
		if (engine.IsPlayingDemo() || cl_smooth.GetInt() == 0 || cl_predict.GetInt() == 0 || engine.IsPaused()) {
			offset = default;
			return;
		}

		// TODO: Evaluate how changing cast order affects this accuracy
		float errorAmount = ((float)gpGlobals.CurTime - (float)PredictionErrorTime) / cl_smoothtime.GetFloat();

		if (errorAmount >= 1.0f) {
			offset = default;
			return;
		}

		errorAmount = 1.0f - errorAmount;

		offset = PredictionError * errorAmount;
	}
	public Vector3 PredictionError;
	public Vector3 PreviouslyPredictedOrigin;
	public TimeUnit_t PredictionErrorTime;
	public void NotePredictionError(in Vector3 delta) {
		if (!IsAlive())
			return;

		GetPredictionErrorSmoothingVector(out Vector3 oldDelta);
		PredictionError = delta + oldDelta;
		PredictionErrorTime = gpGlobals.CurTime;
		ResetLatched();
	}

	protected override bool ShouldInterpolate() {
		if (IsLocalPlayer())
			return true;

		return base.ShouldInterpolate();
	}

	public ref readonly QAngle GetPunchAngle() => ref Local.PunchAngle;
	public void SetPunchAngle(in QAngle angle) => Local.PunchAngle = angle;

	public BaseCombatWeapon? GetActiveWeapon() {
		BasePlayer fromPlayer = this;

		if (fromPlayer == GetLocalPlayer()) {// observer mode todo
			C_BaseEntity? target = ObserverTarget.Get();

			if (target != null && target.IsPlayer())
				fromPlayer = (BasePlayer)target;
		}

		return fromPlayer.ActiveWeapon.Get();
	}

	public bool IsPoisoned() => Local.Poisoned;

	public void ResetAutoaim() => OnTarget = false;
	public ObserverMode GetObserverMode() => (ObserverMode)ObserverMode;

	public int CurrentCommandNumber() => CurrentCommand.Get().CommandNumber;
}
