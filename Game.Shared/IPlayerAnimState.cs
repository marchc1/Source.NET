using Source.Common.Mathematics;

namespace Game.Shared;

public enum LegAnimType
{
	Anim9Way,
	Anim8Way,
	AnimGoldSrc,
}

public interface IPlayerAnimState
{
	void Release();

	// Update() and DoAnimationEvent() together maintain the entire player's animation state.
	//
	// Update() maintains the the lower body animation (the player's m_nSequence)
	// and the upper body overlay based on the player's velocity and look direction.
	//
	// It also modulates these based on events triggered by DoAnimationEvent.
	void Update(float eyeYaw, float eyePitch);

	// This is called by the client when a new player enters the PVS to clear any events
	// the dormant version of the entity may have been playing.
	void ClearAnimationState();

	// The client uses this to figure out what angles to render the entity with (since as the guy turns,
	// it will change his body_yaw pose parameter before changing his rendered angle).
	ref readonly QAngle GetRenderAngles();
}
