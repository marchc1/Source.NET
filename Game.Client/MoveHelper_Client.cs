using CommunityToolkit.HighPerformance;

using Game.Shared;

using Source.Common.Audio;
using Source.Common.MaterialSystem;
using Source.Common.Mathematics;
using Source.Common.Physics;

using System.Numerics;

namespace Game.Client;

public class MoveHelperClient : IMoveHelper
{
	public static readonly MoveHelperClient s_MoveHelperClient = new();
	public MoveHelperClient() {
		IMoveHelper.SetSingleton(this);
	}

	public bool AddToTouched(ref Trace tr, in Vector3 impactvelocity) {
		int i;
		Span<TouchList_t> tl = TouchList.AsSpan();

		// Look for duplicates
		for (i = 0; i < tl.Length; i++) 
			if (tl[i].Trace.Ent == tr.Ent) 
				return false;

		TouchList.Add(default);
		i = TouchList.Count - 1;
		tl = TouchList.AsSpan();
		tl[i].Trace = tr;
		MathLib.VectorCopy(impactvelocity, out tl[i].DeltaVelocity);

		return true;
	}

	public ReadOnlySpan<char> GetName(EntityHandle_t handle) => "";
	public IPhysicsSurfaceProps? GetSurfaceProps() => physprops;
	public bool IsWorldEntity(EntityHandle_t handle) => handle == cl_entitylist.GetNetworkableHandle(0);

	public void PlaybackEventFull(int flags, int clientindex, ushort eventindex, float delay, out Vector3 origin, out Vector3 angles, float fparam1, float fparam2, int iparam1, int iparam2, int bparam1, int bparam2) {
		// todo
		origin = default;
		angles = default;
	}

	public bool PlayerFallingDamage() => true;
	public void PlayerSetAnimation(PlayerAnim playerAnim) {}

	public void ProcessImpacts() {
		C_BasePlayer? player = C_BasePlayer.GetLocalPlayer();
		if (player == null)
			return;

		// Relink in order to build absorigin and absmin/max to reflect any changes
		//  from prediction.  Relink will early out on SOLID_NOT

		// TODO: Touch triggers on the client
		//pPlayer->PhysicsTouchTriggers();

		// Don't bother if the player ain't solid
		if (player.IsSolidFlagSet(Source.SolidFlags.NotSolid))
			return;

		// Save off the velocity, cause we need to temporarily reset it
		Vector3 vel = player.GetAbsVelocity();

		// Touch other objects that were intersected during the movement.
		for (int i = 0; i < TouchList.Count; i++) {
			// Run the impact function as if we had run it during movement.
			ref TouchList_t tl = ref TouchList.AsSpan()[i];
			C_BaseEntity? entity = cl_entitylist.GetEnt(tl.Trace.Ent!.EntIndex());
			if (entity == null)
				continue;

			Assert(entity != player);
			// Don't ever collide with self!!!!
			if (entity == player)
				continue;

			// Reconstruct trace results.
			tl.Trace.Ent = entity;

			// Use the velocity we had when we collided, so boxes will move, etc.
			player.SetAbsVelocity(tl.DeltaVelocity);
			entity.PhysicsImpact(player, tl.Trace);
		}

		// Restore the velocity
		player.SetAbsVelocity(vel);

		// So no stuff is ever left over, sigh...
		ResetTouchList();
	}

	public void ResetTouchList() {
		TouchList.Clear();
	}

	public void StartSound(in Vector3 origin, int channel, ReadOnlySpan<char> sample, float volume, SoundLevel soundlevel, SoundFlags flags, int pitch) {
		if (!sample.IsEmpty) {
			C_BaseEntity.PrecacheScriptSound(sample);
			LocalPlayerFilter filter = new();
			filter.UsePredictionRules();

			scoped EmitSound_t ep = new();
			ep.Channel = channel;
			ep.SoundName = sample;
			ep.Volume = volume;
			ep.SoundLevel = soundlevel;
			ep.Pitch = pitch;
			ep.Origin = ref origin;

			C_BaseEntity.EmitSound(filter, SOUND_FROM_LOCAL_PLAYER, in ep);
		}
	}

	public void StartSound(in Vector3 origin, ReadOnlySpan<char> soundname) {
		if (soundname.IsEmpty)
			return;

		LocalPlayerFilter filter = new();
		filter.UsePredictionRules();
		C_BaseEntity.EmitSound(filter, SOUND_FROM_LOCAL_PLAYER, soundname, in origin, 0, out _);
	}

	struct TouchList_t
	{
		public Vector3 DeltaVelocity;
		public Trace Trace;
	}
	readonly List<TouchList_t> TouchList = [];
}
