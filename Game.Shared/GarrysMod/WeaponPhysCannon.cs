#if (CLIENT_DLL || GAME_DLL) && GMOD_DLL
using Source.Common;
using Source.Common.Mathematics;

using System.Numerics;
namespace Game.Shared.GarrysMod;
using FIELD = Source.FIELD<WeaponPhysCannon>;
public class WeaponPhysCannon : BaseHL2MPCombatWeapon
{
	public static readonly
#if CLIENT_DLL
		RecvTable
#else
		SendTable
#endif
		DT_WeaponPhysCannon = new(DT_BaseHL2MPCombatWeapon, [
#if CLIENT_DLL
			RecvPropBool(FIELD.OF(nameof(Active))),
			RecvPropEHandle(FIELD.OF(nameof(AttachedObject))),
			RecvPropVector(FIELD.OF(nameof(AttachedPositionObjectSpace))),
			RecvPropFloat(FIELD.OF_VECTORELEM(nameof(AttachedAnglesPlayerSpace), 0)),
			RecvPropFloat(FIELD.OF_VECTORELEM(nameof(AttachedAnglesPlayerSpace), 1)),
			RecvPropFloat(FIELD.OF_VECTORELEM(nameof(AttachedAnglesPlayerSpace), 2)),
			RecvPropInt(FIELD.OF(nameof(EffectState))),
			RecvPropBool(FIELD.OF(nameof(Open))),
			RecvPropBool(FIELD.OF(nameof(PhyscannonState))),
#else
			SendPropBool(FIELD.OF(nameof(Active))),
			SendPropEHandle(FIELD.OF(nameof(AttachedObject))),
			SendPropVector(FIELD.OF(nameof(AttachedPositionObjectSpace)), 0, PropFlags.Coord),
			SendPropFloat(FIELD.OF_VECTORELEM(nameof(AttachedAnglesPlayerSpace), 0), 11, PropFlags.RoundDown),
			SendPropFloat(FIELD.OF_VECTORELEM(nameof(AttachedAnglesPlayerSpace), 1), 11, PropFlags.RoundDown),
			SendPropFloat(FIELD.OF_VECTORELEM(nameof(AttachedAnglesPlayerSpace), 2), 11, PropFlags.RoundDown),
			SendPropInt(FIELD.OF(nameof(EffectState))),
			SendPropBool(FIELD.OF(nameof(Open))),
			SendPropBool(FIELD.OF(nameof(PhyscannonState))),
#endif
		]);
#if CLIENT_DLL
	public static readonly new ClientClass ClientClass = new ClientClass("WeaponPhysCannon", null, null, DT_WeaponPhysCannon).WithManualClassID(StaticClassIndices.CWeaponPhysCannon);
#else
	public static readonly new ServerClass ServerClass = new ServerClass("WeaponPhysCannon", DT_WeaponPhysCannon).WithManualClassID(StaticClassIndices.CWeaponPhysCannon);
#endif
	public bool Active;
	public readonly EHANDLE AttachedObject = new();
	public Vector3 AttachedPositionObjectSpace;
	public QAngle AttachedAnglesPlayerSpace;
	public int EffectState;
	public bool Open;
	public bool PhyscannonState;

	public void OpenElements() {
		if (Open)
			return;
		// WeaponSound() todo

		BasePlayer? owner = ToBasePlayer(GetOwner());
		if (owner == null)
			return;

		SendWeaponAnim(Activity.ACT_VM_IDLE);
		Open = true;
		// DoEffect() todo
	}
	public void CloseElements() {
		if (!Open)
			return;
		// WeaponSound() todo

		BasePlayer? owner = ToBasePlayer(GetOwner());
		if (owner == null)
			return;

		SendWeaponAnim(Activity.ACT_VM_IDLE);
		Open = false;
		// DoEffect() todo
	}
}
#endif
