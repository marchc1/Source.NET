using Game.Shared;

using Source;
using Source.Common;
using Source.Common.Engine;

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

	public double NextAttack;
	public readonly Handle<BaseCombatWeapon> LastWeapon = new();
	public readonly Handle<BaseCombatWeapon> ActiveWeapon = new();
	public InlineArrayNewMaxWeapons<Handle<BaseCombatWeapon>> MyWeapons = new();
	public InlineArrayMaxAmmoSlots<int> Ammo;
	public Color BloodColor;

	private static object? SendProxy_SendBaseCombatCharacterLocalDataTable(SendProp prop, object instance, IFieldAccessor data, SendProxyRecipients recipients, int objectID) {
		throw new NotImplementedException();
	}

	public static readonly new ServerClass ServerClass = new ServerClass("BaseCombatCharacter", DT_BaseCombatCharacter).WithManualClassID(StaticClassIndices.CBaseCombatCharacter);
}
