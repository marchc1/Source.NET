#if CLIENT_DLL || GAME_DLL
#if CLIENT_DLL
global using BaseCombatWeapon = Game.Client.C_BaseCombatWeapon;
global using WeaponHL2MPBase = Game.Client.C_WeaponHL2MPBase;
global using BaseHL2MPCombatWeapon = Game.Client.C_BaseHL2MPCombatWeapon;
#elif GAME_DLL
global using BaseCombatWeapon = Game.Server.BaseCombatWeapon;
global using WeaponHL2MPBase = Game.Server.WeaponHL2MPBase;
global using BaseHL2MPCombatWeapon = Game.Server.BaseHL2MPCombatWeapon;
#endif

using Source.Common;
using Source;

using Game.Shared;

using Source.Common.Engine;


#if CLIENT_DLL
using Game.Client.HUD;

using System.Diagnostics;
using System.Reflection;

using Microsoft.VisualBasic;
namespace Game.Client;
#else
namespace Game.Server;
#endif

using Table =
#if CLIENT_DLL
	RecvTable;
#else
	SendTable;
#endif

using Class =
#if CLIENT_DLL
	ClientClass;
#else
	ServerClass;
#endif
#if CLIENT_DLL || GAME_DLL
using FIELD = Source.FIELD<BaseCombatWeapon>;
#endif

public partial class
#if CLIENT_DLL
		C_BaseCombatWeapon : C_BaseAnimating
#elif GAME_DLL
	BaseCombatWeapon : BaseAnimating
#else
	SHUT_UP_ABOUT_GAME_SHARED_INTELLISENSE
#endif
{
#if !CLIENT_DLL && !GAME_DLL // God intellisense is annoying me. Fixme when we can get Intellisense to shut up about Game.Shared (it never gets built)
	public static readonly Table DT_BaseAnimating = new();
#endif
	public static readonly Table DT_LocalWeaponData = new([
#if CLIENT_DLL
		RecvPropIntWithMinusOneFlag(FIELD.OF(nameof(Clip1))),
		RecvPropIntWithMinusOneFlag(FIELD.OF(nameof(Clip1))),
		RecvPropInt(FIELD.OF(nameof(PrimaryAmmoType))),
		RecvPropInt(FIELD.OF(nameof(SecondaryAmmoType))),
		RecvPropInt(FIELD.OF(nameof(nViewModelIndex))),
		RecvPropInt(FIELD.OF(nameof(FlipViewModel))),
#elif GAME_DLL
		SendPropIntWithMinusOneFlag(FIELD.OF(nameof(Clip1)), 16),
		SendPropIntWithMinusOneFlag(FIELD.OF(nameof(Clip1)), 16),
		SendPropInt(FIELD.OF(nameof(PrimaryAmmoType)), 8),
		SendPropInt(FIELD.OF(nameof(SecondaryAmmoType)), 8),
		SendPropInt(FIELD.OF(nameof(nViewModelIndex)), BaseViewModel.VIEWMODEL_INDEX_BITS, PropFlags.Unsigned),
		SendPropInt(FIELD.OF(nameof(FlipViewModel)), 8),
#endif
	]); public static readonly Class SC_LocalWeaponData = new Class("LocalWeaponData", DT_LocalWeaponData);

	public static readonly Table DT_LocalActiveWeaponData = new([
#if CLIENT_DLL
		RecvPropTime(FIELD.OF(nameof(NextPrimaryAttack))),
		RecvPropTime(FIELD.OF(nameof(NextSecondaryAttack))),
		RecvPropInt(FIELD.OF(nameof(NextThinkTick))),
		RecvPropTime(FIELD.OF(nameof(TimeWeaponIdle))),
#elif GAME_DLL
		SendPropTime(FIELD.OF(nameof(NextPrimaryAttack))),
		SendPropTime(FIELD.OF(nameof(NextSecondaryAttack))),
		SendPropInt(FIELD.OF(nameof(NextThinkTick))),
		SendPropTime(FIELD.OF(nameof(TimeWeaponIdle))),
#endif
	]); public static readonly Class SC_LocalActiveWeaponData = new Class("LocalActiveWeaponData", DT_LocalActiveWeaponData);

	public static readonly Table DT_BaseCombatWeapon = new(DT_BaseAnimating, [
#if CLIENT_DLL
		RecvPropDataTable("LocalWeaponData", DT_LocalWeaponData),
		RecvPropDataTable("LocalActiveWeaponData", DT_LocalActiveWeaponData),
		RecvPropInt(FIELD.OF(nameof(iViewModelIndex))),
		RecvPropInt(FIELD.OF(nameof(WorldModelIndex))),
		RecvPropInt(FIELD.OF(nameof(State)), 0, RecvProxy_WeaponState),
		RecvPropEHandle(FIELD.OF(nameof(Owner))),
#elif GAME_DLL
		SendPropDataTable("LocalWeaponData", DT_LocalWeaponData, SendProxy_SendLocalWeaponDataTable),
		SendPropDataTable("LocalActiveWeaponData", DT_LocalActiveWeaponData, SendProxy_SendActiveLocalWeaponDataTable ),
		SendPropModelIndex(FIELD.OF(nameof(iViewModelIndex))),
		SendPropModelIndex(FIELD.OF(nameof(WorldModelIndex))),
		SendPropInt(FIELD.OF(nameof(State)), 8, PropFlags.Unsigned),
		SendPropEHandle(FIELD.OF(nameof(Owner))),
#endif
	]);

#if CLIENT_DLL
	private static void RecvProxy_WeaponState(ref readonly RecvProxyData data, object instance, IFieldAccessor field) {
		BaseCombatWeapon weapon = (BaseCombatWeapon)instance;
		weapon.State = data.Value.Int;
		weapon.UpdateVisibility();
	}
#else

	private static object? SendProxy_SendLocalWeaponDataTable(SendProp prop, object instance, IFieldAccessor data, SendProxyRecipients recipients, int objectID) {
		throw new NotImplementedException();
	}
	private static object? SendProxy_SendActiveLocalWeaponDataTable(SendProp prop, object instance, IFieldAccessor data, SendProxyRecipients recipients, int objectID) {
		throw new NotImplementedException();
	}
#endif
	public static readonly new Class
#if CLIENT_DLL
		ClientClass
#else
		ServerClass
#endif
		= new Class("BaseCombatWeapon", DT_BaseCombatWeapon).WithManualClassID(StaticClassIndices.CBaseCombatWeapon);

	public int Clip1;
	public int Clip2;
	public int PrimaryAmmoType;
	public int SecondaryAmmoType;
	// View model index (entity offset)
	public int nViewModelIndex;
	// View model index (art)
	public int iViewModelIndex;
	public int WorldModelIndex;
	public bool FlipViewModel;

	public TimeUnit_t NextPrimaryAttack;
	public TimeUnit_t NextSecondaryAttack;
	public TimeUnit_t TimeWeaponIdle;

	public int State;
	public readonly EHANDLE Owner = new();

	Activity Activity;
	int IdealSequence;
	Activity IdealActivity;
	bool Removable;
	int PrimaryAmmoCount;
	int SecondaryAmmoCount;

	int AltFireHudHintCount;
	int ReloadHudHintCount;
	bool AltFireHudHintDisplayed;
	bool ReloadHudHintDisplayed;
	TimeUnit_t HudHintPollTime;
	TimeUnit_t HudHintMinDisplayTime;

	public virtual bool IsOverridingViewmodel() => false;
	public virtual int DrawOverriddenViewmodel(BaseViewModel viewmodel, StudioFlags flags) => 0;
	public virtual void ViewModelDrawn(BaseViewModel viewmodelflags) { }
	public virtual int UpdateClientData(BasePlayer player) {
		WeaponState iNewState = WeaponState.IsCarriedByPlayer;

		if (player.GetActiveWeapon() == this) {
			if (player.OnTarget) 
				iNewState = WeaponState.IsOnTarget;
			else 
				iNewState = WeaponState.IsActive;
		}
		else 
			iNewState = WeaponState.IsCarriedByPlayer;

		if (State != (int)iNewState) {
			int iOldState = State;
			State = (int)iNewState;
			OnActiveStateChanged((WeaponState)iOldState);
		}

		return 1;
	}
	public virtual void OnActiveStateChanged(WeaponState state){ }

	public virtual bool IsWeaponVisible() {
		BaseViewModel? vm = null;
		BasePlayer? owner = ToBasePlayer(GetOwner());
		if (owner != null) {
			vm = owner.GetViewModel(nViewModelIndex);
			if (vm != null)
				return (!vm.IsEffectActive(EntityEffects.NoDraw));
		}

		return false;
	}

	public virtual bool IsHolstered() {
		return false;
	}

	public int GetPrimaryAmmoCount() => PrimaryAmmoCount;
	public void SetPrimaryAmmoCount(int count) => PrimaryAmmoCount = count;

	public int GetSecondaryAmmoCount() => SecondaryAmmoCount;
	public void SetSecondaryAmmoCount(int count) => SecondaryAmmoCount = count;

	public virtual bool ForceWeaponSwitch() => false;

	public bool HasAnyAmmo() {
		if (!UsesPrimaryAmmo() && !UsesSecondaryAmmo())
			return true;

		return HasPrimaryAmmo() || HasSecondaryAmmo();
	}



	public bool HasPrimaryAmmo() {
		// If I use a clip, and have some ammo in it, then I have ammo
		if (UsesClipsForAmmo1()) {
			if (Clip1 > 0)
				return true;
		}

		// Otherwise, I have ammo if I have some in my ammo counts
		BaseCombatCharacter? owner = GetOwner();
		if (owner != null) {
			if (owner.GetAmmoCount(PrimaryAmmoType) > 0)
				return true;
		}
		else {
			// No owner, so return how much primary ammo I have along with me.
			if (GetPrimaryAmmoCount() > 0)
				return true;
		}

		return false;
	}
	public bool HasSecondaryAmmo() {
		// If I use a clip, and have some ammo in it, then I have ammo
		if (UsesClipsForAmmo2()) {
			if (Clip2 > 0)
				return true;
		}

		// Otherwise, I have ammo if I have some in my ammo counts
		BaseCombatCharacter? owner = GetOwner();
		if (owner != null) {
			if (owner.GetAmmoCount(SecondaryAmmoType) > 0)
				return true;
		}

		return false;
	}

	public virtual bool DefaultDeploy(ReadOnlySpan<char> viewModel, ReadOnlySpan<char> weaponModel, Activity activity, ReadOnlySpan<char> animExt) {
		if (!HasAnyAmmo() && AllowsAutoSwitchFrom())
			return false;

		BasePlayer? owner = ToBasePlayer(GetOwner());
		if (owner != null) {
			// Dead men deploy no weapons
			if (owner.IsAlive() == false)
				return false;

			owner.SetAnimationExtension(animExt);

			SetViewModel();
			SendWeaponAnim(activity);

			owner.SetNextAttack(gpGlobals.CurTime + SequenceDuration());
		}

		// Can't shoot again until we've finished deploying
		NextPrimaryAttack = gpGlobals.CurTime + SequenceDuration();
		NextSecondaryAttack = gpGlobals.CurTime + SequenceDuration();
		HudHintMinDisplayTime = 0;

		AltFireHudHintDisplayed = false;
		ReloadHudHintDisplayed = false;
		HudHintPollTime = gpGlobals.CurTime + 5.0;

		WeaponSound(Shared.WeaponSound.Deploy);

		SetWeaponVisible(true);

		return true;
	}

	public void SetWeaponVisible(bool visible) {
		BaseViewModel? vm = null;

		BasePlayer? owner = ToBasePlayer(GetOwner());
		if (owner != null)
			vm = owner.GetViewModel(nViewModelIndex);

		if (visible) {
			RemoveEffects(EntityEffects.NoDraw);
			vm?.RemoveEffects(EntityEffects.NoDraw);
		}
		else {
			AddEffects(EntityEffects.NoDraw);
			vm?.AddEffects(EntityEffects.NoDraw);
		}
	}

	public int GetPrimaryAmmoType() => PrimaryAmmoType;
	public int GetSecondaryAmmoType() => SecondaryAmmoType;

	public void PoseParameterOverride(bool reset) {
		BaseCombatCharacter? owner = GetOwner();
		if (owner == null)
			return;

		StudioHdr? studioHdr = owner.GetModelPtr();
		if (studioHdr == null)
			return;

		// todo
	}

	public void SetViewModel() {
		BasePlayer? owner = ToBasePlayer(GetOwner());
		if (owner == null)
			return;
		BaseViewModel? vm = owner.GetViewModel(nViewModelIndex, false);
		if (vm == null)
			return;
		Assert(vm.ViewModelIndex() == nViewModelIndex);
		vm.SetWeaponModel(GetViewModel(nViewModelIndex), this);
	}

	public virtual Activity GetDrawActivity() => Activity.ACT_VM_DRAW;
	public Activity GetActivity() => this.Activity;

	public virtual bool CanDeploy() => true;
	public virtual bool CanHolster() => true;

	public virtual bool Deploy() {
		bool bResult = DefaultDeploy(GetViewModel(), GetWorldModel(), GetDrawActivity(), GetAnimPrefix());

		// override pose parameters
		PoseParameterOverride(false);

		return bResult;
	}

	public bool InReload;
	public bool FireOnEmpty;
	public bool FiringWholeClip;

	public virtual bool Holster(BaseCombatWeapon switchingTo) {
		InReload = false;
		FiringWholeClip = false;

		// todo: think function

		// Send holster animation
		SendWeaponAnim(Activity.ACT_VM_HOLSTER);

		// Some weapon's don't have holster anims yet, so detect that
		TimeUnit_t sequenceDuration = 0;
		if (GetActivity() == Activity.ACT_VM_HOLSTER) 
			sequenceDuration = SequenceDuration();
		
		BaseCombatCharacter? owner = GetOwner();
		if (owner != null) 
			owner.SetNextAttack(gpGlobals.CurTime + sequenceDuration);

		// If we don't have a holster anim, hide immediately to avoid timing issues
		if (sequenceDuration == 0) 
			SetWeaponVisible(false);
		// else 
			// Hide the weapon when the holster animation's finished todo
		

		// if we were displaying a hud hint, squelch it.
		if (HudHintMinDisplayTime != 0 && gpGlobals.CurTime < HudHintMinDisplayTime) {
			// if (AltFireHudHintDisplayed)				RescindAltFireHudHint();
			// if (ReloadHudHintDisplayed)				RescindReloadHudHint();
		}

		// reset pose parameters
		PoseParameterOverride(true);

		return true;
	}

	public BaseCombatCharacter? GetOwner() => ToBaseCombatCharacter(Owner.Get());

	public bool SetIdealActivity(Activity ideal) {
		return false; // todo
	}
	public bool SendWeaponAnim(Activity act) {
		return SetIdealActivity(act);
	}
	public void WeaponSound(WeaponSound soundType, TimeUnit_t soundTime = 0.0) {

	}

	public override void Precache() {
		PrimaryAmmoType = SecondaryAmmoType = -1;
		if (WeaponParse.ReadWeaponDataFromFileForSlot(filesystem, GetClassname(), out WeaponFileInfoHandle)) {

		}
	}

	TimeUnit_t NextEmptySoundTime;

	public override void Spawn() {
		Precache();
		base.Spawn();

		SetSolid(SolidType.BBox);
		NextEmptySoundTime = 0.0;

		// Weapons won't show up in trace calls if they are being carried...
		RemoveEFlags(EFL.UsePartitionWhenNotSolid);

		State = (int)WeaponState.NotCarried;
		// Assume 
		nViewModelIndex = 0;

		// GiveDefaultAmmo();

		if (!GetWorldModel() .IsEmpty) 
			SetModel(GetWorldModel());

#if !CLIENT_DLL // todo
		// FallInit();
		// SetCollisionGroup(COLLISION_GROUP_WEAPON);
		// m_takedamage = DAMAGE_EVENTS_ONLY;

		// SetBlocksLOS(false);

		// Default to non-removeable, because we don't want the
		// game_weapon_manager entity to remove weapons that have
		// been hand-placed by level designers. We only want to remove
		// weapons that have been dropped by NPC's.
		// SetRemoveable(false);
#endif

		// Bloat the box for player pickup
		CollisionProp().UseTriggerBounds(true, 36);

		// Use more efficient bbox culling on the client. Otherwise, it'll setup bones for most
		// characters even when they're not in the frustum.
		AddEffects(EntityEffects.BoneMergeFastCull);

		ReloadHudHintCount = 0;
		AltFireHudHintCount = 0;
		HudHintMinDisplayTime = 0;
	}

	public bool VisibleInWeaponSelection() => true;
	public int GetPosition() => GetWpnData().Position;
	public int GetSlot() => GetWpnData().Slot;

	WEAPON_FILE_INFO_HANDLE WeaponFileInfoHandle;
	public WEAPON_FILE_INFO_HANDLE GetWeaponFileInfoHandle() => WeaponFileInfoHandle;

	public FileWeaponInfo GetWpnData() {
		return WeaponParse.GetFileWeaponInfoFromHandle(WeaponFileInfoHandle);
	}

	public ReadOnlySpan<char> GetName() => GetWpnData().ClassName.SliceNullTerminatedString();
	public ReadOnlySpan<char> GetPrintName() => GetWpnData().PrintName.SliceNullTerminatedString();
#if CLIENT_DLL
	public HudTexture GetSpriteActive() => GetWpnData().IconActive;
	public HudTexture GetSpriteInactive() => GetWpnData().IconInactive;
	public HudTexture GetSpriteAmmo() => GetWpnData().IconAmmo;
	public HudTexture GetSpriteAmmo2() => GetWpnData().IconAmmo2;
	public HudTexture GetSpriteCrosshair() => GetWpnData().IconCrosshair;
	public HudTexture GetSpriteAutoaim() => GetWpnData().IconAutoaim;
	public HudTexture GetSpriteZoomedCrosshair() => GetWpnData().IconZoomedCrosshair;
	public HudTexture GetSpriteZoomedAutoaim() => GetWpnData().IconZoomedAutoaim;
#endif

	public WeaponFlags GetWeaponFlags() => (WeaponFlags)GetWpnData().Flags;

	public bool HasAmmo() {
		if (PrimaryAmmoType == -1 && SecondaryAmmoType == -1)
			return true;
		if ((GetWeaponFlags() & WeaponFlags.SelectionEmpty) != 0)
			return true;

		BasePlayer? player = ToBasePlayer(GetOwner());
		if (player == null)
			return false;
		return (Clip1 > 0 || player.GetAmmoCount(PrimaryAmmoType) != 0 || Clip2 > 0 || player.GetAmmoCount(SecondaryAmmoType) != 0);
	}

	public bool CanBeSelected() {
		if (!VisibleInWeaponSelection())
			return false;

		return HasAmmo();
	}

	public int GetMaxClip1() => GetWpnData().MaxClip1;
	public int GetMaxClip2() => GetWpnData().MaxClip2;
	public int GetDefaultClip1() => GetWpnData().DefaultClip1;
	public int GetDefaultClip2() => GetWpnData().DefaultClip2;
	public bool IsMeleeWeapon() => GetWpnData().MeleeWeapon;

	public int SubType;
	public int GetSubType() => SubType;
	public void SetSubType(int subtype) => SubType = subtype;

	public bool UsesClipsForAmmo1() => GetMaxClip1() != WEAPON_NOCLIP;
	public bool UsesClipsForAmmo2() => GetMaxClip2() != WEAPON_NOCLIP;
	public int GetWeight() => GetWpnData().Weight;
	public bool AllowsAutoSwitchTo() => GetWpnData().AutoSwitchTo;
	public bool AllowsAutoSwitchFrom() => GetWpnData().AutoSwitchFrom;
	public bool UsesPrimaryAmmo() => PrimaryAmmoType >= 0;
	public bool UsesSecondaryAmmo() => SecondaryAmmoType >= 0;
	public ReadOnlySpan<char> GetViewModel(int _ = 0) => GetWpnData().ViewModel;
	public ReadOnlySpan<char> GetWorldModel() => GetWpnData().WorldModel;
	public ReadOnlySpan<char> GetAnimPrefix() => GetWpnData().AnimationPrefix;
}
#endif
