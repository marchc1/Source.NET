using Source.Common;

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
}
