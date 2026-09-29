using Game.Shared;

using Source;
using Source.Common;
using Source.Common.Engine;

using System.Numerics;

namespace Game.Server;

using FIELD = Source.FIELD<BaseCombatCharacter>;

[NetworkName("CBaseCombatCharacter")]
public partial class BaseCombatCharacter : BaseFlex
{
	public bool ForceServerRagdoll;

	public virtual Source.Common.Mathematics.QAngle BodyAngles() => GetAbsAngles();

	public virtual Vector3 BodyDirection2D() {
		Vector3 bodyDir = BodyDirection3D();
		bodyDir.Z = 0;
		float len = MathF.Sqrt(bodyDir.X * bodyDir.X + bodyDir.Y * bodyDir.Y);
		if (len != 0) {
			bodyDir.X /= len;
			bodyDir.Y /= len;
		}
		return bodyDir;
	}

	public virtual Vector3 BodyDirection3D() {
		Source.Common.Mathematics.QAngle angles = BodyAngles();

		// FIXME: cache this
		Source.Common.Mathematics.MathLib.AngleVectors(angles, out Vector3 bodyDir);
		return bodyDir;
	}

	public virtual Vector3 HeadDirection3D() => BodyDirection2D(); // No head motion so just return body dir
	public virtual Vector3 EyeDirection3D() => HeadDirection3D(); // No eye motion so just return head dir

	public static readonly SendTable DT_BCCLocalPlayerExclusive = new(nameof(DT_BCCLocalPlayerExclusive), [
		SendPropTime64(FIELD.OF(nameof(NextAttack))),
	]);

	public static readonly SendTable DT_BaseCombatCharacter = new(DT_BaseFlex, [
		SendPropDataTable( "bcc_localdata", DT_BCCLocalPlayerExclusive, SendProxy_SendBaseCombatCharacterLocalDataTable ),
		SendPropEHandle(FIELD.OF(nameof(ActiveWeapon))),
		SendPropArray3(FIELD.OF_ARRAY(nameof(MyWeapons)), SendPropEHandle( FIELD.OF_ARRAY(nameof(MyWeapons)))),
		SendPropInt(FIELD.OF(nameof(BloodColor)), 5, 0)
	]);

	public TimeUnit_t GetNextAttack() => NextAttack;
	public void SetNextAttack(TimeUnit_t wait) => NextAttack = wait;

	[NetworkName("m_flNextAttack")]
	public TimeUnit_t NextAttack;
	public float ImpactEnergyScale;
	[NetworkName("m_hLastWeapon")]
	public Handle<BaseCombatWeapon> LastWeapon = new();
	[NetworkName("m_hActiveWeapon")]
	public Handle<BaseCombatWeapon> ActiveWeapon = new();
	[NetworkName("m_hMyWeapons")]
	public InlineArrayNewMaxWeapons<Handle<BaseCombatWeapon>> MyWeapons = new();
	[NetworkName("m_iAmmo")]
	[NetworkArraySize(MAX_AMMO_TYPES)] public readonly NetworkArray<int> Ammo = new(MAX_AMMO_TYPES);
	[NetworkName("m_bloodColor")]
	public Color BloodColor;

	private static object? SendProxy_SendBaseCombatCharacterLocalDataTable(SendProp prop, object instance, IFieldAccessor data, SendProxyRecipients recipients, int objectID) {
		recipients.ClearAllRecipients();

		BaseCombatCharacter character = (BaseCombatCharacter)instance;
		if (character != null) {
			if (character.IsPlayer())
				recipients.SetOnly(character.EntIndex() - 1);
			else {
				IServerVehicle vehicle = character.GetServerVehicle();
				if (vehicle != null) {
					BaseCombatCharacter driver = vehicle.GetPassenger();
					if (driver != null)
						recipients.SetOnly(driver.EntIndex() - 1);
				}
			}
		}

		return instance;
	}
	public void ClearLastKnownArea() {
		// TODO
	}

	public int WeaponCount() => MAX_WEAPONS;
	public BaseCombatWeapon? GetWeapon(int i) => MyWeapons[i].Get();

	public static readonly new ServerClass ServerClass = new ServerClass(DT_BaseCombatCharacter);

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
