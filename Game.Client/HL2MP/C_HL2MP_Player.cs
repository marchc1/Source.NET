using Game.Client.HL2;
using Game.Shared;

using Source;
using Source.Common;
using Source.Common.Commands;
using Source.Common.Mathematics;

using System.Numerics;

namespace Game.Client.HL2MP;
using FIELD = FIELD<C_HL2MP_Player>;
using FIELD_RD = FIELD<C_HL2MPRagdoll>;

[LinkEntityToClass("player")]
public partial class C_HL2MP_Player : C_BaseHLPlayer
{
	static ConVar cl_playermodel = new("none", FCvar.UserInfo | FCvar.Archive | FCvar.ServerCanExecute, "Default Player Model");
	static ConVar cl_defaultweapon = new("weapon_physcannon", FCvar.UserInfo | FCvar.Archive, "Default Spawn Weapon");


	public static readonly RecvTable DT_HL2MPLocalPlayerExclusive = new([
		RecvPropVector(FIELD.OF_NAMED(nameof(NetworkOrigin), nameof(Origin))),

		RecvPropFloat(FIELD.OF_VECTORELEM(nameof(AngEyeAngles), 0)),
		RecvPropFloat(FIELD.OF_VECTORELEM(nameof(AngEyeAngles), 1)),
	]); public static readonly ClientClass CC_HL2MPLocalPlayerExclusive = new ClientClass("HL2MPLocalPlayerExclusive", null, null, DT_HL2MPLocalPlayerExclusive);

	public static readonly RecvTable DT_HL2MPNonLocalPlayerExclusive = new([
		RecvPropVector(FIELD.OF_NAMED(nameof(NetworkOrigin), nameof(Origin))),

		RecvPropFloat(FIELD.OF_VECTORELEM(nameof(AngEyeAngles), 0)),
		RecvPropFloat(FIELD.OF_VECTORELEM(nameof(AngEyeAngles), 1)),
	]); public static readonly ClientClass CC_HL2MPNonLocalPlayerExclusive = new ClientClass("HL2MPNonLocalPlayerExclusive", null, null, DT_HL2MPNonLocalPlayerExclusive);


	public static readonly RecvTable DT_HL2MP_Player = new(DT_BasePlayer, [
		RecvPropDataTable("hl2mplocaldata", DT_HL2MPLocalPlayerExclusive),
		RecvPropDataTable("hl2mpnonlocaldata", DT_HL2MPNonLocalPlayerExclusive),
		RecvPropEHandle(FIELD.OF(nameof(Ragdoll))),
		RecvPropInt(FIELD.OF(nameof(SpawnInterpCounter))),
		RecvPropInt(FIELD.OF(nameof(PlayerSoundType))),
		RecvPropBool(FIELD.OF(nameof(IsWalking)))
	]);
	public static readonly new ClientClass ClientClass = new ClientClass("HL2MP_Player", null, null, DT_HL2MP_Player)
															.WithManualClassID(StaticClassIndices.CHL2MP_Player);

	public QAngle AngEyeAngles;
	readonly InterpolatedVar<QAngle> IV_AngEyeAngles = new(nameof(AngEyeAngles));
	public EHANDLE Ragdoll = new();
	public int SpawnInterpCounter;
	public int SpawnInterpCounterCache;
	public int PlayerSoundType;
	public bool IsWalking;

	public C_HL2MP_Player() : base() {
		AddVar(FIELD.OF(nameof(AngEyeAngles)), IV_AngEyeAngles, LatchFlags.LatchSimulationVar);
	}

	public override void PostDataUpdate(DataUpdateType updateType) {
	if(SpawnInterpCounter != SpawnInterpCounterCache){
			MoveToLastReceivedPosition(true);
			ResetLatched();
			SpawnInterpCounterCache = SpawnInterpCounter;
	}
		base.PostDataUpdate(updateType);
	}

	public override ref readonly QAngle EyeAngles() {
		if (IsLocalPlayer())
			return ref base.EyeAngles();
		else
			return ref AngEyeAngles;
	}

	public override void CalcView(ref Vector3 eyeOrigin, ref QAngle eyeAngles, ref float zNear, ref float zFar, ref float fov) {
		if ((LifeState)LifeState != Source.LifeState.Alive) {
			
		}
		base.CalcView(ref eyeOrigin, ref eyeAngles, ref zNear, ref zFar, ref fov);
	}
}

public class C_HL2MPRagdoll : C_BaseAnimatingOverlay
{
	public static readonly RecvTable DT_HL2MPRagdoll = new([
		RecvPropVector(FIELD_RD.OF(nameof(RagdollOrigin))),
		RecvPropEHandle(FIELD_RD.OF(nameof(Player))),
		RecvPropInt(FIELD_RD.OF(nameof(ModelIndex))),
		RecvPropInt(FIELD_RD.OF(nameof(ForceBone))),
		RecvPropVector(FIELD_RD.OF(nameof(Force))),
		RecvPropVector(FIELD_RD.OF(nameof(RagdollVelocity)))
	]);
	public static readonly new ClientClass ClientClass = new ClientClass("HL2MPRagdoll", null, null, DT_HL2MPRagdoll).WithManualClassID(StaticClassIndices.CHL2MPRagdoll);

	public Vector3 RagdollOrigin;
	public readonly EHANDLE Player = new();
	public Vector3 RagdollVelocity;
}
