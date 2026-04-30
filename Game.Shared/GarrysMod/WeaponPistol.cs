#if (CLIENT_DLL || GAME_DLL) && GMOD_DLL

using Source.Common;
namespace Game.Shared.GarrysMod;

using FIELD = Source.FIELD<WeaponPistol>;

[LinkEntityToClass("weapon_pistol")]
[PrecacheWeaponRegister("weapon_pistol")]
public class WeaponPistol : HL2MPMachineGun
{
	public static readonly
#if CLIENT_DLL
		RecvTable
#else
		SendTable
#endif
		DT_WeaponPistol = new(DT_HL2MPMachineGun, [
#if CLIENT_DLL
			RecvPropTime64(FIELD.OF(nameof(SoonestPrimaryAttack))),
			RecvPropTime64(FIELD.OF(nameof(LastAttackTime))),
			RecvPropFloat(FIELD.OF(nameof(AccuracyPenalty))),
			RecvPropInt(FIELD.OF(nameof(NumShotsFired))),
#else
			SendPropTime64(FIELD.OF(nameof(SoonestPrimaryAttack))),
			SendPropTime64(FIELD.OF(nameof(LastAttackTime))),
			SendPropFloat(FIELD.OF(nameof(AccuracyPenalty)), 0, PropFlags.NoScale),
			SendPropInt(FIELD.OF(nameof(NumShotsFired)), 16),
#endif
		]);
#if CLIENT_DLL
	public static readonly new ClientClass ClientClass = new ClientClass("WeaponPistol", null, null, DT_WeaponPistol).WithManualClassID(StaticClassIndices.CWeaponPistol);
	public static readonly new DataMap PredMap = new([], typeof(WeaponPistol), HL2MPMachineGun.PredMap); public override DataMap? GetPredDescMap() => PredMap;
#else
	public static readonly new ServerClass ServerClass = new ServerClass("WeaponPistol", DT_WeaponPistol).WithManualClassID(StaticClassIndices.CWeaponPistol);
#endif
	public TimeUnit_t SoonestPrimaryAttack;
	public TimeUnit_t LastAttackTime;
	public TimeUnit_t AccuracyPenalty;
	public int NumShotsFired;
	public override float GetFireRate() => 0.5f;
	public const TimeUnit_t PISTOL_FASTEST_REFIRE_TIME = 0.1;
	public const TimeUnit_t PISTOL_FASTEST_DRY_REFIRE_TIME = 0.2;
	public const TimeUnit_t PISTOL_ACCURACY_SHOT_PENALTY_TIME = 0.2;// Applied amount of time each shot adds to the time we must recover from
	public const TimeUnit_t PISTOL_ACCURACY_MAXIMUM_PENALTY_TIME = 1.5;// Maximum penalty to deal out

	public override void ItemPostFrame() {
		base.ItemPostFrame();

		if (InReload)
			return;

		BasePlayer? owner = ToBasePlayer(GetOwner());

		if (owner == null)
			return;

		if ((owner.Buttons & InButtons.Attack2) != 0) {
			LastAttackTime = gpGlobals.CurTime + PISTOL_FASTEST_REFIRE_TIME;
			SoonestPrimaryAttack = gpGlobals.CurTime + PISTOL_FASTEST_REFIRE_TIME;
			NextPrimaryAttack = gpGlobals.CurTime + PISTOL_FASTEST_REFIRE_TIME;
		}

		//Allow a refire as fast as the player can click
		if (((owner.Buttons & InButtons.Attack) == 0) && (SoonestPrimaryAttack < gpGlobals.CurTime))
			NextPrimaryAttack = gpGlobals.CurTime - 0.1;
		else if ((owner.Buttons & InButtons.Attack) != 0 && (NextPrimaryAttack < gpGlobals.CurTime) && (iClip1 <= 0))
			DryFire();
	}

	public void DryFire() {
		WeaponSound(Shared.WeaponSound.Empty);
		SendWeaponAnim(Activity.ACT_VM_DRYFIRE);

		SoonestPrimaryAttack = gpGlobals.CurTime + PISTOL_FASTEST_DRY_REFIRE_TIME;
		NextPrimaryAttack = gpGlobals.CurTime + SequenceDuration();
	}
}
#endif
