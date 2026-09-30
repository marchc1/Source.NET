using Game.Server;
using Game.Shared;

using Source.Common;
using Source.Common.GUI;


namespace Game.Server;

using Source;
using Source.Common.Commands;
using Source.Common.Formats.BSP;
using Source.Common.Mathematics;
using Source.Common.Physics;

using System.Diagnostics;
using System.Numerics;

using FIELD_BPD = Source.FIELD<BasePropDoor>;
using FIELD_DP = Source.FIELD<DynamicProp>;
using FIELD_PBM = Source.FIELD<PhysBoxMultiplayer>;
using FIELD_PP = Source.FIELD<PhysicsProp>;
using FIELD_PPM = Source.FIELD<PhysicsPropMultiplayer>;

public class BaseProp : BaseAnimating
{
	public override void Spawn() {
		ReadOnlySpan<char> szModel = GetModelName();
		if (szModel.IsEmpty) {
			Vector3 org = GetAbsOrigin();
			Warning($"prop at {org.X:0} {org.Y:0} {org.Z:0} missing modelname\n");
			Util.Remove(this);
			return;
		}

		PrecacheModel(szModel);
		Precache();
		SetModel(szModel);

		// Load this prop's data from the propdata file
		ParsePropData();

		SetMoveType(Source.MoveType.Push);
		m_takedamage = (byte)Damage.No;
		SetNextThink(TICK_NEVER_THINK);

		AnimTime = gpGlobals.CurTime;
		PlaybackRate = 0.0;
		SetCycle(0);
	}
	public override void Precache() { }
	public override void Activate() { }
	public void CalculateBlockLOS() { }
	public void ParsePropData() { }
	public virtual new bool IsAlive() => false;
	public virtual bool OverridePropdata() => true;
}

[NetworkName("CBreakableProp")]
public class BreakableProp : BaseProp
{
	public static readonly SendTable DT_BreakableProp = new(DT_BaseAnimating, []);
	public static readonly new ServerClass ServerClass = new ServerClass(DT_BreakableProp);
}

[LinkEntityToClass("func_physbox_multiplayer")]
[NetworkName("CPhysBoxMultiplayer")]
public partial class PhysBoxMultiplayer : PhysBox, IMultiplayerPhysics
{
	public static readonly SendTable DT_PhysBoxMultiplayer = new(DT_PhysBox, [
		SendPropInt(NetworkVarFields.PhysicsMode, 1, PropFlags.Unsigned),
		SendPropFloat(NetworkVarFields.Mass, 0, PropFlags.NoScale)
	]);
	public static readonly new ServerClass ServerClass = new ServerClass(DT_PhysBoxMultiplayer);
	[NetworkName("m_iPhysicsMode")]
	[NetworkVar] public partial int PhysicsMode { get; set; }
	[NetworkName("m_fMass")]
	[NetworkVar] public partial float Mass { get; set; }
}

[LinkEntityToClass("physics_prop")]
[LinkEntityToClass("prop_physics")]
[LinkEntityToClass("prop_physics_override")]
[NetworkName("CPhysicsProp")]
public partial class PhysicsProp : BreakableProp
{
	public static readonly SendTable DT_PhysicsProp = new(DT_BreakableProp, [
		SendPropBool(NetworkVarFields.Awake)
	]);
	public static readonly new ServerClass ServerClass = new ServerClass(DT_PhysicsProp);
	[NetworkName("m_bAwake")]
	[NetworkVar] public partial bool Awake { get; set; }

	public override void Spawn() {
		// Condense classnames to one, except for "prop_physics_override"
		if (FClassnameIs(this, "physics_prop"))
			SetClassname("prop_physics");

		base.Spawn();

		if (IsMarkedForDeletion())
			return;

		// Now condense all classnames to one
		if (FClassnameIs(this, "prop_physics_override"))
			SetClassname("prop_physics");

		CreateVPhysics();
	}

	public virtual bool CreateVPhysics() {
		SetSolid(SolidType.VPhysics);

		if (m_takedamage == (byte)Damage.No)
			SetMoveType(Source.MoveType.None);

		IPhysicsObject? physObj = VPhysicsInitNormal(SolidType.VPhysics, GetSolidFlags(), false);
		if (physObj == null) {
			SetSolid(SolidType.None);
			SetMoveType(Source.MoveType.None);
			Warning($"ERROR!: Can't create physics object for {GetModelName()}\n");
			return false;
		}

		return true;
	}
}

[LinkEntityToClass("dynamic_prop")]
[LinkEntityToClass("prop_dynamic")]
[LinkEntityToClass("prop_dynamic_override")]
[NetworkName("CDynamicProp")]
public partial class DynamicProp : BreakableProp
{
	public static readonly SendTable DT_DynamicProp = new(DT_BreakableProp, [
		SendPropBool(NetworkVarFields.UseHitboxesForRenderBox)
	]);
	public static readonly new ServerClass ServerClass = new ServerClass(DT_DynamicProp);
	[NetworkName("m_bUseHitboxesForRenderBox")]
	[NetworkVar] public partial bool UseHitboxesForRenderBox { get; set; }
}

[LinkEntityToClass("prop_physics_multiplayer")]
[NetworkName("CPhysicsPropMultiplayer")]
public partial class PhysicsPropMultiplayer : PhysicsProp
{
	public static readonly SendTable DT_PhysicsPropMultiplayer = new(DT_PhysicsProp, [
		SendPropInt(NetworkVarFields.PhysicsMode, 2, PropFlags.Unsigned),
		SendPropFloat(NetworkVarFields.Mass, 0, PropFlags.NoScale),
		SendPropVector(NetworkVarFields.CollisionMins, 0, PropFlags.NoScale),
		SendPropVector(NetworkVarFields.CollisionMaxs, 0, PropFlags.NoScale),
	]);
	public static readonly new ServerClass ServerClass = new ServerClass(DT_PhysicsPropMultiplayer);

	[NetworkName("m_iPhysicsMode")]
	[NetworkVar] public partial int PhysicsMode { get; set; }
	[NetworkName("m_fMass")]
	[NetworkVar] public partial float Mass { get; set; }
	[NetworkName("m_collisionMins")]
	[NetworkVar] public partial Vector3 CollisionMins { get; set; }
	[NetworkName("m_collisionMaxs")]
	[NetworkVar] public partial Vector3 CollisionMaxs { get; set; }
}


[NetworkName("CBasePropDoor")]
public partial class BasePropDoor : DynamicProp
{
	[NetworkName("m_bLocked")]
	[NetworkVar] public partial bool Locked { get; set; }
	[NetworkName("m_eDoorState")]
	[NetworkVar] public partial int DoorState { get; set; }
	public static readonly SendTable DT_BasePropDoor = new(DT_DynamicProp, [
		SendPropBool(NetworkVarFields.Locked),
		SendPropInt(NetworkVarFields.DoorState, 3, PropFlags.Unsigned)
	]);
	public static readonly new ServerClass ServerClass = new ServerClass(DT_BasePropDoor);
}

[LinkEntityToClass("prop_door_rotating")]
[NetworkName("CPropDoorRotating")]
public class PropDoorRotating : BasePropDoor
{
	public static readonly SendTable DT_PropDoorRotating = new(DT_BasePropDoor, []);
	public static readonly new ServerClass ServerClass = new ServerClass(DT_PropDoorRotating);
}

public static class Props
{
	public static PhysicsProp? CreatePhysicsProp(ReadOnlySpan<char> modelName, in Vector3 traceStart, in Vector3 traceEnd, IHandleEntity? traceIgnore, bool requireVCollide, ReadOnlySpan<char> className = default) {
		if (className.IsStringEmpty)
			className = "physics_prop";
		MDLHandle_t h = mdlcache.FindMDL(modelName);
		if (h == MDLHANDLE_INVALID)
			return null;

		// Must have vphysics to place as a physics prop
		StudioHeader? studioHdr = mdlcache.GetStudioHdr(h);
		if (studioHdr == null)
			return null;

		// Must have vphysics to place as a physics prop
		if (requireVCollide && null == mdlcache.GetVCollide(h))
			return null;

		QAngle angles = new(0.0f, 0.0f, 0.0f);
		Vector3 vecSweepMins = studioHdr.HullMin;
		Vector3 vecSweepMaxs = studioHdr.HullMax;

		Util.TraceHull(traceStart, traceEnd, vecSweepMins, vecSweepMaxs, Mask.NPCSolid, traceIgnore, CollisionGroup.None, out GameTrace tr);

		// No hit? We're done.
		if ((tr.Fraction == 1.0 && (traceEnd - traceStart).Length() > 0.01) || tr.AllSolid) 
			return null;

		MathLib.VectorMA(tr.EndPos, 1.0f, tr.Plane.Normal, out tr.EndPos);

		bool allowPrecache = BaseEntity.IsPrecacheAllowed();
		BaseEntity.SetAllowPrecache(true);

		// Try to create entity
		PhysicsProp? prop = (PhysicsProp?)CreateEntityByName(className);
		if (prop != null) {
			Span<char> buf = stackalloc char[512];
			// Pass in standard key values
			prop.KeyValue("origin", sprintf(buf, "%f %f %f").F(tr.EndPos.X).F(tr.EndPos.Y).F(tr.EndPos.Z).ToSpan());
			prop.KeyValue("angles", sprintf(buf, "%f %f %f").F(angles.X).F(angles.Y).F(angles.Z).ToSpan());
			prop.KeyValue("model", modelName);
			prop.KeyValue("fademindist", "-1");
			prop.KeyValue("fademaxdist", "0");
			prop.KeyValue("fadescale", "1");
			prop.KeyValue("inertiaScale", "1.0");
			prop.KeyValue("physdamagescale", "0.1");
			prop.Precache();
			Util.DispatchSpawn(prop);
			prop.Activate();
		}
		BaseEntity.SetAllowPrecache(allowPrecache);

		return prop;
	}

	[ConCommand("prop_physics_create", "Creates a physics prop with a specific .mdl aimed away from where the player is looking.\n\tArguments: {.mdl name}", FCvar.Cheat)]
	public static void CC_Prop_Physics_Create(in TokenizedCommand args) {
		if (args.ArgC() != 2)
			return;

		Span<char> modelName = stackalloc char[512];
		if (!args[1].StartsWith("models/") && !args[1].StartsWith("models\\"))
			sprintf(modelName, "models/%s").S(args[1]);
		else
			strcpy(modelName, args[1]);
		StrTools.DefaultExtension(modelName, ".mdl");

		BasePlayer? player = Util.GetCommandClient();
		if (player == null)
			return;

		player.EyeVectors(out Vector3 forward);
		CreatePhysicsProp(modelName, player.EyePosition(), player.EyePosition() + forward * MAX_TRACE_LENGTH, player, true);
	}
}
