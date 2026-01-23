#if CLIENT_DLL || GAME_DLL

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
		public SharedBaseEntity? GetGroundEntity() => GroundEntity.Get();

		public void PhysicsCheckForEntityUntouch() {
			// todo

			// SetCheckUntouch(false);
		}

		public virtual void PhysicsSimulate() {
			if (SimulationTick == gpGlobals.TickCount)
				return;

			SimulationTick = gpGlobals.TickCount;

			Assert(!IsPlayer());
			SharedBaseEntity? moveParent = GetMoveParent();

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

	}
}
#endif
