global using EntityHandle_t = Source.Common.BaseHandle;
global using static Game.Shared.MoveHelperExts;
using Source.Common;
using Source.Common.Audio;
using Source.Common.Mathematics;
using Source.Common.Physics;

using System.Numerics;

namespace Game.Shared;

public static class MoveHelperExts {
	public static IMoveHelper MoveHelper() => IMoveHelper.GetSingleton()!;

}
public interface IMoveHelper
{
	static IMoveHelper? GetSingleton() => sm_pSingleton;

	// Methods associated with a particular entity
	ReadOnlySpan<char> GetName(EntityHandle_t handle);

	// Adds the trace result to touch list, if contact is not already in list.
	void ResetTouchList();
	bool AddToTouched(in GameTrace tr, in Vector3 impactvelocity);
	void ProcessImpacts();

	// These have separate server vs client impementations
	void StartSound(in Vector3 origin, int channel, ReadOnlySpan<char> sample, float volume, SoundLevel soundlevel, int flags, int pitch);
	void StartSound(in Vector3 origin, ReadOnlySpan<char> soundname);
	void PlaybackEventFull(int flags, int clientindex, ushort eventindex, float delay, out Vector3 origin, out Vector3 angles, float fparam1, float fparam2, int iparam1, int iparam2, int bparam1, int bparam2);

	// Apply falling damage to m_pHostPlayer based on m_pHostPlayer->m_flFallVelocity.
	bool PlayerFallingDamage();

	// Apply falling damage to m_pHostPlayer based on m_pHostPlayer->m_flFallVelocity.
	void PlayerSetAnimation(PlayerAnim playerAnim);
	IPhysicsSurfaceProps? GetSurfaceProps();

	bool IsWorldEntity(BaseHandle handle);

	// Inherited classes can call this to set the singleton
	protected static void SetSingleton(IMoveHelper? moveHelper) { sm_pSingleton = moveHelper; }

	// The global instance
	protected static IMoveHelper? sm_pSingleton;
}
