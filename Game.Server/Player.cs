using Game.Shared;

using Source;
using Source.Common;
using Source.Common.Client;
using Source.Common.Engine;
using Source.Common.Mathematics;
using Source.Common.Physics;

using System.Numerics;
using System.Runtime.CompilerServices;

namespace Game.Server;

using FIELD = Source.FIELD<BasePlayer>;

public enum PlayerConnectedState
{
	Connected,
	Disconnecting,
	Disconnected
}

public partial class BasePlayer : BaseCombatCharacter
{
	public static readonly SendTable DT_PlayerState = new([
		SendPropInt(FIELD<PlayerState>.OF(nameof(PlayerState.DeadFlag)), 1, PropFlags.Unsigned)
	]); public static readonly ServerClass CC_PlayerState = new("PlayerState", DT_PlayerState);
	public override bool IsPlayer() => true;
	public BaseViewModel GetViewModel(int index) => throw new NotImplementedException();

	public static readonly SendTable DT_LocalPlayerExclusive = new([
		SendPropDataTable(nameof(Local), PlayerLocalData.DT_Local),
		SendPropFloat(FIELD.OF(nameof(Friction)), 0, PropFlags.NoScale | PropFlags.RoundDown, 0.0f, 4.0f),
		SendPropArray3(FIELD.OF_ARRAY(nameof(Ammo)), SendPropInt( FIELD.OF_ARRAYINDEX(nameof(Ammo)), 16, PropFlags.Unsigned)),
		SendPropInt(FIELD.OF(nameof(OnTarget)), 2, PropFlags.Unsigned),
		SendPropInt(FIELD.OF(nameof(TickBase)), -1, PropFlags.ChangesOften),
		SendPropInt(FIELD.OF(nameof(NextThinkTick))),
		SendPropEHandle(FIELD.OF(nameof(LastWeapon))),
		SendPropEHandle(FIELD.OF(nameof(GroundEntity)), PropFlags.ChangesOften),
		SendPropVector(FIELD.OF(nameof(BaseVelocity)), 0, PropFlags.NoScale),
		SendPropEHandle(FIELD.OF(nameof(ConstraintEntity))),
		SendPropVector(FIELD.OF(nameof(ConstraintCenter)), 0, PropFlags.NoScale),
		SendPropFloat(FIELD.OF(nameof(ConstraintRadius)), 0, PropFlags.NoScale),
		SendPropFloat(FIELD.OF(nameof(ConstraintWidth)), 0, PropFlags.NoScale),
		SendPropFloat(FIELD.OF(nameof(ConstraintSpeedFactor)), 0, PropFlags.NoScale),
		SendPropFloat(FIELD.OF(nameof(DeathTime)), 0, PropFlags.NoScale),
		SendPropFloat(FIELD.OF(nameof(LaggedMovementValue)), 0, PropFlags.NoScale),
		SendPropEHandle(FIELD.OF(nameof(TonemapController))),
		SendPropEHandle(FIELD.OF(nameof(ViewEntity))),
		SendPropBool(FIELD.OF(nameof(DisableWorldClicking))),
	]); public static readonly ServerClass PlayerExclusive = new("LocalPlayerExclusive", DT_LocalPlayerExclusive);

	public static readonly SendTable DT_BasePlayer = new(DT_BaseCombatCharacter, [
		SendPropDataTable(nameof(pl), FIELD.OF(nameof(pl)), DT_PlayerState, SendProxy_DataTableToDataTable),
		SendPropEHandle(FIELD.OF(nameof(Vehicle))),
		SendPropEHandle(FIELD.OF(nameof(UseEntity))),
		SendPropInt(FIELD.OF(nameof(LifeState)), 3, PropFlags.Unsigned ),
		SendPropEHandle(FIELD.OF(nameof(ColorCorrectionCtrl))), // << gmod specific
		SendPropFloat(FIELD.OF(nameof(Maxspeed)), 12, PropFlags.RoundDown, 0.0f, 2048.0f ),
		SendPropInt(FIELD.OF(nameof(Flags)), Constants.PLAYER_FLAG_BITS, PropFlags.Unsigned|PropFlags.ChangesOften, SendProxy_CropFlagsToPlayerFlagBitsLength),
		SendPropInt(FIELD.OF(nameof(ObserverMode)), 3, PropFlags.Unsigned),
		SendPropEHandle(FIELD.OF(nameof(ObserverTarget))),
		SendPropInt(FIELD.OF(nameof(FOV)), 8, PropFlags.Unsigned),
		SendPropInt(FIELD.OF(nameof(FOVStart)), 8, PropFlags.Unsigned),
		SendPropFloat(FIELD.OF(nameof(FOVTime)), 0, PropFlags.NoScale),
		SendPropFloat(FIELD.OF(nameof(DefaultFOV)), 8, PropFlags.Unsigned),
		SendPropEHandle(FIELD.OF(nameof(ZoomOwner))),

		SendPropEHandle(FIELD.OF_ARRAYINDEX(nameof(ViewModel), 0)),
		SendPropArray(FIELD.OF_ARRAY(nameof(ViewModel))),

		SendPropString(FIELD.OF(nameof(LastPlaceName))),
		SendPropBool(FIELD.OF(nameof(UseWeaponsInVehicle))),
		SendPropDataTable( "localdata", DT_LocalPlayerExclusive, SendProxy_SendLocalDataTable),
	]);

	public static Edict? s_PlayerEdict;

	public int StuckLast;
	public float MaxSpeed() => Maxspeed;
	public void SetMaxSpeed(float maxSpeed) => Maxspeed = maxSpeed;
	public TimeUnit_t GetLaggedMovementValue() => LaggedMovementValue;

	public int SurfaceProps;
	public SurfaceData_ptr? SurfaceData;
	public float SurfaceFriction;

	public static void SendProxy_CropFlagsToPlayerFlagBitsLength(SendProp prop, object instance, IFieldAccessor field, ref DVariant outData, int element, int objectID) {
		int mask = (1 << Constants.PLAYER_FLAG_BITS) - 1;
		int data = field.GetValue<int>(instance);
		outData.Int = data & mask;
	}

	public static object? SendProxy_SendLocalDataTable(SendProp prop, object instance, IFieldAccessor data, SendProxyRecipients recipients, int objectID) {
		// recipients.SetOnly(objectID - 1);
		return data;
	}
	public static object? SendProxy_SendNonLocalDataTable(SendProp prop, object instance, IFieldAccessor data, SendProxyRecipients recipients, int objectID) {
		// throw new NotImplementedException();
		return data;
	}

	public static readonly new ServerClass ServerClass = new ServerClass("BasePlayer", DT_BasePlayer).WithManualClassID(StaticClassIndices.CBasePlayer);

	public BasePlayer() {
		AddEFlags(EFL.NoAutoEdictAttach);

		if (s_PlayerEdict != null) {
			NetworkProp().AttachEdict(s_PlayerEdict);
			s_PlayerEdict = null;
		}

		pl.FixAngle = (int)FixAngle.Absolute;
		pl.HLTV = false;
		pl.Replay = false;
		pl.Frags = 0;
		pl.Deaths = 0;

		Netname[0] = '\0';

		Health = 0;
		Weapon_SetLast(null);
		// BitsDamageType = 0;

		// ForceOrigin = false;
		Vehicle.Set(null);
		// CurrentCommand = null;
		// LockViewanglesTickNumber = 0;
		// AngLockViewangles.Init();

		// Setup our default FOV
		// DefaultFOV = GameRules.DefaultFOV();

		ZoomOwner.Set(null);

		// UpdateRate = 20;  // cl_updaterate defualt
		// LerpTime = 0.1f; // cl_interp default
		// PredictWeapons = true;
		// LagCompensation = false;
		LaggedMovementValue = 1.0f;
		StuckLast = 0;
		// ImpactEnergyScale = 1.0f;
		// LastPlayerTalkTime = 0.0f;
		// PlayerInfo.SetParent(this);

		// ResetObserverMode();

		SurfaceProps = 0;
		SurfaceData = null;
		SurfaceFriction = 1.0f;
		// TextureType = 0;
		// PreviousTextureType = 0;

		// SuicideCustomKillFlags = 0;
		// Delay = 0.0f;
		// ReplayEnd = -1;
		// ReplayEntity = 0;

		// AutoKickDisabled = false;

		// NumCrouches = 0;
		// DuckToggled = false;
		// PhysicsWasFrozen = false;

		// ButtonDisabled = 0;
		// ButtonForced = 0;

		// BodyPitchPoseParam = -1;
		// ForwardMove = 0;
		// SideMove = 0;

		// // NVNT default to no haptics
		// HasHaptics = false;

		ConstraintCenter = vec3_origin;

		// LastUserCommandTime = 0.0f;
		// MovementTimeForUserCmdProcessingRemaining = 0.0f;

		// LastObjectiveTime = -1.0f;
	}

	public readonly PlayerState pl = new();
	public readonly PlayerLocalData Local = new();
	public EHANDLE Vehicle = new();
	public EHANDLE UseEntity = new();
	public EHANDLE ObserverTarget = new();
	public EHANDLE ZoomOwner = new();
	public EHANDLE ConstraintEntity = new();
	public EHANDLE TonemapController = new();
	public EHANDLE ViewEntity = new();
	InlineArrayNewMaxViewmodels<Handle<BaseViewModel>> ViewModel = new();
	readonly List<Handle<BaseEntity>> SimulatedByThisPlayer = [];

	public IServerVehicle? GetVehicle() => Vehicle.Get()?.GetServerVehicle();
	public BaseEntity? GetVehicleEntity() => Vehicle.Get();
	public bool IsInAVehicle() => Vehicle.Get() != null;


	bool DisableWorldClicking;
	float Maxspeed;
	int Flags;
	int ObserverMode;
	int FOV;
	int TickBase;
	int FOVStart;
	TimeUnit_t FOVTime;
	float DefaultFOV;
	Vector3 ConstraintCenter;
	float ConstraintRadius;
	float ConstraintWidth;
	float ConstraintSpeedFactor;
	InlineArray18<char> LastPlaceName;
	EHANDLE ColorCorrectionCtrl = new();
	bool UseWeaponsInVehicle;
	public bool OnTarget;
	public TimeUnit_t DeathTime;
	public double LaggedMovementValue;
	public TimeUnit_t StepSoundTime;

	InlineArray32<char> AnimExtension;
	public Vector3 WaterJumpVel;
	public TimeUnit_t SwimSoundTime;
	public Vector3 LadderNormal;
	public TimeUnit_t WaterJumpTime;
	public PlayerConnectedState Connected;
	public bool IsObserver() => GetObserverMode() != Shared.ObserverMode.None;
	public InButtons AfButtonLast;
	public InButtons AfButtonPressed;
	public InButtons AfButtonReleased;
	public InButtons Buttons;

	public BaseViewModel? GetViewModel(int index = 0, bool observerOK = true) {
		return ViewModel[index].Get();
	}

	public BaseCombatWeapon? GetLastWeapon() => LastWeapon.Get();
	public BaseCombatWeapon? GetActiveWeapon() => ActiveWeapon.Get();
	public void ResetAutoaim() => OnTarget = false;

	public ObserverMode GetObserverMode() => (ObserverMode)ObserverMode;
	public AnonymousSafeFieldPointer<UserCmd> CurrentCommand;
	public int CurrentCommandNumber() => CurrentCommand.Get().CommandNumber;

	public void ImpulseCommands() {
		// todo
	}

	public virtual void InitialSpawn() {
		Connected = PlayerConnectedState.Connected;
		// gamestats todo
	}

	BaseEntity? FindPlayerStart(ReadOnlySpan<char> className) {
		BaseEntity? start = gEntList.FindEntityByClassname(null, className);
		BaseEntity? startFirst = start;

		while (start != null) {
			if (start.HasSpawnFlags(1))
				return start;

			start = gEntList.FindEntityByClassname(start, className);
		}

		return startFirst;
	}

	public BaseEntity? EntSelectSpawnPoint() {
		BaseEntity? spot;
		Edict player = Edict();

		// if coop
		// elseif deathmatch todo

		if (gpGlobals.StartSpot == null || gpGlobals.StartSpot.Length == 0) {
			spot = FindPlayerStart("info_player_start");
			if (spot != null)
				goto ReturnSpawn;
		}
		else {
			spot = gEntList.FindEntityByName(null, gpGlobals.StartSpot);
			if (spot != null)
				goto ReturnSpawn;
		}

	ReturnSpawn:
		if (spot == null) {
			Warning("PutClientInServer: no info_player_start on level\n");
			return Instance(engine.PEntityOfEntIndex(0)!);
		}

		// LastSpawn = spot; todo
		return spot;
	}

	public override void Spawn() {
		// if (Hints()) Hints().ResetHints();

		SetClassname("player");

		// SharedSpawn();

		SetSimulatedEveryTick(true);
		SetAnimatedEveryTick(true);

		// ArmorValue = SpawnArmorValue();
		// SetBlockLOS(false);
		MaxHealth = Health;

		if ((GetFlags() & EntityFlags.FakeClient) != 0) {
			ClearFlags();
			AddFlag(EntityFlags.Client | EntityFlags.FakeClient);
		}
		else {
			ClearFlags();
			AddFlag(EntityFlags.Client);
		}

		AddFlag(EntityFlags.AimTarget);

		EntityEffects effects = (EntityEffects)Effects & EntityEffects.NoShadow;
		SetEffects(effects);

		// IncrementInterpolationFrame();

		// InitFogController();

		// DmgTake = 0;
		// DmgSave = 0;
		// HUDDamage = -1;
		// DamageType = 0;
		// PhysicsFlags = 0;
		// DrownRestored = DrownDmg;

		// SetFOV(this, 0);

		// NextDecalTime = 0;

		// GeigerDelay = gpGlobals.CurTime + 2.0f;

		// FieldOfView = 0.766;

		// AdditionPVSOrigin = vec3_origin;
		// CameraPVSOrigin = vec3_origin;

		// if (!GameHUDInitialized)
		// 	GameRules.SetDefaultPlayerTeam(this);

		GameRules.GetPlayerSpawnSpot(this);

		Local.Ducked = false;
		Local.Ducking = false;
		SetViewOffset(VEC_VIEW_SCALED(this));
		Precache();

		// SetPlayerUnderwater(false);

		// Train = TRAIN_NEW;

		// HackedGunPos = new Vector3(0, 32, 0);
		// BonusChallenge;

		// SetThink(null);

		// more todo

		// GameRules.PlayerSpawn(this);
		LaggedMovementValue = 1.0f;

		base.Spawn();
	}

	public TimeUnit_t GetDeathTime() => DeathTime;

	public virtual void SetAnimation(PlayerAnim playerAnim) { } // todo

	public virtual Vector3 GetAutoaimVector(float scale) {
		MathLib.AngleVectors(GetAbsAngles(), out Vector3 forward, out _, out _);
		return forward;
	}

	// todo
	public virtual void SetSuitUpdate(ReadOnlySpan<char> name, int fgroup, int noRepeat) { }
	[MethodImpl(MethodImplOptions.AggressiveInlining)] public void SetSuitUpdate(ReadOnlySpan<char> name, bool fgroup, int noRepeat) => SetSuitUpdate(name, fgroup ? 1 : 0, noRepeat);
	[MethodImpl(MethodImplOptions.AggressiveInlining)] public void SetSuitUpdate(ReadOnlySpan<char> name, int fgroup, bool noRepeat) => SetSuitUpdate(name, fgroup, noRepeat ? 1 : 0);
	[MethodImpl(MethodImplOptions.AggressiveInlining)] public void SetSuitUpdate(ReadOnlySpan<char> name, bool fgroup, bool noRepeat) => SetSuitUpdate(name, fgroup ? 1 : 0, noRepeat ? 1 : 0);

	double LastPlayerTalkTime;
	public TimeUnit_t LastTimePlayerTalked() => LastPlayerTalkTime;
	public void NotePlayerTalked() => LastPlayerTalkTime = gpGlobals.CurTime;

	public bool CanSpeak() => true;
}
