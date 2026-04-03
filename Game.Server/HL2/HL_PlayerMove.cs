global using static Game.Server.HL2.HLPlayerMoveSingletons;

using Game.Shared;
using Game.Shared.HL2;

using Source;
using Source.Common.Mathematics;
using Source.Common.Physics;
using Source.Engine;

using System.Numerics;

namespace Game.Server.HL2;

public static class HLPlayerMoveSingletons
{
	public static HLPlayerMove g_PlayerMove = new();
	public static HLMoveData g_HLMoveData = new();
	public static MoveData g_MoveData = g_HLMoveData;
}

public class HLPlayerMove : PlayerMove
{
	Vector3 SaveOrigin;
	bool WasInVehicle;
	bool VehicleFlipped;
	bool InGodMode;
	bool InNoClip;

	public HLPlayerMove() => SaveOrigin.Init();

	public override void SetupMove(BasePlayer player, ref UserCmd ucmd, IMoveHelper helper, MoveData move) {
		base.SetupMove(player, ref ucmd, helper, move);

		HL2_Player hlPlayer = (HL2_Player)player;
		Assert(hlPlayer);

		HLMoveData hlMove = (HLMoveData)move;
		Assert(hlMove);

		player.ForwardMove = ucmd.ForwardMove;
		player.SideMove = ucmd.SideMove;

		hlMove.IsSprinting = hlPlayer.IsSprinting();

		if (gpGlobals.FrameTime != 0) {
			IServerVehicle? vehicle = player.GetVehicle();

			if (vehicle != null) {
				vehicle.SetupMove(player, ref ucmd, helper, move);

				if (!WasInVehicle) {
					WasInVehicle = true;
					SaveOrigin.Init();
				}
			}
			else {
				SaveOrigin = player.GetAbsOrigin();
				if (WasInVehicle)
					WasInVehicle = false;
			}
		}
	}

	public override void FinishMove(BasePlayer player, ref UserCmd ucmd, MoveData move) {
		base.FinishMove(player, ref ucmd, move);

		if (gpGlobals.FrameTime != 0) {
			float distance = 0.0f;
			IServerVehicle? vehicle = player.GetVehicle();
			if (vehicle != null) {
				vehicle.FinishMove(player, ref ucmd, move);
				IPhysicsObject? obj = player.GetVehicleEntity()?.VPhysicsGetObject();
				if (obj != null) {
					obj.GetPosition(out Vector3 newPos, out _);
					distance = Vector3.Distance(newPos, SaveOrigin);
					if (SaveOrigin == Vector3.Zero || distance > 100.0f)
						distance = 0.0f;
					SaveOrigin = newPos;
				}

				if (player.GetVehicleEntity() is PropVehicleDriveable driveable) {
					// bool flipped = driveable.IsOverturned() && (distance < 0.5f);
					// IsOverturned todo
					bool flipped = false;
					if (VehicleFlipped != flipped) {
						// todo gamestats
						VehicleFlipped = flipped;
					}
				}
				else
					VehicleFlipped = false;
			}
			else {
				VehicleFlipped = false;
				distance = Vector3.Distance(player.GetAbsOrigin(), SaveOrigin);
			}

			// todo gamestats
		}

		bool godMode = (player.GetFlags() & EntityFlags.GodMode) != 0;
		if (InGodMode != godMode) {
			InGodMode = godMode;
			// todo gamestats
		}

		bool noClip = player.GetMoveType() == MoveType.Noclip;
		if (InNoClip != noClip) {
			InNoClip = noClip;
			// todo gamestats
		}
	}
}