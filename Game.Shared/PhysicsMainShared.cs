#if CLIENT_DLL || GAME_DLL

global using static Game.Shared.DataObjectAccessSystemGlobals;

using Game.Shared;

using Source.Common.Mathematics;
using Source.Common.Physics;

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Text;

namespace Game.Shared
{
}

// Define physics methods for base entity
#if CLIENT_DLL
namespace Game.Client
#else
namespace Game.Server
#endif
{
	public partial class
#if CLIENT_DLL
		C_BaseEntity
#else
		BaseEntity
#endif
	{
		public BaseEntity? GetGroundEntity() => GroundEntity.Get();

		public void PhysicsCheckForEntityUntouch() {
			// todo

			// SetCheckUntouch(false);
		}

		public bool HasDataObjectType(DataObjectType type) => (DataObjectTypes & (1 << (int)type)) != 0;
		public void AddDataObjectType(DataObjectType type) => DataObjectTypes |= (1 << (int)type);
		public void RemoveDataObjectType(DataObjectType type) => DataObjectTypes &= ~(1 << (int)type);

		public ref T GetDataObject<T>(DataObjectType type) where T : new() {
			if (!HasDataObjectType(type))
				return ref Unsafe.NullRef<T>();
			return ref g_DataObjectAccessSystem.GetDataObject<T>(type, this);
		}

		public ref T CreateDataObject<T>(DataObjectType type) where T : new() {
			AddDataObjectType(type);
			return ref g_DataObjectAccessSystem.CreateDataObject<T>(type, this);
		}

		public void DestroyDataObject(DataObjectType type) {
			if (!HasDataObjectType(type))
				return;
			RemoveDataObjectType(type);
			g_DataObjectAccessSystem.DestroyDataObject(type, this);
		}

		public void DestroyAllDataObjects() {
			for (DataObjectType i = 0; i < DataObjectType.NumTypes; i++)
				if (HasDataObjectType(i))
					DestroyDataObject(i);
		}
		static Trace g_TouchTrace;

		public void PhysicsImpact(BaseEntity? other, in Trace trace) {
			if (other == null)
				return;
			// If either of the entities is flagged to be deleted, 
			//  don't call the touch functions
			if (((GetFlags() | other.GetFlags()) & Source.EntityFlags.KillMe) != 0)
				return;

			PhysicsMarkEntitiesAsTouching(other, trace);
		}

		private void PhysicsMarkEntityAsTouched(BaseEntity other) {
			//todo
		}

		private void PhysicsMarkEntitiesAsTouching(BaseEntity other, in Trace trace) {
			g_TouchTrace = trace;
			PhysicsMarkEntityAsTouched(other);
			other.PhysicsMarkEntityAsTouched(this);
		}

		public GroundLink? AddEntityToGroundList(BaseEntity? other) {
			return null; // TODO
		}

		public static void PhysicsNotifyOtherOfGroundRemoval(BaseEntity? ent, BaseEntity? other) {
			// TODO
		}

		public void SetGroundEntity(BaseEntity? ground) {
			if (GroundEntity.Get() == ground)
				return;

#if GAME_DLL
			// this can happen in-between updates to the held object controller (physcannon, +USE)
			// so trap it here and release held objects when they become player ground
			if (ground != null && IsPlayer() && ground.GetMoveType() == Source.MoveType.VPhysics) {
				BasePlayer? player = ToBasePlayer(this);
				IPhysicsObject? physGround = ground.VPhysicsGetObject();
				if (physGround != null && player != null)
					if ((physGround.GetGameFlags() & PhysicsFlags.PlayerHeld) != 0)
						player.ForceDropOfCarriedPhysObjects(ground);
			}
#endif

			BaseEntity? oldGround = GroundEntity.Get();
			GroundEntity.Set(ground);

			// Just starting to touch
			if (oldGround == null && ground != null)
				ground.AddEntityToGroundList(this);
			// Just stopping touching
			else if (oldGround != null && ground == null)
				PhysicsNotifyOtherOfGroundRemoval(this, oldGround);
			// Changing out to new ground entity
			else {
				PhysicsNotifyOtherOfGroundRemoval(this, oldGround);
				ground!.AddEntityToGroundList(this);
			}

			// HACK/PARANOID:  This is redundant with the code above, but in case we get out of sync groundlist entries ever, 
			//  this will force the appropriate flags
			if (ground != null)
				AddFlag(Source.EntityFlags.OnGround);
			else
				RemoveFlag(Source.EntityFlags.OnGround);
		}

		public virtual void PhysicsSimulate() {
			if (SimulationTick == gpGlobals.TickCount)
				return;

			SimulationTick = gpGlobals.TickCount;

			Assert(!IsPlayer());
			BaseEntity? moveParent = GetMoveParent();

			if ((GetMoveType() == Source.MoveType.None && moveParent == null) || (GetMoveType() == Source.MoveType.VPhysics)) {
				PhysicsNone();
				return;
			}

			// If ground entity goes away, make sure FL_ONGROUND is valid
			if (GetGroundEntity() == null)
				RemoveFlag(Source.EntityFlags.OnGround);

			if (moveParent != null) {
				moveParent.PhysicsSimulate();
			}
			else {
				UpdateBaseVelocity();

				if (((GetFlags() & Source.EntityFlags.BaseVelocity) == 0) && (GetBaseVelocity() != vec3_origin)) {
					// Apply momentum (add in half of the previous frame of velocity first)
					// BUGBUG: This will break with PhysicsStep() because of the timestep difference
					MathLib.VectorMA(GetAbsVelocity(), 1.0f + (float)(gpGlobals.FrameTime * 0.5), GetBaseVelocity(), out Vector3 absVelocity);
					SetAbsVelocity(absVelocity);
					SetBaseVelocity(vec3_origin);
				}
				RemoveFlag(Source.EntityFlags.BaseVelocity);
			}

			switch (GetMoveType()) {
				case Source.MoveType.Push:
					PhysicsPusher();
					break;
				case Source.MoveType.VPhysics:
					break;
				case Source.MoveType.None:
					Assert(moveParent != null);
					PhysicsRigidChild();
					break;
				case Source.MoveType.Noclip:
					PhysicsNoclip();
					break;
				case Source.MoveType.Step:
					PhysicsStep();
					break;
				case Source.MoveType.Fly:
				case Source.MoveType.FlyGravity:
					PhysicsToss();
					break;
				case Source.MoveType.Custom:
					PhysicsCustom();
					break;
				default:
					Warning($"PhysicsSimulate: {GetClassname()} bad movetype {GetMoveType()}");
					Assert(0);
					break;
			}
		}


		public bool PhysicsRunThink(ThinkMethods thinkMethod = ThinkMethods.FireAllFunctions) {
			if (IsEFlagSet(EFL.NoThinkFunction))
				return true;

			// todo
			return true;
		}

		public void UpdateWaterState() {
			// todo
		}

		public void PhysicsCheckVelocity() {
			throw new NotImplementedException();
		}

		private bool PhysicsCheckWater() {
			throw new NotImplementedException();
		}

		public void SimulateAngles(TimeUnit_t frameTime) {
			throw new NotImplementedException();
		}
	}
}

namespace Game.Shared
{
	public enum ThinkMethods
	{
		FireAllFunctions,
		FireBaseOnly,
		FireAllButBase,
	}

	public class DataObjectAccessSystem : AutoGameSystem
	{
		public override bool Init() {

			return true;
		}

		public override void Shutdown() {
			
		}

		readonly IEntityDataInstantiator[] Accessors = new IEntityDataInstantiator[MAX_ACCESSORS];
		// Blank for now
		const int MAX_ACCESSORS = 32;

		bool IsValidType(DataObjectType type) {
			if (type < 0 || (int)type >= MAX_ACCESSORS)
				return false;

			if (Accessors[(int)type] == null)
				return false;
			return true;
		}

		public ref T GetDataObject<T>(DataObjectType type, BaseEntity? instance) where T : new() {
			if (!IsValidType(type)) {
				AssertMsg(false, "Bogus type");
				return ref Unsafe.NullRef<T>();
			}
			return ref Accessors[(int)type].GetDataObject<T>(instance!);
		}
		public ref T CreateDataObject<T>(DataObjectType type, BaseEntity? instance) where T : new() {
			if (!IsValidType(type)) {
				AssertMsg(false, "Bogus type");
				return ref Unsafe.NullRef<T>();
			}
			return ref Accessors[(int)type].CreateDataObject<T>(instance!);
		}
		public void DestroyDataObject(DataObjectType type, BaseEntity? instance) {
			if (!IsValidType(type)) {
				AssertMsg(false, "Bogus type");
				return;
			}

			Accessors[(int)type].DestroyDataObject(instance!);
		}
	}

	public static class DataObjectAccessSystemGlobals
	{
		public static readonly DataObjectAccessSystem g_DataObjectAccessSystem = new();
	}
}
#endif
