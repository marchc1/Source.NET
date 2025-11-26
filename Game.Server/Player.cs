using Game.Shared;

using Source;
using Source.Common;
using Source.Common.Engine;

using System.Numerics;

namespace Game.Server;
using FIELD = Source.FIELD<BasePlayer>;

public partial class BasePlayer : BaseCombatCharacter
{
	public static readonly SendTable DT_PlayerState = new([
		SendPropInt(FIELD.OF(nameof(DeadFlag)), 1, PropFlags.Unsigned)
	]); public static readonly ServerClass CC_PlayerState = new("PlayerState", DT_PlayerState);
	public override bool IsPlayer() => true;
	public BaseViewModel GetViewModel(int index) => throw new NotImplementedException();
	
	public static readonly SendTable DT_LocalPlayerExclusive = new([
		SendPropDataTable(nameof(Local), PlayerLocalData.DT_Local),
		SendPropFloat(FIELD.OF(nameof(Friction)), 0, PropFlags.NoScale | PropFlags.RoundDown, 0.0f, 4.0f),
		SendPropArray3(FIELD.OF_ARRAY(nameof(Ammo)), SendPropInt( FIELD.OF_ARRAYINDEX(nameof(Ammo)), 16, PropFlags.Unsigned)),
		SendPropInt(FIELD.OF(nameof(OnTarget)), 2, PropFlags.Unsigned),
		SendPropInt(FIELD.OF(nameof(TickBase)), -1, PropFlags.ChangesOften),
		SendPropInt(FIELD.OF(nameof(NextThinkTick))),
		SendPropEHandle(FIELD.OF(nameof(LastWeapon))),
		SendPropEHandle(FIELD.OF(nameof(GroundEntity)), PropFlags.ChangesOften),
		SendPropVector(FIELD.OF(nameof(BaseVelocity)), 0, PropFlags.NoScale),
		SendPropEHandle(FIELD.OF(nameof(ConstraintEntity))),
		SendPropVector(FIELD.OF(nameof(ConstraintCenter)), 0, PropFlags.NoScale),
		SendPropFloat(FIELD.OF(nameof(ConstraintRadius)), 0, PropFlags.NoScale),
		SendPropFloat(FIELD.OF(nameof(ConstraintWidth)), 0, PropFlags.NoScale),
		SendPropFloat(FIELD.OF(nameof(ConstraintSpeedFactor)), 0, PropFlags.NoScale),
		SendPropFloat(FIELD.OF(nameof(DeathTime)), 0, PropFlags.NoScale),
		SendPropFloat(FIELD.OF(nameof(LaggedMovementValue)), 0, PropFlags.NoScale),
		SendPropEHandle(FIELD.OF(nameof(TonemapController))),
		SendPropEHandle(FIELD.OF(nameof(ViewEntity))),
		SendPropBool(FIELD.OF(nameof(DisableWorldClicking))),
	]); public static readonly ServerClass PlayerExclusive = new("LocalPlayerExclusive", DT_LocalPlayerExclusive);

	public static readonly SendTable DT_BasePlayer = new(DT_BaseCombatCharacter, [
		SendPropDataTable(nameof(pl), DT_PlayerState, SendProxy_DataTableToDataTable),
		SendPropEHandle(FIELD.OF(nameof(Vehicle))),
		SendPropEHandle(FIELD.OF(nameof(UseEntity))),
		SendPropInt(FIELD.OF(nameof(LifeState)), 3, PropFlags.Unsigned ),
		SendPropEHandle(FIELD.OF(nameof(ColorCorrectionCtrl))), // << gmod specific
		SendPropFloat(FIELD.OF(nameof(MaxSpeed)), 12, PropFlags.RoundDown, 0.0f, 2048.0f ),
		SendPropInt(FIELD.OF(nameof(Flags)), Constants.PLAYER_FLAG_BITS, PropFlags.Unsigned|PropFlags.ChangesOften, SendProxy_CropFlagsToPlayerFlagBitsLength),
		SendPropInt(FIELD.OF(nameof(ObserverMode)), 3, PropFlags.Unsigned),
		SendPropEHandle(FIELD.OF(nameof(ObserverTarget))),
		SendPropInt(FIELD.OF(nameof(FOV)), 8, PropFlags.Unsigned),
		SendPropInt(FIELD.OF(nameof(FOVStart)), 8, PropFlags.Unsigned),
		SendPropFloat(FIELD.OF(nameof(FOVTime)), 0, PropFlags.NoScale),
		SendPropFloat(FIELD.OF(nameof(DefaultFOV)), 8, PropFlags.Unsigned),
		SendPropEHandle(FIELD.OF(nameof(ZoomOwner))),

		SendPropEHandle(FIELD.OF_ARRAYINDEX(nameof(ViewModel), 0)),
		SendPropArray(FIELD.OF_ARRAY(nameof(ViewModel))),

		SendPropString(FIELD.OF(nameof(LastPlaceName))),
		SendPropBool(FIELD.OF(nameof(UseWeaponsInVehicle))),
		SendPropDataTable( "localdata", DT_LocalPlayerExclusive, SendProxy_SendLocalDataTable),
	]);

	
	public static void SendProxy_CropFlagsToPlayerFlagBitsLength(SendProp prop, object instance, IFieldAccessor field, ref DVariant outData, int element, int objectID) {
		throw new NotImplementedException();
	}

	public static object? SendProxy_SendLocalDataTable(SendProp prop, object instance, IFieldAccessor data, SendProxyRecipients recipients, int objectID) {
		throw new NotImplementedException();
	}
	public static object? SendProxy_SendNonLocalDataTable(SendProp prop, object instance, IFieldAccessor data, SendProxyRecipients recipients, int objectID) {
		throw new NotImplementedException();
	}

	public static readonly new ServerClass ServerClass = new ServerClass("BasePlayer", DT_BasePlayer);

	bool DeadFlag;
	readonly PlayerState pl = new();
	readonly PlayerLocalData Local = new();
	readonly EHANDLE Vehicle = new();
	readonly EHANDLE UseEntity = new();
	readonly EHANDLE ObserverTarget = new();
	readonly EHANDLE ZoomOwner = new();
	readonly EHANDLE ConstraintEntity = new();
	readonly EHANDLE TonemapController = new();
	readonly EHANDLE ViewEntity = new();
	InlineArrayNewMaxViewmodels<EHANDLE> ViewModel = new(); 
	bool DisableWorldClicking;
	float MaxSpeed;
	int Flags;
	int ObserverMode;
	int FOV;
	int TickBase;
	int FOVStart;
	float FOVTime;
	float DefaultFOV;
	Vector3 ConstraintCenter;
	float ConstraintRadius;
	float ConstraintWidth;
	float ConstraintSpeedFactor;
	InlineArray18<char> LastPlaceName;
	readonly EHANDLE ColorCorrectionCtrl = new();
	bool UseWeaponsInVehicle;
	public bool OnTarget;
	public double DeathTime;
	public double LaggedMovementValue;
	readonly EHANDLE LastWeapon = new();

	public bool IsInAVehicle() => Vehicle.Get() != null;
}
