using Game.Shared;

using Source;
using Source.Common;
using Source.Common.Engine;

using System.Numerics;

namespace Game.Server;

using FIELD = Source.FIELD<BaseCombatCharacter>;

public partial class BaseCombatCharacter : BaseFlex
{
	public static readonly SendTable DT_BCCLocalPlayerExclusive = new([
		SendPropTime(FIELD.OF(nameof(NextAttack))),
	]);
	public static readonly ServerClass CC_BCCLocalPlayerExclusive = new ServerClass("BCCLocalPlayerExclusive", DT_BCCLocalPlayerExclusive);

	public static readonly SendTable DT_BaseCombatCharacter = new(DT_BaseFlex, [
		SendPropDataTable( "bcc_localdata", DT_BCCLocalPlayerExclusive, SendProxy_SendBaseCombatCharacterLocalDataTable ),
		SendPropEHandle(FIELD.OF(nameof(ActiveWeapon))),
		SendPropArray3(FIELD.OF_ARRAY(nameof(MyWeapons)), SendPropEHandle( FIELD.OF_ARRAY(nameof(MyWeapons)))),
		SendPropInt(FIELD.OF(nameof(BloodColor)), 32, 0)
	]);

	public TimeUnit_t GetNextAttack() => NextAttack;
	public void SetNextAttack(TimeUnit_t wait) => NextAttack = wait;

	public TimeUnit_t NextAttack;
	public readonly Handle<BaseCombatWeapon> LastWeapon = new();
	public readonly Handle<BaseCombatWeapon> ActiveWeapon = new();
	public InlineArrayNewMaxWeapons<Handle<BaseCombatWeapon>> MyWeapons = new();
	[NetworkArraySize(MAX_AMMO_TYPES)] public readonly NetworkArray<int> Ammo = new(MAX_AMMO_TYPES);
	public Color BloodColor;

	private static object? SendProxy_SendBaseCombatCharacterLocalDataTable(SendProp prop, object instance, IFieldAccessor data, SendProxyRecipients recipients, int objectID) {
		throw new NotImplementedException();
	}

	public static readonly new ServerClass ServerClass = new ServerClass("BaseCombatCharacter", DT_BaseCombatCharacter).WithManualClassID(StaticClassIndices.CBaseCombatCharacter);

	public override void DoMuzzleFlash() {
		BaseCombatWeapon? weapon = GetActiveWeapon();
		if (weapon != null)
			weapon.DoMuzzleFlash();
		else
			base.DoMuzzleFlash();
	}

	WeaponProficiency CurrentWeaponProficiency;

	public WeaponProficiency GetCurrentWeaponProficiency() => CurrentWeaponProficiency;

	public Vector3 GetAttackSpread(BaseCombatWeapon? weapon, BaseEntity? target = null) {
		if (weapon != null)
			return weapon.GetBulletSpread(GetCurrentWeaponProficiency());
		return VECTOR_CONE_15DEGREES;
	}
}
