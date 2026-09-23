using Game.Shared;

using Source;
using Source.Common;
using Source.Common.Commands;
using Source.Common.Engine;
using Source.Common.Formats.BSP;
using Source.Common.Mathematics;

using System.Numerics;

namespace Game.Server.HL2MP;

public static class BotTemp
{
	public static readonly ConVar bot_forcefireweapon = new("bot_forcefireweapon", "", 0, "Force bots with the specified weapon to fire.");
	public static readonly ConVar bot_forceattack2 = new("bot_forceattack2", "0", 0, "When firing, use attack2.");
	public static readonly ConVar bot_forceattackon = new("bot_forceattackon", "0", 0, "When firing, don't tap fire, hold it down.");
	public static readonly ConVar bot_flipout = new("bot_flipout", "0", 0, "When on, all bots fire their guns.");
	public static readonly ConVar bot_defend = new("bot_defend", "0", 0, "Set to a team number, and that team will all keep their combat shields raised.");
	public static readonly ConVar bot_changeclass = new("bot_changeclass", "0", 0, "Force all bots to change to the specified class.");
	public static readonly ConVar bot_zombie = new("bot_zombie", "0", 0, "Brraaaaaiiiins.");
	static readonly ConVar bot_mimic_yaw_offset = new("bot_mimic_yaw_offset", "0", 0, "Offsets the bot yaw.");
	public static readonly ConVar bot_attack = new("bot_attack", "1", 0, "Shoot!");
	public static readonly ConVar bot_sendcmd = new("bot_sendcmd", "", 0, "Forces bots to send the specified command.");
	public static readonly ConVar bot_crouch = new("bot_crouch", "0", 0, "Bot crouches");
	public static readonly ConVar bot_mimic = new("bot_mimic", "0", 0, "Bot uses usercmd of player by index.");

	static int BotNumber = 1;
	static int NextBotTeam = -1;
	static int NextBotClass = -1;

	struct BotData
	{
		public bool Backwards;

		public float NextTurnTime;
		public bool LastTurnToRight;

		public float NextStrafeTime;
		public float SideMove;

		public QAngle ForwardAngle;
		public QAngle LastAngles;

		public float JoinTeamTime;
		public int WantedTeam;
		public int WantedClass;
	}

	static InlineArrayMaxPlayers<BotData> g_BotData;

	public static BasePlayer? BotPutInServer(bool frozen, int team) {
		NextBotTeam = team;

		Span<char> botname = stackalloc char[64];
		sprintf(botname, "Bot%s").S(BotNumber.ToString("D2"));

		Edict? edict = engine.CreateFakeClient(botname.SliceNullTerminatedString());

		if (edict == null) {
			Msg("Failed to create Bot.\n");
			return null;
		}

		HL2MP_Player player = (HL2MP_Player)BaseEntity.Instance(edict)!;
		player.ClearFlags();
		player.AddFlag(EntityFlags.Client | EntityFlags.FakeClient);

		if (frozen)
			player.AddEFlags(EFL.BotFrozen);

		BotNumber++;

		g_BotData[player.EntIndex() - 1].WantedTeam = team;
		g_BotData[player.EntIndex() - 1].JoinTeamTime = (float)(gpGlobals.CurTime + 0.3f);

		return player;
	}

	[ConCommand("bot", "Add a bot.")]
	public static void bot() {
		if (!Util.IsCommandIssuedByServerAdmin())
			return;

		if (g_pGameRules == null) {
			Warning("Trying to create a bot too early!\n");
			return;
		}

		if (gpGlobals.MaxClients < 2) {
			Warning("Cannot create a player bot in singleplayer!\n");
			return;
		}

		BotPutInServer(false, 2);
	}

	public static void Bot_RunAll() {
		for (int i = 1; i <= gpGlobals.MaxClients; i++) {
			HL2MP_Player? player = ToHL2MPPlayer(Util.PlayerByIndex(i));

			if (player != null && (player.GetFlags() & EntityFlags.FakeClient) != 0)
				Bot_Think(player);
		}
	}

	public static bool RunMimicCommand(ref UserCmd cmd) {
		if (bot_mimic.GetInt() <= 0)
			return false;

		if (bot_mimic.GetInt() > gpGlobals.MaxClients)
			return false;

		BasePlayer? player = Util.PlayerByIndex(bot_mimic.GetInt());
		if (player == null)
			return false;

		cmd = player.GetLastUserCommand();
		cmd.ViewAngles.Y += bot_mimic_yaw_offset.GetFloat();

		return true;
	}

	static void RunPlayerMove(HL2MP_Player? fakeclient, in QAngle viewangles, float forwardmove, float sidemove, float upmove, InButtons buttons, byte impulse, float frametime) {
		if (fakeclient == null)
			return;

		UserCmd cmd = new();

		double flOldFrametime = gpGlobals.FrameTime;
		double flOldCurtime = gpGlobals.CurTime;

		double flTimeBase = gpGlobals.CurTime + gpGlobals.FrameTime - frametime;
		fakeclient.SetTimeBase(flTimeBase);

		cmd.Reset();

		if (!RunMimicCommand(ref cmd) && !bot_zombie.GetBool()) {
			MathLib.VectorCopy(viewangles, out cmd.ViewAngles);
			cmd.ForwardMove = forwardmove;
			cmd.SideMove = sidemove;
			cmd.UpMove = upmove;
			cmd.Buttons = buttons;
			cmd.Impulse = impulse;
			cmd.RandomSeed = RandomInt(0, 0x7fffffff);
		}

		if (bot_crouch.GetInt() != 0)
			cmd.Buttons |= InButtons.Duck;

		if (bot_attack.GetBool())
			cmd.Buttons |= InButtons.Attack;

		MoveHelperServer.s_MoveHelperServer.SetHost(fakeclient);
		fakeclient.PlayerRunCommand(cmd, MoveHelperServer.s_MoveHelperServer);

		fakeclient.SetLastUserCommand(cmd);

		fakeclient.pl.FixAngle = (int)FixAngle.None;

		gpGlobals.FrameTime = flOldFrametime;
		gpGlobals.CurTime = flOldCurtime;
	}

	public static void Bot_Think(HL2MP_Player bot) {
		bot.AddFlag(EntityFlags.FakeClient);

		ref BotData botdata = ref g_BotData[ENTINDEX(bot.Edict()) - 1];

		QAngle vecViewAngles;
		float forwardmove = 0.0f;
		float sidemove = botdata.SideMove;
		float upmove = 0.0f;
		InButtons buttons = 0;
		byte impulse = 0;
		float frametime = (float)gpGlobals.FrameTime;

		vecViewAngles = bot.GetLocalAngles();

		if (bot.IsAlive() && bot.GetSolid() == SolidType.BBox) {
			Trace trace;

			if (!bot.IsEFlagSet(EFL.BotFrozen)) {
				if (bot.Health == 100) {
					forwardmove = botdata.Backwards ? -600.0f : 600.0f;
					if (botdata.SideMove != 0.0f)
						forwardmove *= RandomFloat(0.1f, 1.0f);
				}
				else
					forwardmove = 0;
			}

			if (!bot.IsEFlagSet(EFL.BotFrozen) && bot.Health == 100) {
				Vector3 vecEnd;
				Vector3 forward;

				QAngle angle;
				float angledelta = 15.0f;

				int maxtries = (int)(360.0f / angledelta);

				if (botdata.LastTurnToRight)
					angledelta = -angledelta;

				angle = bot.GetLocalAngles();

				Vector3 vecSrc;
				while (--maxtries >= 0) {
					MathLib.AngleVectors(angle, out forward);

					vecSrc = bot.GetLocalOrigin() + new Vector3(0, 0, 36);

					vecEnd = vecSrc + forward * 10;

					Util.TraceHull(vecSrc, vecEnd, VEC_HULL_MIN_SCALED(bot), VEC_HULL_MAX_SCALED(bot),
						Mask.PlayerSolid, bot, CollisionGroup.None, out trace);

					if (trace.Fraction == 1.0) {
						if (gpGlobals.CurTime < botdata.NextTurnTime)
							break;
					}

					angle.Y += angledelta;

					if (angle.Y > 180)
						angle.Y -= 360;
					else if (angle.Y < -180)
						angle.Y += 360;

					botdata.NextTurnTime = (float)(gpGlobals.CurTime + 2.0f);
					botdata.LastTurnToRight = RandomInt(0, 1) == 0 ? true : false;

					botdata.ForwardAngle = angle;
					botdata.LastAngles = angle;
				}

				if (gpGlobals.CurTime >= botdata.NextStrafeTime) {
					botdata.NextStrafeTime = (float)(gpGlobals.CurTime + 1.0f);

					if (RandomInt(0, 5) == 0)
						botdata.SideMove = -600.0f + 1200.0f * RandomFloat(0, 2);
					else
						botdata.SideMove = 0;
					sidemove = botdata.SideMove;

					if (RandomInt(0, 20) == 0)
						botdata.Backwards = true;
					else
						botdata.Backwards = false;
				}

				bot.SetLocalAngles(angle);
				vecViewAngles = angle;
			}

			if (bot_defend.GetInt() == bot.GetTeamNumber())
				buttons |= InButtons.Attack2;
			else if (bot_forcefireweapon.GetString() != null) {
				BaseCombatWeapon? weapon = bot.Weapon_OwnsThisType(bot_forcefireweapon.GetString(), 0);
				if (weapon != null) {
					BaseCombatWeapon? activeWeapon = bot.GetActiveWeapon();

					if (activeWeapon != weapon)
						bot.Weapon_Switch(weapon);
					else {
						if (bot_forceattackon.GetBool() || RandomFloat(0.0f, 1.0f) > 0.5f)
							buttons |= bot_forceattack2.GetBool() ? InButtons.Attack2 : InButtons.Attack;
					}
				}
			}

			if (bot_flipout.GetInt() != 0) {
				if (bot_forceattackon.GetBool() || RandomFloat(0.0f, 1.0f) > 0.5f)
					buttons |= bot_forceattack2.GetBool() ? InButtons.Attack2 : InButtons.Attack;
			}

			if (bot_sendcmd.GetString().Length > 0) {
				TokenizedCommand args = new();
				args.Tokenize(bot_sendcmd.GetString());
				bot.ClientCommand(args);

				bot_sendcmd.SetValue("");
			}
		}
		else {
			if (!bot.IsAlive()) {
				if (RandomInt(0, 100) > 80) {
					if (RandomInt(0, 1) == 0)
						buttons |= InButtons.Jump;
					else
						buttons = 0;
				}
			}
		}

		if (bot_flipout.GetInt() >= 2) {
			QAngle angOffset = MathLib.RandomAngle(-1, 1);

			botdata.LastAngles += angOffset;

			for (int i = 0; i < 2; i++) {
				if (MathF.Abs(botdata.LastAngles[i] - botdata.ForwardAngle[i]) > 15.0f) {
					if (botdata.LastAngles[i] > botdata.ForwardAngle[i])
						botdata.LastAngles[i] = botdata.ForwardAngle[i] + 15;
					else
						botdata.LastAngles[i] = botdata.ForwardAngle[i] - 15;
				}
			}

			botdata.LastAngles[2] = 0;

			bot.SetLocalAngles(botdata.LastAngles);
		}

		RunPlayerMove(bot, bot.GetLocalAngles(), forwardmove, sidemove, upmove, buttons, impulse, frametime);
	}
}
