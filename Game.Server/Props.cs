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

public class BreakableProp : BaseProp
{
	public static readonly SendTable DT_BreakableProp = new(DT_BaseAnimating, []);
	public static readonly new ServerClass ServerClass = new ServerClass("BreakableProp", DT_BreakableProp).WithManualClassID(StaticClassIndices.CBreakableProp);
}

[LinkEntityToClass("func_physbox_multiplayer")]
public class PhysBoxMultiplayer : PhysBox, IMultiplayerPhysics
{
	public static readonly SendTable DT_PhysBoxMultiplayer = new(DT_PhysBox, [
		SendPropInt(FIELD_PBM.OF(nameof(PhysicsMode)), -1, PropFlags.Unsigned),
		SendPropFloat(FIELD_PBM.OF(nameof(Mass)), 0, PropFlags.NoScale)
	]);
	public static readonly new ServerClass ServerClass = new ServerClass("PhysBoxMultiplayer", DT_PhysBoxMultiplayer).WithManualClassID(StaticClassIndices.CPhysBoxMultiplayer);
	public int PhysicsMode;
	public float Mass;
}

[LinkEntityToClass("physics_prop")]
[LinkEntityToClass("prop_physics")]
[LinkEntityToClass("prop_physics_override")]
public class PhysicsProp : BreakableProp
{
	public static readonly SendTable DT_PhysicsProp = new(DT_BreakableProp, [
		SendPropBool(FIELD_PP.OF(nameof(Awake)))
	]);
	public static readonly new ServerClass ServerClass = new ServerClass("PhysicsProp", DT_PhysicsProp).WithManualClassID(StaticClassIndices.CPhysicsProp);
	public bool Awake;

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
public class DynamicProp : BreakableProp
{
	public static readonly SendTable DT_DynamicProp = new(DT_BreakableProp, [
		SendPropBool(FIELD_DP.OF(nameof(UseHitboxesForRenderBox)))
	]);
	public static readonly new ServerClass ServerClass = new ServerClass("DynamicProp", DT_DynamicProp).WithManualClassID(StaticClassIndices.CDynamicProp);
	public bool UseHitboxesForRenderBox;
}

[LinkEntityToClass("prop_physics_multiplayer")]
public class PhysicsPropMultiplayer : PhysicsProp
{
	public static readonly SendTable DT_PhysicsPropMultiplayer = new(DT_PhysicsProp, [
		SendPropInt(FIELD_PPM.OF(nameof(PhysicsMode)), 2, PropFlags.Unsigned),
		SendPropFloat(FIELD_PPM.OF(nameof(Mass)), 0, PropFlags.NoScale),
		SendPropVector(FIELD_PPM.OF(nameof(CollisionMins)), 0, PropFlags.NoScale),
		SendPropVector(FIELD_PPM.OF(nameof(CollisionMaxs)), 0, PropFlags.NoScale),
	]);
	public static readonly new ServerClass ServerClass = new ServerClass("PhysicsPropMultiplayer", DT_PhysicsPropMultiplayer).WithManualClassID(StaticClassIndices.CPhysicsPropMultiplayer);

	public int PhysicsMode;
	public float Mass;
	public Vector3 CollisionMins;
	public Vector3 CollisionMaxs;
}


public class BasePropDoor : DynamicProp
{
	bool Locked;
	int DoorState;
	public static readonly SendTable DT_BasePropDoor = new(DT_DynamicProp, [
		SendPropBool(FIELD_BPD.OF(nameof(Locked))),
		SendPropInt(FIELD_BPD.OF(nameof(DoorState)), 3, PropFlags.Unsigned)
	]);
	public static readonly new ServerClass ServerClass = new ServerClass("BasePropDoor", DT_BasePropDoor).WithManualClassID(StaticClassIndices.CBasePropDoor);
}

[LinkEntityToClass("prop_door_rotating")]
public class PropDoorRotating : BasePropDoor
{
	public static readonly SendTable DT_PropDoorRotating = new(DT_BasePropDoor, []);
	public static readonly new ServerClass ServerClass = new ServerClass("PropDoorRotating", DT_PropDoorRotating).WithManualClassID(StaticClassIndices.CPropDoorRotating);
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
