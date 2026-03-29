#if CLIENT_DLL || GAME_DLL

global using static Game.Shared.DataObjectAccessSystemGlobals;

using Game.Shared;

using Source.Common.Mathematics;

using System;
using System.Collections.Generic;
using System.Numerics;
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

		public object? GetDataObject(DataObjectType type) {
			if (!HasDataObjectType(type))
				return null;
			return g_DataObjectAccessSystem.GetDataObject(type, this);
		}

		public object? CreateDataObject(DataObjectType type) {
			AddDataObjectType(type);
			return g_DataObjectAccessSystem.CreateDataObject(type, this);
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

	public class DataObjectAccessSystem : AutoGameSystem {
		// Blank for now

		public object? GetDataObject(DataObjectType type, BaseEntity? instance) => null;
		public object? CreateDataObject(DataObjectType type, BaseEntity? instance) => null;
		public void DestroyDataObject(DataObjectType type, BaseEntity? instance){ }
	}

	public static class DataObjectAccessSystemGlobals {
		public static readonly DataObjectAccessSystem g_DataObjectAccessSystem = new();
	}
}
#endif
