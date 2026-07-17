#if CLIENT_DLL || GAME_DLL

#if CLIENT_DLL
global using static Game.Client.BasePlayerGlobals;

global using BasePlayer = Game.Client.C_BasePlayer;
global using RecipientFilter = Game.Client.C_RecipientFilter;

#else
global using static Game.Server.BasePlayerGlobals;

global using BasePlayer = Game.Server.BasePlayer;

#endif
using Source.Common.Mathematics;

using Game.Shared;

using System.Numerics;

#if CLIENT_DLL
namespace Game.Client;
#else
namespace Game.Server;
#endif

using Source.Common.Commands;
using Source.Common.Physics;
using Source;
using Source.Common;
using Source.Common.Audio;
using Source.Common.Formats.BSP;
using Source.Common.SoundEmitterSystem;

using System.Runtime.CompilerServices;

using Game.Shared.GarrysMod;

public static class BasePlayerGlobals
{
	public static BasePlayer? ToBasePlayer(BaseEntity? entity) {
		if (entity == null || !entity.IsPlayer())
			return null;

		return (BasePlayer?)entity;
	}

	public static BaseCombatCharacter? ToBaseCombatCharacter(BaseEntity? entity) {
		if (entity == null || !entity.IsBaseCombatCharacter())
			return null;

		return (BaseCombatCharacter?)entity;
	}

	public const TimeUnit_t DEATH_ANIMATION_TIME = 3.0f;

	public enum StepSoundTimes
	{
		Normal = 0,
		OnLadder,
		WaterKnee,
		WaterFoot
	}
}

public partial class
#if CLIENT_DLL
	C_BasePlayer
#elif GAME_DLL
	BasePlayer
#endif
{
	public void SharedSpawn() {
		SetMoveType(Source.MoveType.Walk);
		SetSolid(SolidType.BBox);
		AddSolidFlags(SolidFlags.NotStandable);
		Friction = 1.0f;

		pl.DeadFlag = false;
		LifeState = (int)Source.LifeState.Alive;
		Health = 100;
		TakeDamage = 2; // DAMAGE_YES

		Local.DrawViewmodel = true;
		Local.StepSize = sv_stepsize.GetFloat();
		Local.AllowAutoMovement = true;

		RenderFX = (byte)RenderFx.None;
		SetNextAttack(gpGlobals.CurTime);
		Maxspeed = 0.0f;

		SetSequence(SelectWeightedSequence(Activity.ACT_IDLE));

		if ((GetFlags() & EntityFlags.Ducking) != 0)
			SetCollisionBounds(VEC_DUCK_HULL_MIN, VEC_DUCK_HULL_MAX);
		else
			SetCollisionBounds(VEC_HULL_MIN, VEC_HULL_MAX);

		Local.FallVelocity = 0;

		// SetBloodColor(BLOOD_COLOR_RED);
	}

	public TimeUnit_t GetTimeBase() => TickBase * TICK_INTERVAL;
	public virtual void CalcView(ref Vector3 eyeOrigin, ref QAngle eyeAngles, ref float zNear, ref float zFar, ref float fov) {
		CalcPlayerView(ref eyeOrigin, ref eyeAngles, ref fov); // << TODO: There is a lot more logic here for observers, vehicles, etc!
	}

	public override Vector3 EyePosition() {
		return base.EyePosition();
	}

	public ref readonly QAngle LocalEyeAngles() => ref pl.ViewingAngle;

	static QAngle angEyeWorld;
	public override ref readonly QAngle EyeAngles() {
		// NOTE: Viewangles are measured *relative* to the parent's coordinate system
		BaseEntity? pMoveParent = null; //this.GetMoveParent();

		if (pMoveParent == null)
			return ref pl.ViewingAngle;

		// FIXME: Cache off the angles?
		Matrix3x4 eyesToParent = default, eyesToWorld = default;
		MathLib.AngleMatrix(pl.ViewingAngle, out eyesToParent);
		MathLib.ConcatTransforms(pMoveParent.EntityToWorldTransform(), eyesToParent, out eyesToWorld);

		MathLib.MatrixAngles(in eyesToWorld, out angEyeWorld);
		return ref angEyeWorld;
	}

	public virtual void CalcViewModelView(in Vector3 eyeOrigin, in QAngle eyeAngles) {
		for (int i = 0; i < MAX_VIEWMODELS; i++) {
			BaseViewModel? vm = GetViewModel(i);
			if (vm == null)
				continue;

			vm.CalcViewModelView(this, eyeOrigin, eyeAngles);
		}
	}
	public void CalcViewRoll(ref QAngle angles) {
		// todo
	}
	private void CalcPlayerView(ref Vector3 eyeOrigin, ref QAngle eyeAngles, ref float fov) {
		eyeOrigin = EyePosition();
		eyeAngles = EyeAngles();

		Vector3 vecBaseEyePosition = eyeOrigin;

		CalcViewRoll(ref eyeAngles);
		eyeAngles += Local.PunchAngle;

#if CLIENT_DLL
		if (!prediction.InPrediction()) {
			vieweffects.CalcShake();
			vieweffects.ApplyShake(ref eyeOrigin, ref eyeAngles, 1.0f);
		}
#endif

#if CLIENT_DLL
		GetPredictionErrorSmoothingVector(out Vector3 smoothOffset);
		eyeOrigin += smoothOffset;
#endif
	}

	public InlineArrayMaxPlayerNameLength<char> Netname;

	public ReadOnlySpan<char> GetPlayerName() {
		return ((Span<char>)Netname).SliceNullTerminatedString();
	}
	public void SetPlayerName(ReadOnlySpan<char> name) {
		strcpy(Netname, name);
	}

	static ConVar sv_suppress_viewpunch = new("sv_suppress_viewpunch", "0", FCvar.Replicated | FCvar.Cheat | FCvar.DevelopmentOnly);
	public const int PLAY_PLAYER_JINGLE = 1;
	public const int UPDATE_PLAYER_RADAR = 2;
	public void SelectItem(ReadOnlySpan<char> str, int subtype) {
		if (str.IsEmpty)
			return;

		BaseCombatWeapon? item = Weapon_OwnsThisType(str, subtype);

		if (item == null)
			return;

		if (GetObserverMode() != Shared.ObserverMode.None)
			return;// Observers can't select things.

		if (!Weapon_ShouldSelectItem(item))
			return;

		// FIX, this needs to queue them up and delay
		// Make sure the current weapon can be holstered
		if (GetActiveWeapon() != null) {
			if (!GetActiveWeapon()!.CanHolster() && !item.ForceWeaponSwitch())
				return;

			ResetAutoaim();
		}

		Weapon_Switch(item);
	}

	public virtual bool Weapon_ShouldSetLast(BaseCombatWeapon? last, BaseCombatWeapon? active) => true;

	public override bool Weapon_Switch(BaseCombatWeapon? weapon, int viewmodelindex = 0) {
		BaseCombatWeapon? lastWeapon = GetActiveWeapon();

		if (base.Weapon_Switch(weapon, viewmodelindex)) {
			if (lastWeapon != null && Weapon_ShouldSetLast(lastWeapon, GetActiveWeapon()))
				Weapon_SetLast(lastWeapon.GetLastWeapon());

			BaseViewModel? pViewModel = GetViewModel(viewmodelindex);
			Assert(pViewModel != null);
			if (pViewModel != null)
				pViewModel.RemoveEffects(EntityEffects.NoDraw);
			ResetAutoaim();
			return true;
		}
		return false;
	}

	public virtual ReadOnlySpan<char> GetOverrideStepSound(ReadOnlySpan<char> baseStepSoundName) => baseStepSoundName;
	public virtual void OnEmitFootstepSound(in SoundParameters parms, in Vector3 origin, float volume) { }

	struct StepSoundCache_t
	{
		public StepSoundCache_t() { SoundNameIndex = 0; }
		public SoundParameters SoundParameters;
		public UtlSymId_t SoundNameIndex;
	}
	StepSoundCache_t[] StepSoundCache = [new(), new()];

	public void PlayStepSound(in Vector3 origin, SurfaceData_ptr? surface, float vol, bool force) {
		if (gpGlobals.MaxClients > 1 && !sv_footsteps.GetBool())
			return;

#if CLIENT_DLL
		if (prediction.InPrediction() && !prediction.IsFirstTimePredicted())
			return;
#endif

		if (surface == null)
			return;

		int side = Local.StepSide;
		UtlSymId_t stepSoundName = side != 0 ? surface.Sounds.StepLeft : surface.Sounds.StepRight;
		if (stepSoundName == 0)
			return;

		Local.StepSide = side != 0 ? 0 : 1;

		SoundParameters parms = new();

		Assert(side == 0 || side == 1);

		if (StepSoundCache[side].SoundNameIndex == stepSoundName)
			parms = StepSoundCache[side].SoundParameters;
		else {
			ReadOnlySpan<char> soundName = MoveHelper().GetSurfaceProps()!.GetString(stepSoundName);

			soundName = GetOverrideStepSound(soundName);

			if (!GetParametersForSound(soundName, ref parms, null))
				return;

			if (parms.Count == 1) {
				StepSoundCache[side].SoundNameIndex = stepSoundName;
				StepSoundCache[side].SoundParameters = parms;
			}
		}

		RecipientFilter filter = new();
		filter.AddRecipientsByPAS(origin);

#if !CLIENT_DLL
		if (gpGlobals.MaxClients > 1)
			filter.RemoveRecipientsByPVS(origin);
#endif

		scoped EmitSound_t ep = new() {
			Channel = (int)SoundEntityChannel.Body,
			SoundName = ((ReadOnlySpan<char>)parms.SoundName).SliceNullTerminatedString(),
			Volume = vol,
			SoundLevel = parms.SoundLevel,
			Flags = 0,
			Pitch = parms.Pitch,
			Origin = ref origin
		};

		EmitSound(filter, EntIndex(), in ep);

		OnEmitFootstepSound(parms, origin, vol);
	}

	public virtual bool Weapon_ShouldSelectItem(BaseCombatWeapon weapon) => weapon != GetActiveWeapon();

	public virtual void UpdateButtonState(InButtons userCmdButtonMask) {
		AfButtonLast = Buttons;
		Buttons = userCmdButtonMask;
		InButtons buttonsChanged = AfButtonLast ^ Buttons;

		// Debounced button codes for pressed/released
		// UNDONE: Do we need auto-repeat?
		AfButtonPressed = buttonsChanged & Buttons;        // The changed ones still down are "pressed"
		AfButtonReleased = buttonsChanged & (~Buttons);    // The ones not down are "released"
	}

	public float GetPlayerMaxSpeed() {
		float speed = Local.WalkSpeed;
		if ((Buttons & InButtons.Walk) != 0)
			speed = Local.SlowWalkSpeed;
		else if ((Buttons & InButtons.Speed) != 0)
			speed = Local.SprintSpeed;

		float maxSpeed = sv_maxspeed.GetFloat();
		if (speed > 0.0f && speed < maxSpeed)
			maxSpeed = speed;

		if (MaxSpeed() > 0.0f && MaxSpeed() < maxSpeed)
			maxSpeed = MaxSpeed();

		return maxSpeed;
	}

	int SkipStep;
	public void UpdateStepSound(SurfaceData_ptr? surface, in Vector3 origin, in Vector3 velocity) {
		bool walking;
		float vol = 0;
		Vector3 knee, feet;
		float height, speed, velrun, velwalk;
		bool ladder;

		if (StepSoundTime > 0) {
			StepSoundTime -= 1000.0f * gpGlobals.FrameTime;
			if (StepSoundTime < 0)
				StepSoundTime = 0;
		}

		if (StepSoundTime > 0)
			return;

		if ((GetFlags() & (EntityFlags.Frozen | EntityFlags.AtControls)) != 0)
			return;

		if (GetMoveType() == Source.MoveType.Noclip || GetMoveType() == Source.MoveType.Observer)
			return;

		if (!sv_footsteps.GetBool())
			return;

		speed = MathLib.VectorLength(Velocity);
		float groundSpeed = MathLib.Vector2DLength(Velocity.AsVector2());

		ladder = GetMoveType() == Source.MoveType.Ladder;

		GetStepSoundVelocities(out velwalk, out velrun);

		bool onground = (GetFlags() & EntityFlags.OnGround) != 0;
		bool movingAlongGround = groundSpeed > 0.0001f;
		bool movingFastEnough = speed >= velwalk;

		if (!movingFastEnough || !(ladder || (onground && movingAlongGround)))
			return;

		walking = speed < velrun;

		MathLib.VectorCopy(Origin, out knee);
		MathLib.VectorCopy(Origin, out feet);

		height = GetPlayerMaxs()[2] - GetPlayerMins()[2];
		knee[2] = Origin[2] + 0.2f * height;

		if (ladder) {
			surface = GetLadderSurface(Origin);
			vol = 0.5f;

			SetStepSoundTime(StepSoundTimes.OnLadder, walking);
		}
		else if (GetWaterLevel() == Shared.WaterLevel.Waist) {

			if (SkipStep == 0) {
				SkipStep++;
				return;
			}

			if (SkipStep++ == 3)
				SkipStep = 0;

			surface = physprops.GetSurfaceData(physprops.GetSurfaceIndex("wade"));
			vol = 0.65f;

			SetStepSoundTime(StepSoundTimes.WaterKnee, walking);
		}
		else if (GetWaterLevel() == Shared.WaterLevel.Feet) {
			surface = physprops.GetSurfaceData(physprops.GetSurfaceIndex("water"));
			vol = walking ? .2f : .5f;
			SetStepSoundTime(StepSoundTimes.WaterKnee, walking);
		}
		else {
			if (surface == null)
				return;

			SetStepSoundTime(StepSoundTimes.Normal, walking);

			switch ((char)surface.Game.Material) {
				default:
				case Decals.CHAR_TEX_CONCRETE:
					vol = walking ? 0.2f : 0.5f;
					break;

				case Decals.CHAR_TEX_METAL:
					vol = walking ? 0.2f : 0.5f;
					break;

				case Decals.CHAR_TEX_DIRT:
					vol = walking ? 0.25f : 0.55f;
					break;

				case Decals.CHAR_TEX_VENT:
					vol = walking ? 0.4f : 0.7f;
					break;

				case Decals.CHAR_TEX_GRATE:
					vol = walking ? 0.2f : 0.5f;
					break;

				case Decals.CHAR_TEX_TILE:
					vol = walking ? 0.2f : 0.5f;
					break;

				case Decals.CHAR_TEX_SLOSH:
					vol = walking ? 0.2f : 0.5f;
					break;
			}

			if ((GetFlags() & EntityFlags.Ducking) != 0)
				vol *= 0.65f;
		}

		PlayStepSound(feet, surface, vol, false);
	}

	private SurfaceData_ptr GetLadderSurface(Vector3 origin) {
#if CLIENT_DLL
		return GetFootstepSurface(origin, "ladder")!;
#else
		return physprops.GetSurfaceData(physprops.GetSurfaceIndex("ladder"))!;
#endif
	}

#if CLIENT_DLL
	private SurfaceData_ptr GetFootstepSurface(Vector3 origin, ReadOnlySpan<char> v) => physprops.GetSurfaceData(physprops.GetSurfaceIndex(v))!;
#endif

	private void GetStepSoundVelocities(out float velwalk, out float velrun) {
		if ((GetFlags() & EntityFlags.Ducking) != 0 || GetMoveType() == Source.MoveType.Ladder) {
			velwalk = 60;
			velrun = 80;
		}
		else {
			velwalk = 90;
			velrun = 220;
		}
	}

	private void SetStepSoundTime(StepSoundTimes stepSoundTime, bool walking) {
		switch (stepSoundTime) {
			case StepSoundTimes.Normal:
			case StepSoundTimes.WaterFoot:
				StepSoundTime = walking ? 400.0f : 300.0f;
				break;
			case StepSoundTimes.OnLadder:
				StepSoundTime = 350.0f;
				break;
			case StepSoundTimes.WaterKnee:
				StepSoundTime = 600.0f;
				break;
			default:
				Assert(false);
				break;
		}

		if ((GetFlags() & EntityFlags.Ducking) != 0 || (GetMoveType() == Source.MoveType.Ladder))
			StepSoundTime += 100.0f;
	}


	Vector3 GetPlayerMins() {
		if (IsObserver())
			return VEC_OBS_HULL_MIN_SCALED(this);
		else {
			if ((GetFlags() & EntityFlags.Ducking) != 0)
				return VEC_DUCK_HULL_MIN_SCALED(this);
			else
				return VEC_HULL_MIN_SCALED(this);
		}
	}

	Vector3 GetPlayerMaxs() {
		if (IsObserver())
			return VEC_OBS_HULL_MAX_SCALED(this);
		else {
			if ((GetFlags() & EntityFlags.Ducking) != 0)
				return VEC_DUCK_HULL_MAX_SCALED(this);
			else
				return VEC_HULL_MAX_SCALED(this);
		}
	}


	public void ViewPunch(in QAngle angleOffset) {
		//See if we're suppressing the view punching
		if (sv_suppress_viewpunch.GetBool())
			return;

		// We don't allow view kicks in the vehicle
		if (IsInAVehicle())
			return;

		Local.PunchAngleVel += angleOffset * 20;
	}

	public void ViewPunchReset(float tolerance = 0) {
		if (tolerance != 0) {
			tolerance *= tolerance; // square
			float check = Local.PunchAngleVel.LengthSqr() + Local.PunchAngle.LengthSqr();
			if (check > tolerance)
				return;
		}
		Local.PunchAngle = vec3_angle;
		Local.PunchAngleVel = vec3_angle;
	}

	void Weapon_SetLast(BaseCombatWeapon? pWeapon) {
		LastWeapon.Set(pWeapon);
	}

	public void SetAnimationExtension(ReadOnlySpan<char> extension) {
		strcpy(AnimExtension, extension);
	}


	public void AddToPlayerSimulationList(BaseEntity other) {
		// Already in list
		foreach (var entry in SimulatedByThisPlayer)
			if (entry.Get() == other) return;

		Assert(other.IsPlayerSimulated());

		Handle<BaseEntity> h = new();
		h.Set(other);
		SimulatedByThisPlayer.Add(h);
	}

	public void RemoveFromPlayerSimulationList(BaseEntity? other) {
		if (other == null)
			return;

		Assert(other.IsPlayerSimulated());
		Assert(other.GetSimulatingPlayer() == this);

		foreach (var entry in SimulatedByThisPlayer)
			if (entry.Get() == other) {
				SimulatedByThisPlayer.Remove(entry);
				return;
			}
	}

	public virtual void ItemPostFrame() {
		// Put viewmodels into basically correct place based on new player origin
		CalcViewModelView(EyePosition(), EyeAngles());

		// Don't process items while in a vehicle.
		if (GetVehicle() != null) {
#if CLIENT_DLL
			IClientVehicle vehicle = GetVehicle()!;
#else
			IServerVehicle vehicle = GetVehicle()!;
#endif

			bool usingStandardWeapons = UsingStandardWeaponsInVehicle();

#if CLIENT_DLL
			if (vehicle.IsPredicted())
#endif
				vehicle.ItemPostFrame(this);

			if (!usingStandardWeapons || GetVehicle() == null)
				return;
		}


		// check if the player is using something
		if (UseEntity.Get() != null) {
#if !CLIENT_DLL
			// Assert(!IsInAVehicle());
			ImpulseCommands();// this will call playerUse
#endif
			return;
		}

		if (gpGlobals.CurTime < NextAttack)
			GetActiveWeapon()?.ItemBusyFrame();
		else {
			if (GetActiveWeapon() != null && (!IsInAVehicle() || UsingStandardWeaponsInVehicle())) {
#if CLIENT_DLL
				// Not predicting this weapon
				if (GetActiveWeapon()!.IsPredicted())
#endif
					GetActiveWeapon()!.ItemPostFrame();
			}
		}

#if !CLIENT_DLL
		ImpulseCommands();
#else
		// NOTE: If we ever support full impulse commands on the client,
		// remove this line and call ImpulseCommands instead.
		Impulse = 0;
#endif
	}

	public void EyeVectors(out Vector3 forward, out Vector3 right, out Vector3 up) {
		if (GetVehicle() != null) {
			// TODO: Cache or retrieve our calculated position in the vehicle
			// CacheVehicleView();
			//AngleVectors(m_vecVehicleViewAngles, pForward, pRight, pUp);
			forward = right = up = default;
		}
		else
			MathLib.AngleVectors(EyeAngles(), out forward, out right, out up);
	}
	[MethodImpl(MethodImplOptions.AggressiveInlining)] public void EyeVectors(out Vector3 forward, out Vector3 right) => EyeVectors(out forward, out right, out _);
	[MethodImpl(MethodImplOptions.AggressiveInlining)] public void EyeVectors(out Vector3 forward) => EyeVectors(out forward, out _, out _);
	public void ClearPlayerSimulationList() {
		int c = SimulatedByThisPlayer.Count;
		int i;

		for (i = c - 1; i >= 0; i--) {
			Handle<BaseEntity> h = SimulatedByThisPlayer[i];
			BaseEntity? e = h.Get();
			e?.UnsetPlayerSimulated();
		}

		SimulatedByThisPlayer.Clear();
	}

	public void SimulatePlayerSimulatedEntities() {
		int c = SimulatedByThisPlayer.Count;
		int i;

		for (i = c - 1; i >= 0; i--) {
			Handle<BaseEntity> h = SimulatedByThisPlayer[i];
			BaseEntity? e = h.Get();

			if (e == null || !e.IsPlayerSimulated()) {
				SimulatedByThisPlayer.RemoveAt(i);
				continue;
			}

#if CLIENT_DLL
			if (e.IsClientCreated() && prediction.InPrediction() && !prediction.IsFirstTimePredicted())
				continue;
#endif
			Assert(e.IsPlayerSimulated());
			Assert(e.GetSimulatingPlayer() == this);

			e.PhysicsSimulate();
		}

		// Loop through all entities again, checking their untouch if flagged to do so
		c = SimulatedByThisPlayer.Count;

		for (i = c - 1; i >= 0; i--) {
			Handle<BaseEntity> h = SimulatedByThisPlayer[i];
			BaseEntity? e = h.Get();

			if (e == null || !e.IsPlayerSimulated()) {
				SimulatedByThisPlayer.RemoveAt(i);
				continue;
			}

#if CLIENT_DLL
			if (e.IsClientCreated() && prediction.InPrediction() && !prediction.IsFirstTimePredicted())
				continue;
#endif

			Assert(e.IsPlayerSimulated());
			Assert(e.GetSimulatingPlayer() == this);

			if (!e.GetCheckUntouch())
				continue;

			PhysicsCheckForEntityUntouch();
		}
	}

	public bool UsingStandardWeaponsInVehicle() {
		Assert(IsInAVehicle());
#if !CLIENT_DLL
		IServerVehicle? vehicle = GetVehicle();
#else
		IClientVehicle? vehicle = GetVehicle();
#endif

		if (vehicle == null)
			return true;

		PassengerRole role = vehicle.GetPassengerRole(this);
		bool bUsingStandardWeapons = vehicle.IsPassengerUsingStandardWeapons(role);

		// Fall through and check weapons, etc. if we're using them 
		if (!bUsingStandardWeapons)
			return false;

		return true;
	}

	private void UpdateUnderwaterState() {
		if (GetWaterLevel() == Shared.WaterLevel.Eyes) {
			if (IsPlayerUnderwater() == false)
				SetPlayerUnderwater(true);
			return;
		}

		if (IsPlayerUnderwater())
			SetPlayerUnderwater(false);

		if (GetWaterLevel() == 0) {
			if ((GetFlags() & EntityFlags.InWater) != 0) {
#if !CLIENT_DLL
				if (Health > 0 && IsAlive())
					EmitSound("Player.Wade");
#endif
				RemoveFlag(EntityFlags.InWater);
			}
		}
		else if ((GetFlags() & EntityFlags.InWater) == 0) {
#if !CLIENT_DLL
			if (GetWaterType() == Contents.Water)
				EmitSound("Player.Wade");
#endif

			AddFlag(EntityFlags.InWater);
		}
	}

	public virtual void SetPlayerUnderwater(bool v) {
		// throw new NotImplementedException();
	}

	public float GetFOVDistanceAdjustFactor() {
		// TODO! GetDefaultFOV/GetFOV
		float defaultFOV = DefaultFOV;
		float localFOV = FOV;

		if (localFOV == defaultFOV || defaultFOV < 0.001f)
			return 1.0f;

		return localFOV / defaultFOV;
	}
}
#endif
