using Source.Common;
using Source.Common.Engine;
using Source.Common.Mathematics;

namespace Game.Client;

public partial class C_BaseViewModel
{
	int OldAnimationParity;
	public override int DrawModel(StudioFlags flags) {
		if (!ReadyToDraw)
			return 0;

		if((flags & StudioFlags.Render) != 0) {
			// Determine blending amount and tell engine
			float blend = (float)(GetFxBlend() / 255.0f);

			// Totally gone
			if (blend <= 0.0f)
				return 0;

			// Tell engine
			render.SetBlend(blend);

			Span<float> color = stackalloc float[3];
			GetColorModulation(color);
			render.SetColorModulation(color);
		}

		C_BasePlayer? player = C_BasePlayer.GetLocalPlayer();
		C_BaseCombatWeapon? weapon = GetOwningWeapon();
		int ret;
		if (player != null && player.IsOverridingViewmodel())
			ret = player.DrawOverriddenViewmodel(this, flags);
		else if (weapon != null && weapon.IsOverridingViewmodel())
			ret = weapon.DrawOverriddenViewmodel(this, flags);
		else
			ret = base.DrawModel(flags);

		if ((flags & StudioFlags.Render) != 0) {
			if (OldAnimationParity != AnimationParity) 
				OldAnimationParity = AnimationParity;

			// Tell the weapon itself that we've rendered, in case it wants to do something
			if (weapon != null) 
				weapon.ViewModelDrawn(this);
		}

		return ret;
	}
	public void UpdateAnimationParity() {
		C_BasePlayer? player = C_BasePlayer.GetLocalPlayer();
		if(OldAnimationParity != AnimationParity && !GetPredictable()) {
			double curtime = player != null && IsIntermediateDataAllocated() ? player.GetFinalPredictedTime() : gpGlobals.CurTime;
			SetCycle(0);
			AnimTime = curtime;
		}
	}

	public override void OnDataChanged(DataUpdateType updateType) {
		SetPredictionEligible(true);
		base.OnDataChanged(updateType);
	}

	public override void PostDataUpdate(DataUpdateType updateType) {
		base.PostDataUpdate(updateType);
		OnLatchInterpolatedVariables(LatchFlags.LatchAnimationVar);
	}

	public override bool Interpolate(TimeUnit_t currentTime) {
		StudioHdr? studioHdr = GetModelPtr();
		UpdateAnimationParity();
		bool bret = base.Interpolate(currentTime);

		// Hack to extrapolate cycle counter for view model
		TimeUnit_t elapsed_time = currentTime - AnimTime;
		C_BasePlayer? player = C_BasePlayer.GetLocalPlayer();

		// Predicted viewmodels have fixed up interval
		if (GetPredictable() || IsClientCreated()) {
			Assert(player != null);
			TimeUnit_t curtime = player != null? player.GetFinalPredictedTime() : gpGlobals.CurTime;
			elapsed_time = curtime - AnimTime;
			if (!engine.IsPaused()) 
				elapsed_time += (gpGlobals.InterpolationAmount * TICK_INTERVAL);
		}

		// Prediction errors?	
		if (elapsed_time < 0) 
			elapsed_time = 0;

		TimeUnit_t dt = elapsed_time * GetSequenceCycleRate(studioHdr, GetSequence()) * GetPlaybackRate();
		if (dt >= 1.0f) {
			if (!IsSequenceLooping(GetSequence())) {
				dt = 0.999f;
			}
			else {
				dt = MathLib.Fmodf(dt, 1.0);
			}
		}

		SetCycle(dt);
		return bret;
	}
	public override bool IsViewModel() => true;
	public override RenderGroup GetRenderGroup() {
		return RenderGroup.ViewModelOpaque;
	}
}
