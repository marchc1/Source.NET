#if (CLIENT_DLL || GAME_DLL) && GMOD_DLL

#if CLIENT_DLL
using Game.Client;
#endif

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
#if CLIENT_DLL
	public bool OldOpen;
	public int OldEffectState;
	public readonly InterpolatedValue ElementParameter = new();
#endif
	public void OpenElements() {
		if (Open)
			return;
		WeaponSound(Shared.WeaponSound.Special1);

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
		WeaponSound(Shared.WeaponSound.MeleeHit);

		BasePlayer? owner = ToBasePlayer(GetOwner());
		if (owner == null)
			return;

		SendWeaponAnim(Activity.ACT_VM_IDLE);
		Open = false;
		// DoEffect() todo
	}
#if CLIENT_DLL
	public override void ClientThink() {
		UpdateElementPosition();
	}
	public void UpdateElementPosition() {
		BasePlayer? owner = ToBasePlayer(GetOwner());
		float elementPosition = ElementParameter.Interp(gpGlobals.CurTime);

		if (ShouldDrawUsingViewModel()) {
			if(owner != null) {
				BaseViewModel? vm = owner.GetViewModel();
				if (vm != null)
					vm.SetPoseParameter("active", elementPosition);
			}
		}
	}
	public override void OnDataChanged(DataUpdateType type) {
		base.OnDataChanged(type);

		if (type == DataUpdateType.Created) {
			SetNextClientThink(CLIENT_THINK_ALWAYS);
		}

		// Update effect state when out of parity with the server
		if (OldEffectState != EffectState) {
			// DoEffect(EffectState);
			OldEffectState = EffectState;
		}

		// Update element state when out of parity
		if (OldOpen != Open) {
			if (Open) 
				ElementParameter.InitFromCurrent(1.0f, 0.2f, InterpType.Spline);
			else 
				ElementParameter.InitFromCurrent(0.0f, 0.5f, InterpType.Spline);

			OldOpen = (bool)Open;
		}
	}
#endif

}
#endif
