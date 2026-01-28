global using static Game.Shared.MoveVarsShared;

using Source;
using Source.Common.Commands;

namespace Game.Shared;


public static class MoveVarsShared
{
	public static float GetCurrentGravity() => sv_gravity.GetFloat();
	public const string DEFAULT_GRAVITY_STRING = "600";

	public static readonly ConVar sv_gravity = new(DEFAULT_GRAVITY_STRING, FCvar.Notify | FCvar.Replicated, "World gravity.");
	public static readonly ConVar sv_stopspeed = new("100", FCvar.Notify | FCvar.Replicated, "Minimum stopping speed when on ground.");

	public static readonly ConVar sv_maxspeed = new("320", FCvar.Notify | FCvar.Replicated);
	public static readonly ConVar sv_accelerate = new("10", FCvar.Notify | FCvar.Replicated);

	public static readonly ConVar sv_airaccelerate = new("10", FCvar.Notify | FCvar.Replicated);
	public static readonly ConVar sv_wateraccelerate = new("10", FCvar.Notify | FCvar.Replicated);
	public static readonly ConVar sv_waterfriction = new("1", FCvar.Notify | FCvar.Replicated);
	public static readonly ConVar sv_footsteps = new("1", FCvar.Notify | FCvar.Replicated, "Play footstep sound for players");
	public static readonly ConVar sv_rollspeed = new("200", FCvar.Notify | FCvar.Replicated);
	public static readonly ConVar sv_rollangle = new("0", FCvar.Notify | FCvar.Replicated, "Max view roll angle");

	public static readonly ConVar sv_bounce = new("0", FCvar.Notify | FCvar.Replicated, "Bounce multiplier for when physically simulated objects collide with other objects.");
	public static readonly ConVar sv_maxvelocity = new("3500", FCvar.Replicated, "Maximum speed any ballistically moving object is allowed to attain per axis.");
	public static readonly ConVar sv_stepsize = new("18", FCvar.Notify | FCvar.Replicated);
	public static readonly ConVar sv_backspeed = new("0.6", FCvar.Archive | FCvar.Replicated, "How much to slow down backwards motion");
	public static readonly ConVar sv_waterdist = new("12", FCvar.Replicated, "Vertical view fixup when eyes are near water plane.");

	public static readonly ConVar sv_skyname = new("sky_urb01", FCvar.Archive | FCvar.Replicated, "Current name of the skybox texture");
	public static readonly ConVar sv_friction = new("sv_friction", "4", FCvar.Notify | FCvar.Replicated | FCvar.DevelopmentOnly, "World friction.");

	// Vehicle convars
	public static readonly ConVar r_VehicleViewDampen = new("r_VehicleViewDampen", "1", FCvar.Cheat | FCvar.Notify | FCvar.Replicated);

	// Jeep convars
	public static readonly ConVar r_JeepViewDampenFreq = new("7.0", FCvar.Cheat | FCvar.Notify | FCvar.Replicated);
	public static readonly ConVar r_JeepViewDampenDamp = new("1.0", FCvar.Cheat | FCvar.Notify | FCvar.Replicated);
	public static readonly ConVar r_JeepViewZHeight = new("10.0", FCvar.Cheat | FCvar.Notify | FCvar.Replicated);

	// Airboat convars
	public static readonly ConVar r_AirboatViewDampenFreq = new("7.0", FCvar.Cheat | FCvar.Notify | FCvar.Replicated);
	public static readonly ConVar r_AirboatViewDampenDamp = new("1.0", FCvar.Cheat | FCvar.Notify | FCvar.Replicated);
	public static readonly ConVar r_AirboatViewZHeight = new("0.0", FCvar.Cheat | FCvar.Notify | FCvar.Replicated);

	public static readonly ConVar sv_noclipaccelerate = new("sv_noclipaccelerate", "5", FCvar.Notify | FCvar.Archive | FCvar.Replicated);
	public static readonly ConVar sv_noclipspeed = new("sv_noclipspeed", "5", FCvar.Archive | FCvar.Notify | FCvar.Replicated);
	public static readonly ConVar sv_specaccelerate = new("sv_specaccelerate", "5", FCvar.Notify | FCvar.Archive | FCvar.Replicated);
	public static readonly ConVar sv_specspeed = new("sv_specspeed", "3", FCvar.Archive | FCvar.Notify | FCvar.Replicated);
	public static readonly ConVar sv_specnoclip = new("sv_specnoclip", "1", FCvar.Archive | FCvar.Notify | FCvar.Replicated);
}
