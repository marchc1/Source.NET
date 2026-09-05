using CommunityToolkit.HighPerformance;

using Game.Shared;

using Source.Common;
using Source.Common.Audio;
using Source.Common.Engine;
using Source.Common.Mathematics;
using Source.Common.Physics;

using System.Numerics;
using System.Runtime.CompilerServices;

namespace Game.Server;


public interface IMoveHelperServer : IMoveHelper
{
	void SetHost(BasePlayer host);
}
public class MoveHelperServer : IMoveHelperServer
{
	public static readonly MoveHelperServer s_MoveHelperServer = new();
	public MoveHelperServer() {
		IMoveHelper.SetSingleton(this);
	}

	public bool AddToTouched(ref Trace tr, in Vector3 impactvelocity) {
		Assert(HostPlayer != null);

		// Trace missed
		if (tr.Ent == null)
			return false;

		if (tr.Ent == HostPlayer) {
			AssertMsg(false, "CMoveHelperServer::AddToTouched:  Tried to add self to touchlist!!!");
			return false;
		}

		Span<TouchList_t> tl = TouchList.AsSpan();

		// Check for duplicate entities
		for (int j = tl.Length; --j >= 0;)
			if (tl[j].Trace.Ent == tr.Ent)
				return false;

		TouchList.Add(default);
		int i = TouchList.Count - 1;
		tl = TouchList.AsSpan();
		tl[i].Trace = tr;
		MathLib.VectorCopy(impactvelocity, out tl[i].DeltaVelocity);

		return true;
	}

	public static Edict? GetEdict(EntityHandle_t handle) => gEntList.GetEdict(handle);

	public ReadOnlySpan<char> GetName(EntityHandle_t handle) {
		// This ain't pertickulerly fast, but it's for debugging anyways
		Edict? edict = GetEdict(handle);
		BaseEntity? ent = BaseEntity.Instance(edict);

		// Is it the world?
		if (ENTINDEX(edict) == 0)
			return gpGlobals.MapName;

		// Is it a model?
		if (ent != null && !ent.GetModelName().IsEmpty)
			return ent!.GetModelName();

		if (!ent!.GetClassname().IsEmpty)
			return ent!.GetClassname();

		return "?";
	}

	public IPhysicsSurfaceProps? GetSurfaceProps() => physprops;

	public bool IsWorldEntity(EntityHandle_t handle) => handle == BaseEntity.Instance(0)?.GetRefEHandle();


	public void PlaybackEventFull(int flags, int clientindex, ushort eventindex, float delay, out Vector3 origin, out Vector3 angles, float fparam1, float fparam2, int iparam1, int iparam2, int bparam1, int bparam2) {
		origin = default;
		angles = default;
	}

	public bool PlayerFallingDamage() {
		// todo
		return true;
	}

	public void PlayerSetAnimation(PlayerAnim playerAnim) {

	}

	public void ProcessImpacts() {
		Assert(HostPlayer != null);

		// Relink in order to build absorigin and absmin/max to reflect any changes
		//  from prediction.  Relink will early out on SOLID_NOT. TODO
		// HostPlayer.PhysicsTouchTriggers();

		// Don't bother if the player ain't solid
		if (HostPlayer.IsSolidFlagSet(Source.SolidFlags.NotSolid))
			return;

		// Save off the velocity, cause we need to temporarily reset it
		Vector3 vel = HostPlayer.GetAbsVelocity();

		Span<TouchList_t> tl = TouchList.AsSpan();
		// Touch other objects that were intersected during the movement.
		for (int i = 0; i < tl.Length; i++) {
			BaseHandle entindex = tl[i].Trace.Ent!.GetRefEHandle();

			// We should have culled negative indices by now
			Assert(entindex.IsValid());

			Edict? ent = GetEdict(entindex);
			if (ent == null)
				continue;

			// Run the impact function as if we had run it during movement.
			BaseEntity? entity = BaseEntity.GetContainingEntity(ent);
			if (entity == null)
				continue;

			Assert(entity != HostPlayer);
			// Don't ever collide with self!!!!
			if (entity == HostPlayer)
				continue;

			// Reconstruct trace results.
			tl[i].Trace.Ent = BaseEntity.Instance(ent);

			// Use the velocity we had when we collided, so boxes will move, etc.
			HostPlayer.SetAbsVelocity(tl[i].DeltaVelocity);

			entity.PhysicsImpact(HostPlayer, in tl[i].Trace);
		}

		// Restore the velocity
		HostPlayer.SetAbsVelocity(vel);

		// So no stuff is ever left over, sigh...
		ResetTouchList();
	}

	public void ResetTouchList() {
		TouchList.Clear();
	}

	public void SetHost(BasePlayer? host) {
		HostPlayer = host;
	}

	public void StartSound(in Vector3 origin, int channel, ReadOnlySpan<char> sample, float volume, SoundLevel soundlevel, SoundFlags flags, int pitch) {
		RecipientFilter filter = new();
		filter.AddRecipientsByPAS(origin);
		// FIXME, these sounds should not go to the host entity ( SND_NOTHOST )
		if (gpGlobals.MaxClients == 1) {
			// Always send sounds down in SP

			scoped EmitSound_t ep = new();
			ep.Channel = channel;
			ep.SoundName = sample;
			ep.Volume = volume;
			ep.SoundLevel = soundlevel;
			ep.Flags = flags;
			ep.Pitch = pitch;
			ep.Origin = ref origin;

			BaseEntity.EmitSound(filter, HostPlayer!.EntIndex(), ep);
		}
		else {
			filter.UsePredictionRules();

			scoped EmitSound_t ep = new();
			ep.Channel = channel;
			ep.SoundName = sample;
			ep.Volume = volume;
			ep.SoundLevel = soundlevel;
			ep.Flags = flags;
			ep.Pitch = pitch;
			ep.Origin = ref origin;

			BaseEntity.EmitSound(filter, HostPlayer!.EntIndex(), ep);
		}
	}

	public void StartSound(in Vector3 origin, ReadOnlySpan<char> soundname) {
		RecipientFilter filter = new();
		filter.AddRecipientsByPAS(origin);

		BaseEntity.EmitSound(filter, HostPlayer!.EntIndex(), soundname, in Unsafe.NullRef<Vector3>(), 0, out _);
	}

	BasePlayer? HostPlayer;
	struct TouchList_t
	{
		public Vector3 DeltaVelocity;
		public Trace Trace;
	}
	readonly List<TouchList_t> TouchList = [];
}
