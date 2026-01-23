using Game.Shared;

using Source;
using Source.Common;
using Source.Common.Bitbuffers;
using Source.Common.Client;
using Source.Common.Mathematics;
using Source.Engine;

using System;
using System.Numerics;

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
	public TimeUnit_t GetFinalPredictedTime() => gpGlobals.TickCount * TICK_INTERVAL; // TEMPORARY //  FinalPredictedTick * TICK_INTERVAL;
	public bool IsLocalPlayer() => GetLocalPlayer() == this;
	public static bool ShouldDrawLocalPlayer() {
		return false; // todo
	}

	public InButtons AfButtonLast;
	public InButtons AfButtonPressed;
	public InButtons AfButtonReleased;
	public InButtons Buttons;
	public AnonymousSafeFieldPointer<UserCmd> CurrentCommand;
	public int Impulse;

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

	public virtual void PreThink() { }
	public virtual void PostThink() { }

	InlineArrayMaxAmmoSlots<int> OldAmmo;

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

	private static void RecvProxy_LocalVelocityX(ref readonly RecvProxyData data, object instance, IFieldAccessor field) {
		throw new NotImplementedException();
	}

	private static void RecvProxy_LocalVelocityY(ref readonly RecvProxyData data, object instance, IFieldAccessor field) {
		throw new NotImplementedException();
	}

	private static void RecvProxy_LocalVelocityZ(ref readonly RecvProxyData data, object instance, IFieldAccessor field) {
		throw new NotImplementedException();
	}

	public static readonly ClientClass CC_LocalPlayerExclusive = new ClientClass("LocalPlayerExclusive", null, null, DT_LocalPlayerExclusive);

	public static readonly RecvTable DT_BasePlayer = new(DT_BaseCombatCharacter, [
		RecvPropDataTable(nameof(pl), FIELD.OF(nameof(pl)), DT_PlayerState),
		RecvPropEHandle(FIELD.OF(nameof(Vehicle))),
		RecvPropEHandle(FIELD.OF(nameof(UseEntity))),
		RecvPropInt(FIELD.OF(nameof(LifeState))),
		RecvPropEHandle(FIELD.OF(nameof(ColorCorrectionCtrl))), // << gmod specific
		RecvPropFloat(FIELD.OF(nameof(MaxSpeed))),
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
	public override bool ShouldPredict() {
		return IsLocalPlayer();
	}

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

		if (IsLocalPlayer()) {
			engine.GetViewAngles(out QAngle angles);
			if (updateType == DataUpdateType.Created) {
				SetLocalViewAngles(in angles);
				OldPlayerZ = GetLocalOrigin().Z;
			}
			SetLocalAngles(angles);
		}

		base.PostDataUpdate(updateType);
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
	public float MaxSpeed;
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


	public virtual bool CreateMove(TimeUnit_t inputSampleTime, ref UserCmd cmd) {
		return true;
	}

	public bool IsInAVehicle() => Vehicle.Get() != null;

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

}
