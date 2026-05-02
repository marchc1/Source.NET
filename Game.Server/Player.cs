using Game.Shared;

using Source;
using Source.Common;
using Source.Common.Client;
using Source.Common.Commands;
using Source.Common.Engine;
using Source.Common.Formats.BSP;
using Source.Common.Mathematics;
using Source.Common.Physics;
using Source.Engine;

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
	public BaseViewModel? GetViewModel(int index) => ViewModel[index].Get();

	public static readonly SendTable DT_LocalPlayerExclusive = new([
		SendPropDataTable(nameof(Local), PlayerLocalData.DT_Local),
		SendPropFloat(FIELD.OF(nameof(Friction)), 0, PropFlags.NoScale | PropFlags.RoundDown, 0.0f, 4.0f),
		SendPropArray3(FIELD.OF_ARRAY(nameof(Ammo)), SendPropInt( FIELD.OF_ARRAYINDEX(nameof(Ammo)), 16, PropFlags.Unsigned)),
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
		SendPropFloat(FIELD.OF(nameof(FOV)), 16, PropFlags.Unsigned, 0, 65536),
		SendPropFloat(FIELD.OF(nameof(FOVStart)), 16, PropFlags.Unsigned, 0, 65536),
		SendPropTime64(FIELD.OF(nameof(FOVTime))),
		SendPropFloat(FIELD.OF(nameof(DefaultFOV)), 16, PropFlags.Unsigned, 0, 65536),
		SendPropEHandle(FIELD.OF(nameof(ZoomOwner))),

		SendPropEHandle(FIELD.OF_ARRAYINDEX(nameof(ViewModel), 0)),
		SendPropArray(FIELD.OF_ARRAY(nameof(ViewModel))),

		SendPropBool(FIELD.OF(nameof(UseWeaponsInVehicle))),
		SendPropDataTable( "localdata", DT_LocalPlayerExclusive, SendProxy_SendLocalDataTable),
	]);

	public static Edict? s_PlayerEdict;
	public static BaseEntity? g_LastSpawn;

	public int StuckLast;
	public float MaxSpeed() => Maxspeed;
	public void SetMaxSpeed(float maxSpeed) => Maxspeed = maxSpeed;
	public TimeUnit_t GetLaggedMovementValue() => LaggedMovementValue;

	public int SurfaceProps;
	public SurfaceData_ptr? SurfaceData;
	public float SurfaceFriction;
	public UtlSymId_t TextureType;
	public UtlSymId_t PreviousTextureType;

	readonly List<CommandContext> CommandContext = [];
	readonly List<PlayerSimInfo> VecPlayerSimInfo = [];

	TimeUnit_t MovementTimeForUserCmdProcessingRemaining;
	TimeUnit_t LastUserCommandTime;
	UserCmd LastCmd;

	public float ForwardMove;
	public float SideMove;

	uint PhysicsFlags;

	int LastDmageAmount;
	Vector3 DmgOrigin;
	float DmgTake;
	float DmgSave;
	DamageType bitsDamageType;
	int HUDDamage;
	TimeUnit_t DeathAnimTime;
	byte[] TimeBasedDamage = new byte[(int)ITBD.TimeBasedDamageCount];

	int DrownDmg;
	int DrownRestored;

	int PoisonDmg;
	int PoisonRestored;

	public static void SendProxy_CropFlagsToPlayerFlagBitsLength(SendProp prop, object instance, IFieldAccessor field, ref DVariant outData, int element, int objectID) {
		int mask = (1 << Constants.PLAYER_FLAG_BITS) - 1;
		int data = field.GetValue<int>(instance);
		outData.Int = data & mask;
	}

	public static object? SendProxy_SendLocalDataTable(SendProp prop, object instance, IFieldAccessor data, SendProxyRecipients recipients, int objectID) {
		recipients.SetOnly(objectID - 1);
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
		DefaultFOV = 75; // GameRules.DefaultFOV();

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
		TextureType = 0;
		PreviousTextureType = 0;

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

		LastUserCommandTime = 0.0f;
		MovementTimeForUserCmdProcessingRemaining = 0.0f;

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
	public float GetStepSize() => Local.StepSize;

	bool DisableWorldClicking;
	float Maxspeed;
	int Flags;
	int ObserverMode;
	int FOV;
	public int TickBase;
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
	int DrownDmgRate;
	public PlayerConnectedState Connected;
	TimeUnit_t AirFinished;
	TimeUnit_t PainFinished;
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

	public BaseEntity? GiveNamedItem(ReadOnlySpan<char> name, int subType = 0) {
		if (Weapon_OwnsThisType(name, subType) != null)
			return null;

		BaseEntity? pent = CreateEntityByName(name);
		if (pent == null) {
			Msg("NULL Ent in GiveNamedItem!\n");
			return null;
		}

		pent.SetLocalOrigin(GetLocalOrigin());
		// pent.AddSpawnFlags(SF_NORESPAWN);

		BaseCombatWeapon? weapon = (BaseCombatWeapon?)((BaseEntity)pent);
		weapon?.SetSubType(subType);

		Util.DispatchSpawn(pent);

		// if (pent != null && !(pent.IsMarkedForDeletion()))
		// 	pent.Touch(this);

		return pent;
	}

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

	public virtual BaseEntity? EntSelectSpawnPoint() {
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
		SetClassname("player");

		SharedSpawn();

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

		IncrementInterpolationFrame();

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

		GetPlayerSpawnSpot(this);

		Local.Ducked = false;
		Local.Ducking = false;
		SetViewOffset(VEC_VIEW_SCALED(this));
		Precache();

		SetPlayerUnderwater(false);

		// Train = TRAIN_NEW;

		// HackedGunPos = new Vector3(0, 32, 0);
		// BonusChallenge;

		// SetThink(null);

		InitHUD = true;

		// more todo

		// GameRules.PlayerSpawn(this);
		LaggedMovementValue = 1.0f;

		base.Spawn();
	}

	private void IncrementInterpolationFrame() => InterpolationFrame = (byte)((InterpolationFrame + 1) % NOINTERP_PARITY_MAX);

	public TimeUnit_t GetDeathTime() => DeathTime;

	public virtual void SetAnimation(PlayerAnim playerAnim) { } // todo

#if HL2_DLL
	const int AIRTIME = 7;
	const int DROWNING_DAMAGE_INITIAL = 10;
	const int DROWNING_DAMAGE_MAX = 10;
#else
	const int AIRTIME =  12;
	const int DROWNING_DAMAGE_INITIAL = 2;
	const int DROWNING_DAMAGE_MAX = 5;
#endif

	void WaterMove() {
		if ((GetMoveType() == Source.MoveType.Noclip) && GetMoveParent() == null) {
			AirFinished = gpGlobals.CurTime + AIRTIME;
			return;
		}

		if (Health < 0 || !IsAlive()) {
			UpdateUnderwaterState();
			return;
		}

		if (GetWaterLevel() != Shared.WaterLevel.Eyes || CanBreatheUnderwater()) {
			if (AirFinished < gpGlobals.CurTime)
				EmitSound("Player.DrownStart");

			AirFinished = gpGlobals.CurTime + AIRTIME;
			DrownDmgRate = DROWNING_DAMAGE_INITIAL;

			if (DrownDmg > DrownRestored) {
				bitsDamageType |= DamageType.DrownRecover;
				bitsDamageType &= ~DamageType.Drown;
				TimeBasedDamage[(int)ITBD.DrownRecover] = 0;
			}
		}
		else {
			bitsDamageType &= ~DamageType.DrownRecover;
			TimeBasedDamage[(int)ITBD.DrownRecover] = 0;

			if (AirFinished < gpGlobals.CurTime && (GetFlags() & EntityFlags.GodMode) == 0) // drown!
			{
				if (PainFinished < gpGlobals.CurTime) {
					DrownDmgRate += 1;
					if (DrownDmgRate > DROWNING_DAMAGE_MAX) {
						DrownDmgRate = DROWNING_DAMAGE_MAX;
					}

					// OnTakeDamage(TakeDamageInfo(GetContainingEntity(INDEXENT(0)), GetContainingEntity(INDEXENT(0)), DrownDmgRate, DamageType.Drown));
					PainFinished = gpGlobals.CurTime + 1;

					DrownDmg += DrownDmgRate;
				}
			}
			else
				bitsDamageType &= ~DamageType.Drown;
		}

		UpdateUnderwaterState();
	}

	public virtual bool CanBreatheUnderwater() => false;

	bool PlayerUnderwater;
	private bool IsPlayerUnderwater() => PlayerUnderwater;

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

	int GetCommandContextCount() => CommandContext.Count;

	CommandContext AllocCommandContext() {
		CommandContext ctx = new();
		CommandContext.Add(ctx);

		if (CommandContext.Count > 1000)
			Assert(false);

		return ctx;
	}

	void RemoveCommandContext(int index) => CommandContext.RemoveAt(index);

	CommandContext RemoveAllCommandContextsExceptNewest() {
		int count = CommandContext.Count;
		int toRemove = count - 1;
		if (toRemove > 0)
			CommandContext.RemoveRange(0, toRemove);

		if (CommandContext.Count == 0) {
			Assert(false);
			CommandContext.Add(AllocCommandContext());
		}

		return CommandContext[0];
	}

	CommandContext GetCommandContext(int index) => CommandContext[index];

	void RemoveAllCommandContexts() => CommandContext.Clear();

	void ReplaceContextCommands(CommandContext ctx, UserCmd[] cmds, int commands) {
		ctx.Cmds.Clear();

		ctx.NumCmds = commands;
		ctx.TotalCmds = commands;
		ctx.DroppedPackets = 0;

		for (int i = commands - 1; i >= 0; --i)
			ctx.Cmds.Add(cmds[i]);
	}

	int DetermineSimulationTicks() {
		int commandContextCount = GetCommandContextCount();

		int contextNumber;
		int simulationTicks = 0;

		for (contextNumber = 0; contextNumber < commandContextCount; contextNumber++) {
			CommandContext ctx = GetCommandContext(contextNumber);
			Assert(ctx != null);
			Assert(ctx.NumCmds > 0);
			Assert(ctx.DroppedPackets >= 0);

			simulationTicks += ctx.NumCmds + ctx.DroppedPackets;
		}

		return simulationTicks;
	}

	void AdjustPlayerTimeBase(int simulationTicks) {
		Assert(simulationTicks >= 0);
		if (simulationTicks < 0)
			return;

		// todo

		if (gpGlobals.MaxClients == 1)
			TickBase = (int)(gpGlobals.TickCount + simulationTicks + gpGlobals.SimTicksThisFrame);
		else {

		}
	}

	bool IsUserCmdDataValid(UserCmd cmd) {
		return true;// todo
	}

	bool ShouldRunCommandsInContext(CommandContext ctx) {
		return true; // todo
	}

	UserCmd GetLastUserCommand() => LastCmd; // todo BotCmd

	void SetLastUserCommand(UserCmd cmd) => LastCmd = cmd;

	static ConVar sv_usercmd_custom_random_seed = new("1", FCvar.Cheat, "When enabled server will populate an additional random seed independent of the client");

	public void ProcessUsercmds(UserCmd[] cmds, int numcmds, int totalcmds, int dropped_packets, bool paused) {
		CommandContext ctx = AllocCommandContext();
		Assert(ctx);

		int i;
		for (i = totalcmds - 1; i >= 0; i--) {
			UserCmd cmd = cmds[totalcmds - 1 - i];

			if (!IsUserCmdDataValid(cmd))
				cmd.MakeInert();

			if (sv_usercmd_custom_random_seed.GetBool()) {
				float timeNow = (float)Platform.Time;
				cmd.ServerRandomSeed = BitConverter.SingleToInt32Bits(timeNow);
			}
			else
				cmd.ServerRandomSeed = cmd.RandomSeed;

			ctx.Cmds.Add(cmd);
		}

		ctx.NumCmds = numcmds;
		ctx.TotalCmds = totalcmds;
		ctx.DroppedPackets = dropped_packets;
		ctx.Paused = paused;

		if (ctx.Paused) {
			bool clear_angles = true;

			if (GetMoveType() == Source.MoveType.Noclip /*&& sv_cheats.GetBool() && sv_noclipduringpause.GetBool()*/)
				clear_angles = false;

			for (i = 0; i < ctx.NumCmds; i++) {
				UserCmd cm = ctx.Cmds[i];
				cm.Buttons = 0;
				if (clear_angles) {
					cm.ForwardMove = 0;
					cm.SideMove = 0;
					cm.UpMove = 0;
					MathLib.VectorCopy(pl.ViewingAngle, out cm.ViewAngles);
				}
				ctx.Cmds[i] = cm;
			}

			ctx.DroppedPackets = 0;
		}

		// GamePaused = paused;

		if (paused) {
			ForceSimulation();
			PhysicsSimulate();
		}

		// if (sv_playerperfhistorycount.GetInt() > 0) {

		// }
	}

	public void ForceSimulation() => SimulationTick = -1;

	public override void PhysicsSimulate() {
		BaseEntity? moveParent = GetMoveParent();
		moveParent?.PhysicsSimulate();

		if (SimulationTick == gpGlobals.TickCount)
			return;

		SimulationTick = gpGlobals.TickCount;

		int simulation_ticks = DetermineSimulationTicks();

		if (simulation_ticks > 0)
			AdjustPlayerTimeBase(simulation_ticks);

		TimeUnit_t savetime = gpGlobals.CurTime;
		TimeUnit_t saveframetime = gpGlobals.FrameTime;

		int command_context_count = GetCommandContextCount();

		List<UserCmd> vecAvailCommands = [];

		for (int context_number = 0; context_number < command_context_count; context_number++) {
			CommandContext ctx = GetCommandContext(context_number);
			if (!ShouldRunCommandsInContext(ctx))
				continue;

			if (ctx.Cmds.Count == 0)
				continue;

			int numbackup = ctx.TotalCmds - ctx.NumCmds;

			if (ctx.DroppedPackets < 24) {
				int droppedcmds = ctx.DroppedPackets;

				while (droppedcmds > numbackup) {
					UserCmd lastCmd = GetLastUserCommand();
					lastCmd.CommandNumber++;
					vecAvailCommands.Add(lastCmd);
					droppedcmds--;
				}

				while (droppedcmds > 0) {
					int cmdnum = ctx.NumCmds + droppedcmds - 1;
					vecAvailCommands.Add(ctx.Cmds[cmdnum]);
					droppedcmds--;
				}
			}

			for (int i = ctx.NumCmds - 1; i >= 0; i--)
				vecAvailCommands.Add(ctx.Cmds[i]);

			LastCmd = ctx.Cmds[ctx.Cmds.Count - 1];
		}

		int commandLimit = IsSimulatingOnAlternateTicks() ? 2 : 1;
		int commandsToRun = vecAvailCommands.Count;
		if (gpGlobals.SimTicksThisFrame >= commandLimit && vecAvailCommands.Count > commandLimit) {
			int commandsToRollOver = Math.Min(vecAvailCommands.Count, (gpGlobals.SimTicksThisFrame - 1));
			commandsToRun = vecAvailCommands.Count - commandsToRollOver;
			Assert(commandsToRun >= 0);
			if (commandsToRollOver > 0) {
				CommandContext ctx = RemoveAllCommandContextsExceptNewest();
				ReplaceContextCommands(ctx, [.. vecAvailCommands.GetRange(vecAvailCommands.Count - commandsToRollOver, commandsToRollOver)], commandsToRollOver);
			}
			else
				RemoveAllCommandContexts();
		}
		else
			RemoveAllCommandContexts();

		TimeUnit_t vphysicsArrivalTime = TICK_INTERVAL;

		int numUsrCmdProcessTicksMax = 0; // sv_maxusrcmdprocessticks.GetInt();
		if (gpGlobals.MaxClients != 1 && numUsrCmdProcessTicksMax > 0) {
			MovementTimeForUserCmdProcessingRemaining += TICK_INTERVAL;

			if (MovementTimeForUserCmdProcessingRemaining > numUsrCmdProcessTicksMax * TICK_INTERVAL)
				MovementTimeForUserCmdProcessingRemaining = numUsrCmdProcessTicksMax * TICK_INTERVAL;
		}
		else
			MovementTimeForUserCmdProcessingRemaining = float.MaxValue;

		if (commandsToRun > 0) {
			LastUserCommandTime = savetime;

			MoveHelperServer.s_MoveHelperServer.SetHost(this);

			if (IsPredictingWeapons())
				IPredictionSystem.SuppressHostEvents(this);

			for (int i = 0; i < commandsToRun; ++i) {
				PlayerRunCommand(vecAvailCommands[i], MoveHelperServer.s_MoveHelperServer);

				// if (PhysicsController != null) { // todo
				// 	// UpdateVPhysicsPosition(vNewVPhysicsPosition, vNewVPhysicsVelocity, vphysicsArrivalTime);
				// 	vphysicsArrivalTime += TICK_INTERVAL;
				// }
			}

			IPredictionSystem.SuppressHostEvents(null);

			MoveHelperServer.s_MoveHelperServer.SetHost(null);

			if (VecPlayerSimInfo.Count > 0) {
				PlayerSimInfo pi = VecPlayerSimInfo[VecPlayerSimInfo.Count - 1];
				pi.Time = Platform.Time;
				pi.AbsOrigin = GetAbsOrigin();
				pi.GameSimulationTime = gpGlobals.CurTime;
				pi.NumCmds = commandsToRun;
			}
		}
		else if (GetTimeSinceLastUserCommand() > 3.0f /*sv_player_usercommand_timeout.GetFloat()*/)
			RunNullCommand();

		gpGlobals.CurTime = savetime;
		gpGlobals.FrameTime = saveframetime;
	}

	private void RunNullCommand() {
		UserCmd cmd = new();

		TimeUnit_t oldFrameTime = gpGlobals.FrameTime;
		TimeUnit_t oldCurTime = gpGlobals.CurTime;

		pl.FixAngle = (int)FixAngle.None;

		cmd.ViewAngles = EyeAngles();

		TimeUnit_t timeBase = gpGlobals.CurTime;
		SetTimeBase(timeBase);

		MoveHelperServer.s_MoveHelperServer.SetHost(this);
		PlayerRunCommand(cmd, MoveHelperServer.s_MoveHelperServer);

		SetLastUserCommand(cmd);

		gpGlobals.FrameTime = oldFrameTime;
		gpGlobals.CurTime = oldCurTime;

		MoveHelperServer.s_MoveHelperServer.SetHost(null);
	}

	private void SetTimeBase(double timeBase) => TickBase = TIME_TO_TICKS(timeBase);

	private class UserCmdRef
	{
		public UserCmd Cmd;
		public AnonymousSafeFieldPointer<UserCmd> Ptr => new(this, static o => ref ((UserCmdRef)o).Cmd);
	}

	public virtual void PlayerRunCommand(UserCmd userCmd, MoveHelperServer s_MoveHelperServer) {
		// TouchedPhysObject = false;

		if (pl.FixAngle == (int)FixAngle.None)
			MathLib.VectorCopy(userCmd.ViewAngles, out pl.ViewingAngle);

		// todo

		g_PlayerMove.RunCommand(this, new UserCmdRef { Cmd = userCmd }.Ptr, s_MoveHelperServer);
	}

	public bool IsPredictingWeapons() => false; // todo

	TimeUnit_t GetTimeSinceLastUserCommand() => /*(!IsConnected() || IsFakeClient() || IsBot()) ? 0.0f :*/ gpGlobals.CurTime - LastUserCommandTime;

	public override EdictFlags UpdateTransmitState() => SetTransmitState(EdictFlags.FullCheck);

	public override EdictFlags ShouldTransmit(CheckTransmitInfo info) {
		if (info.ClientEnt == Edict())
			return EdictFlags.Always;

		// todo

		return base.ShouldTransmit(info);
	}

	const float SMOOTHING_FACTOR = 0.9f;
	public virtual void PostThink() {
		// SmoothedVelocity = SmoothedVelocity * SMOOTHING_FACTOR + GetAbsVelocity() * (1 - SMOOTHING_FACTOR);

		if (!g_fGameOver /*&& !PlayerLocked*/) {
			if (IsAlive()) {
				// if ((GetFlags() & EntityFlags.Ducking) != 0)
				// 	SetCollisionBounds(VEC_DUCK_HULL_MIN, VEC_DUCK_HULL_MAX);
				// else
				// 	SetCollisionBounds(VEC_HULL_MIN, VEC_HULL_MAX);

				// if (UseEntity != null) {
				// 	if (UseEntity.OnControls(this) && (!GetActiveWeapon() || GetActiveWeapon()->IsEffectActive(EF_NODRAW) || (GetActiveWeapon()->GetActivity() == ACT_VM_HOLSTER)))
				// 		UseEntity.Use(this, this, USE_SET, 2);
				// 	else
				// 		ClearUseEntity();
				// }

				ItemPostFrame();

				if ((GetFlags() & EntityFlags.OnGround) != 0) {
					// if (Local.FallVelocity > 64 && !g_pGameRules.IsMultiplayer())
					// SoundEnt.InsertSound(SOUND_PLAYER, GetAbsOrigin(), m_Local.m_flFallVelocity, 0.2, this);
					Local.FallVelocity = 0;
				}


				if (IsInAVehicle())
					SetAnimation(PlayerAnim.InVehicle);
				else if (GetAbsVelocity().X == 0 && GetAbsVelocity().Y == 0)
					SetAnimation(PlayerAnim.Idle);
				else if ((GetAbsVelocity().X != 0 || GetAbsVelocity().Y != 0) && (GetFlags() & EntityFlags.OnGround) != 0)
					SetAnimation(PlayerAnim.Walk);
				else if (GetWaterLevel() > (WaterLevel)1)
					SetAnimation(PlayerAnim.Walk);
			}

			if (GetSequence() == -1)
				SetSequence(0);

			// StudioFrameAdvance();
			// DispatchAnimEvents(this);
			SetSimulationTime(gpGlobals.CurTime);
			// Weapon_FrameUpdate();
			// UpdatePlayerSound();

			// if (ForceOrigin) {
			// 	SetLocalOrigin(ForcedOrigin);
			// 	SetLocalAngles(Local.PunchAngle);
			// 	// Local.PunchAngle = RandomAngle(-25, 25);
			// 	Local.PunchAngleVel.Init();
			// }

			// PostThinkVPhysics();
		}

		SimulatePlayerSimulatedEntities();
	}

	public virtual void PreThink() {
		if (g_fGameOver /*|| PlayerLocked*/)
			return;

		// ItemPreFrame();
		WaterMove();

		// if (g_pGameRules && g_pGameRules.FAllowFlashlight())
		// 	Local.HideHUD &= ~HideHudBits.Flashlight;
		// else
		// 	Local.HideHUD |= HideHudBits.Flashlight;

		UpdateClientData();
		// CheckTimeBasedDamage();
		// CheckSuitUpdate();

		// if (GetObserverMode() > Shared.ObserverMode.FreezeCam)
		// 	CheckObserverSettings();

		// if (GetLifeState() >= LifeState.Dying) {
		// 	// UpdateLastKnownArea();
		// 	return;
		// }

		// HandleFuncTrain();

		if ((Buttons & InButtons.Jump) != 0)
			Jump();

		if ((Buttons & InButtons.Duck) != 0 || (GetFlags() & EntityFlags.Ducking) != 0 /*|| (m_afPhysicsFlags & PFLAG_DUCKING)*/)
			Duck();

		if ((GetFlags() & EntityFlags.OnGround) == 0)
			Local.FallVelocity = -GetAbsVelocity().Z;

		// UpdateLastKnownArea();
	}

	bool InitHUD;
	private void UpdateClientData() {
		SingleUserRecipientFilter user = new(this);
		user.MakeReliable();

		if (InitHUD) {
			InitHUD = false;
			// gInitHUD = false;

			UserMessageBegin(user, "ResetHUD");
			WRITE_BYTE(0);
			MessageEnd();

			World? world = GetWorldEntity();
			// todo
		}

		// todo

		// g_pGameRules.UpdateClientData(this);
	}

	private void Duck() {

	}

	private void Jump() {

	}

	internal bool IsDead() {
		throw new NotImplementedException();
	}

	internal void Teleport(AngularImpulse origin, QAngle angles, Vector3 vec3_origin) {
		throw new NotImplementedException();
	}

	public virtual void ForceDropOfCarriedPhysObjects(BaseEntity? ground) { }

	private Action? FnThink;
	internal void Think() => FnThink?.Invoke();
	// todo: thinking about thinking... lots of thinking

	public Activity GetActivity() { return Activity.ACT_IDLE; } // TODO
}

// Something to keep in mind; in base Source, this is stored in a list in the player with value semantics...
// may be worth trying to turn this into a struct one day, review GC usage (? if this is even a problem,
// just happened to notice it)
class CommandContext
{
	public readonly List<UserCmd> Cmds = [];
	public int NumCmds;
	public int TotalCmds;
	public int DroppedPackets;
	public bool Paused;
}

class PlayerSimInfo
{
	public TimeUnit_t Time;
	public int NumCmds;
	public int TicksCorrected;
	public TimeUnit_t FinalSimulationTime;
	public TimeUnit_t GameSimulationTime;
	public TimeUnit_t ServerFrameTime;
	public Vector3 AbsOrigin;
}
