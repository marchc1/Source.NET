using Game.Shared;

using Source;
using Source.Common;

using FIELD = Source.FIELD<Game.Client.C_BaseCombatCharacter>;

namespace Game.Client;
public partial class C_BaseCombatCharacter : C_BaseFlex
{
	public static readonly RecvTable DT_BCCLocalPlayerExclusive = new([
		RecvPropTime(FIELD.OF(nameof(NextAttack))),
	]);
	public override bool IsBaseCombatCharacter() => true;
	public static readonly ClientClass CC_BCCLocalPlayerExclusive = new ClientClass("BCCLocalPlayerExclusive", null, null, DT_BCCLocalPlayerExclusive);

	public static readonly RecvTable DT_BaseCombatCharacter = new(DT_BaseFlex, [
		RecvPropDataTable( "bcc_localdata", DT_BCCLocalPlayerExclusive ),
		RecvPropEHandle(FIELD.OF(nameof(ActiveWeapon))),
		RecvPropArray3(FIELD.OF_ARRAY(nameof(MyWeapons)), RecvPropEHandle( FIELD.OF_ARRAY(nameof(MyWeapons)))),
		RecvPropInt(FIELD.OF(nameof(BloodColor)))
	]);
	public static readonly new ClientClass ClientClass = new ClientClass("BaseCombatCharacter", null, null, DT_BaseCombatCharacter).WithManualClassID(StaticClassIndices.CBaseCombatCharacter);

	public C_BaseCombatWeapon? GetWeapon(int i) => MyWeapons[i].Get();

	public double NextAttack;
	public readonly Handle<C_BaseCombatWeapon> ActiveWeapon = new();
	public InlineArrayNewMaxWeapons<Handle<C_BaseCombatWeapon>> MyWeapons = new();
	public InlineArrayMaxAmmoSlots<int> Ammo;
	public Color BloodColor;
}
