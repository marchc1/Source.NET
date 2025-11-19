using Game.Shared;

using Source;
using Source.Common;
using Source.Common.Client;
using Source.Common.Mathematics;
using Source.Engine;

using System;
using System.Numerics;

using FIELD = Source.FIELD<Game.Client.C_BasePlayer>;

namespace Game.Client;

public partial class C_BasePlayer : C_BaseCombatCharacter, IGameEventListener2
{
	public static readonly RecvTable DT_PlayerState = new([
		RecvPropInt(FIELD.OF(nameof(DeadFlag)))
	]); public static readonly ClientClass CC_PlayerState = new("PlayerState", null, null, DT_PlayerState);

	public TimeUnit_t GetFinalPredictedTime() => gpGlobals.TickCount * TICK_INTERVAL; // TEMPORARY //  FinalPredictedTick * TICK_INTERVAL;
	public bool IsLocalPlayer() => GetLocalPlayer() == this;

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

	public virtual void PreThink() {}
	public virtual void PostThink() {}

	public virtual bool IsOverridingViewmodel() => false;
	public virtual int DrawOverriddenViewmodel(C_BaseViewModel viewmodel, StudioFlags flags) => 0;

	public void SetViewAngles(in QAngle angles) {
		SetLocalAngles(angles);
		SetNetworkAngles(angles);
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
	]); public static readonly new ClientClass ClientClass = new ClientClass("BasePlayer", null, null, DT_BasePlayer);


	static C_BasePlayer? localPlayer;
	public static C_BasePlayer? GetLocalPlayer() => localPlayer;

	public void FireGameEvent(IGameEvent ev) {
		throw new NotImplementedException();
	}

	public override void Dispose() {
		base.Dispose();
		if (this == localPlayer) {
			localPlayer = null;
		}
	}

	float OldPlayerZ;

	public override void PostDataUpdate(DataUpdateType updateType) {
		if(updateType == DataUpdateType.Created) {
			int localPlayerIndex = engine.GetLocalPlayer();

			if(localPlayerIndex == Index) {
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
	readonly EHANDLE ConstraintEntity = new();
	readonly EHANDLE TonemapController = new();
	readonly EHANDLE ViewEntity = new();
	InlineArrayNewMaxViewmodels<Handle<C_BaseViewModel>> ViewModel = new(); 
	bool DisableWorldClicking;
	float MaxSpeed;
	int Flags;
	int ObserverMode;
	int FOV;
	int TickBase;
	int FOVStart;
	float FOVTime;
	float DefaultFOV;
	Vector3 ConstraintCenter;
	float ConstraintRadius;
	float ConstraintWidth;
	float ConstraintSpeedFactor;
	InlineArray18<char> LastPlaceName;
	readonly EHANDLE ColorCorrectionCtrl = new();
	bool UseWeaponsInVehicle;
	public bool OnTarget;
	public double DeathTime;
	public double LaggedMovementValue;
	public int FinalPredictedTick;
	readonly EHANDLE LastWeapon = new();

	public int GetHealth() => Health;
	public bool IsSuitEquipped() => Local.WearingSuit;

	public C_BaseViewModel? GetViewModel(int index, bool observerOK = true) {
		C_BaseViewModel? vm = ViewModel[index].Get();
		// TODO: Observer OK

		return vm;
	}



	public virtual bool CreateMove(TimeUnit_t inputSampleTime, ref UserCmd cmd) {
		return true;
	}
}
