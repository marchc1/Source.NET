#if CLIENT_DLL || GAME_DLL

#if CLIENT_DLL
global using static Game.Client.BasePlayerGlobals;

global using BasePlayer = Game.Client.C_BasePlayer;


#else
global using static Game.Server.BasePlayerGlobals;
global using BasePlayer = Game.Server.BasePlayer;

#endif
using Source.Common.Mathematics;
using Source;
using Game.Shared;
using System.Numerics;

#if CLIENT_DLL
namespace Game.Client;


#else
namespace Game.Server;
#endif

using Source.Common.Commands;
using Source.Common.Physics;

using System.Runtime.CompilerServices;

public static class BasePlayerGlobals
{
	public static BasePlayer? ToBasePlayer(SharedBaseEntity? entity) {
		if (entity == null || !entity.IsPlayer())
			return null;

		return (BasePlayer?)entity;
	}

	public static BaseCombatCharacter? ToBaseCombatCharacter(SharedBaseEntity? entity) {
		if (entity == null || !entity.IsBaseCombatCharacter())
			return null;

		return (BaseCombatCharacter?)entity;
	}

	public const TimeUnit_t DEATH_ANIMATION_TIME = 3.0f;
}

public partial class
#if CLIENT_DLL
	C_BasePlayer
#elif GAME_DLL
	BasePlayer
#endif
{
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
		SharedBaseEntity? pMoveParent = null; //this.GetMoveParent();

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
		if (!prediction.InPrediction()) { } // vieweffects
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
	public void SetPlayerName(ReadOnlySpan<char> name){
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
		float maxSpeed = sv_maxspeed.GetFloat();
		if (MaxSpeed() > 0.0f && MaxSpeed() < maxSpeed)
			maxSpeed = MaxSpeed();
		return maxSpeed;
	}

	public void UpdateStepSound(SurfaceData_ptr surface, in Vector3 origin, in Vector3 velocity) {
		// todo
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
	public void ViewPunchReset(float tolerance) {
		if (tolerance != 0) {
			tolerance *= tolerance; // square
			float check = Local.PunchAngleVel.LengthSqr() + Local.PunchAngle.LengthSqr();
			if (check > tolerance)
				return;
		}
		Local.PunchAngle = vec3_angle;
		Local.PunchAngleVel = vec3_angle;
	}

	void Weapon_SetLast(BaseCombatWeapon pWeapon) {
		throw new NotImplementedException();
	}

	public void SetAnimationExtension(ReadOnlySpan<char> extension) {
		strcpy(AnimExtension, extension);
	}


	public void AddToPlayerSimulationList(SharedBaseEntity other) {
		// Already in list
		foreach (var entry in SimulatedByThisPlayer)
			if (entry.Get() == other) return;

		Assert(other.IsPlayerSimulated());

		Handle<SharedBaseEntity> h = new();
		h.Set(other);
		SimulatedByThisPlayer.Add(h);
	}

	public void RemoveFromPlayerSimulationList(SharedBaseEntity? other) {
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
			Handle<SharedBaseEntity> h = SimulatedByThisPlayer[i];
			SharedBaseEntity? e = h.Get();
			e?.UnsetPlayerSimulated();
		}

		SimulatedByThisPlayer.Clear();
	}

	public void SimulatePlayerSimulatedEntities() {
		int c = SimulatedByThisPlayer.Count;
		int i;

		for (i = c - 1; i >= 0; i--) {
			Handle<SharedBaseEntity> h = SimulatedByThisPlayer[i];
			SharedBaseEntity? e = h.Get();

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
			Handle<SharedBaseEntity> h = SimulatedByThisPlayer[i];
			SharedBaseEntity? e = h.Get();

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
}
#endif
