using Game.Shared;

using Source.Common.Commands;
using Source.Common.GUI;
using Source.Common.Mathematics;

using System.Numerics;

namespace Source.Engine;

public class PlayerMove
{
	static readonly ConVar sv_maxusrcmdprocessticks_warning = new("-1", FCvar.None, "Print a warning when user commands get dropped due to insufficient usrcmd ticks allocated, number of seconds to throttle, negative disabled");
	static readonly ConVar sv_maxusrcmdprocessticks_holdaim = new("1", FCvar.Cheat, "Hold client aim for multiple server sim ticks when client-issued usrcmd contains multiple actions (0: off; 1: hold this server tick; 2+: hold multiple ticks)");

	void StartCommand(BasePlayer player, UserCmd cmd) {
		PredictableId.ResetInstanceCounters();

		// player.CurrentCommand = cmd;
		BaseEntity.SetPredictionRandomSeed(cmd);
		BaseEntity.SetPredictionPlayer(player);

#if HL2_DLL
		// todo
#endif
	}

	void FinishCommand(BasePlayer player) {
		player.CurrentCommand = AnonymousSafeFieldPointer<UserCmd>.Null;
		BaseEntity.SetPredictionRandomSeed(default);
		BaseEntity.SetPredictionPlayer(null);
	}

	void CheckMovingGround(BasePlayer player, double frametime) {
		BaseEntity? groundEntity;

		if ((player.GetFlags() & EntityFlags.OnGround) != 0) {
			groundEntity = player.GetGroundEntity();
			if (groundEntity != null && (groundEntity.GetFlags() & EntityFlags.Conveyor) != 0) {
				// Vector3 newVelocity = groundEntity.GetGroundVelocityToApply(); TODO
				Vector3 newVelocity = vec3_origin;
				if ((player.GetFlags() & EntityFlags.BaseVelocity) != 0)
					newVelocity += player.GetBaseVelocity();

				player.SetBaseVelocity(newVelocity);
				player.AddFlag(EntityFlags.BaseVelocity);
			}
		}

		if ((player.GetFlags() & EntityFlags.BaseVelocity) == 0) {
			// player.ApplyAbsVelocityImpulse((1.0f + (float)(frametime * 0.5)) * player.GetBaseVelocity()); TODO
			player.SetBaseVelocity(vec3_origin);
		}

		player.RemoveFlag(EntityFlags.BaseVelocity);
	}

	public virtual void SetupMove(BasePlayer player, UserCmd ucmd, IMoveHelper pHelper, MoveData move) {
		move.FirstRunOfFunctions = true;
		move.GameCodeMovedPlayer = false;

		// if (player.GetPreviouslyPredictedOrigin() != player.GetAbsOrigin())
		// 	move.GameCodeMovedPlayer = true;

		move.ImpulseCommand = ucmd.Impulse;
		move.ViewAngles = ucmd.ViewAngles;

		BaseEntity? moveParent = player.GetMoveParent();
		if (moveParent == null)
			move.AbsViewAngles = move.ViewAngles;
		else {
			Matrix3x4 viewToParent = default, viewToWorld = default;
			MathLib.ConcatTransforms(moveParent.EntityToWorldTransform(), viewToParent, out viewToWorld);
			MathLib.MatrixAngles(viewToWorld, out move.AbsViewAngles);
		}

		move.Buttons = ucmd.Buttons;

		if ((player.GetFlags() & EntityFlags.AtControls) != 0) {
			move.ForwardMove = 0;
			move.SideMove = 0;
			move.UpMove = 0;
		}
		else {
			move.ForwardMove = ucmd.ForwardMove;
			move.SideMove = ucmd.SideMove;
			move.UpMove = ucmd.UpMove;
		}

		move.ClientMaxSpeed = player.MaxSpeed();
		move.OldButtons = (InButtons)player.Local.OldButtons;
		move.OldForwardMove = player.Local.OldForwardMove;
		move.Angles = player.pl.ViewingAngle;

		move.Velocity = player.GetAbsVelocity();

		// move.PlayerHandle = player;

		move.SetAbsOrigin(player.GetAbsOrigin());

		if (player.ConstraintEntity.Get() != null)
			move.ConstraintCenter = player.ConstraintEntity.Get()!.GetAbsOrigin();
		// else
		// 	move.ConstraintCenter = player.ConstraintEntity; TODO

		// move.ConstraintRadius = player.ConstraintRadius;
		// move.ConstraintWidth = player.ConstraintWidth;
		// move.ConstraintSpeedFactor = player.ConstraintSpeedFactor;
	}

	public virtual void FinishMove(BasePlayer player, UserCmd ucmd, MoveData move) {
		// player.SetAbsOrigin(move.GetAbsOrigin());
		player.SetAbsVelocity(move.Velocity);
		// player.SetPreviouslyPredictedOrigin(move.GetAbsOrigin());

		player.Local.OldButtons = (int)move.Buttons;

		float pitch = move.Angles.X;
		if (pitch > 180.0f)
			pitch -= 360.0f;

		pitch = Math.Clamp(pitch, -90.0f, 90.0f);
		move.Angles.X = pitch;

		// player.SetBodyPitch(pitch);
		player.SetLocalAngles(move.Angles);

		if (player.ConstraintEntity.Get() != null)
			Assert(move.ConstraintCenter == player.ConstraintEntity.Get()!.GetAbsOrigin());
		// else
		// 	Assert(move.ConstraintCenter == player.ConstraintEntity); // todo

		// Assert(move.ConstraintRadius == player.ConstraintRadius);
		// Assert(move.ConstraintWidth == player.ConstraintWidth);
		// Assert(move.ConstraintSpeedFactor == player.ConstraintSpeedFactor);
	}

	void RunPreThink(BasePlayer player) {
		if (!player.PhysicsRunThink())
			return;

		// g_pGameRules.PlayerThink(player);

		// player.PreThink();
	}

	void RunThink(BasePlayer player, double frametime) {
		int thinkTick = (int)player.GetNextThinkTick();

		if (thinkTick <= 0 || thinkTick > player.TickBase)
			return;

		player.SetNextThink(TICK_NEVER_THINK);

		// player.Think();
	}

	void RunPostThink(BasePlayer player) {
		// player.PostThink();
	}

	public void RunCommand(BasePlayer player, UserCmd ucmd, IMoveHelper moveHelper) {

	}
}