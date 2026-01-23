using Game.Shared;

using Source.Common.Audio;
using Source.Common.Physics;

using System.Numerics;

namespace Game.Server;


public interface IMoveHelperServer : IMoveHelper{
	void SetHost(BasePlayer host);
}
public class MoveHelperServer : IMoveHelperServer
{
	public static readonly MoveHelperServer s_MoveHelperServer = new();
	public MoveHelperServer() {
		IMoveHelper.SetSingleton(this);
	}

	public bool AddToTouched(in Trace tr, in Vector3 impactvelocity) {
		throw new NotImplementedException();
	}

	public ReadOnlySpan<char> GetName(EntityHandle_t handle) {
		throw new NotImplementedException();
	}

	public IPhysicsSurfaceProps? GetSurfaceProps() {
		throw new NotImplementedException();
	}

	public bool IsWorldEntity(EntityHandle_t handle) {
		throw new NotImplementedException();
	}

	public void PlaybackEventFull(int flags, int clientindex, ushort eventindex, float delay, out Vector3 origin, out Vector3 angles, float fparam1, float fparam2, int iparam1, int iparam2, int bparam1, int bparam2) {
		throw new NotImplementedException();
	}

	public bool PlayerFallingDamage() {
		throw new NotImplementedException();
	}

	public void PlayerSetAnimation(PlayerAnim playerAnim) {
		throw new NotImplementedException();
	}

	public void ProcessImpacts() {
		throw new NotImplementedException();
	}

	public void ResetTouchList() {
		TouchList.Clear();
	}

	public void SetHost(BasePlayer host) {
		HostPlayer = host;
	}

	public void StartSound(in Vector3 origin, int channel, ReadOnlySpan<char> sample, float volume, SoundLevel soundlevel, int flags, int pitch) {
		throw new NotImplementedException();
	}

	public void StartSound(in Vector3 origin, ReadOnlySpan<char> soundname) {
		throw new NotImplementedException();
	}

	BasePlayer? HostPlayer;
	struct TouchList_t
	{
		public Vector3 DeltaVelocity;
		public Trace Trace;
	}
	readonly List<TouchList_t> TouchList = [];
}
