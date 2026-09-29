using Source.Common;
using Source;
using Game.Shared;
using System.Numerics;
namespace Game.Server;

using FIELD = FIELD<TriggerPlayerMovement>;
[LinkEntityToClass("trigger_playermovement")]
[NetworkName("CTriggerPlayerMovement")]
public class TriggerPlayerMovement : BaseTrigger
{
	public static readonly SendTable DT_TriggerPlayerMovement = new(DT_BaseTrigger, []);
	public static readonly new ServerClass ServerClass = new ServerClass(DT_TriggerPlayerMovement);

	const int SF_TRIGGER_MOVE_AUTODISABLE = 0x80;      // disable auto movement
	const int SF_TRIGGER_AUTO_DUCK = 0x800;    // Duck automatically

	//-----------------------------------------------------------------------------
	// Purpose: Called when spawning, after keyvalues have been handled.
	//-----------------------------------------------------------------------------
	public override void Spawn() {
		if (HasSpawnFlags(TriggerGlobals.SF_TRIGGER_ONLY_PLAYER_ALLY_NPCS)) {
			// @Note (toml 01-07-04): fix up spawn flag collision coding error. Remove at some point once all maps fixed up please!
			DevMsg("*** trigger_playermovement using obsolete spawnflag. Remove and reset with new value for \"Disable auto player movement\"\n");
			RemoveSpawnFlags(TriggerGlobals.SF_TRIGGER_ONLY_PLAYER_ALLY_NPCS);
			AddSpawnFlags(SF_TRIGGER_MOVE_AUTODISABLE);
		}
		base.Spawn();

		InitTrigger();
	}

	// UNDONE: This will not support a player touching more than one of these
	// UNDONE: Do we care?  If so, ref count automovement in the player?
	public override void StartTouch(BaseEntity? other) {
		if (other == null || !PassesTriggerFilters(other))
			return;

		BasePlayer? player = ToBasePlayer(other);

		if (player == null)
			return;

		if (HasSpawnFlags(SF_TRIGGER_AUTO_DUCK))
			player.ForceButtons(InButtons.Duck);

		// UNDONE: Currently this is the only operation this trigger can do
		if (HasSpawnFlags(SF_TRIGGER_MOVE_AUTODISABLE))
			player.Local.AllowAutoMovement = false;
	}

	public override void EndTouch(BaseEntity? other) {
		if (other == null || !PassesTriggerFilters(other))
			return;

		BasePlayer? player = ToBasePlayer(other);

		if (player == null)
			return;

		if (HasSpawnFlags(SF_TRIGGER_AUTO_DUCK))
			player.UnforceButtons(InButtons.Duck);

		if (HasSpawnFlags(SF_TRIGGER_MOVE_AUTODISABLE))
			player.Local.AllowAutoMovement = true;
	}
}
