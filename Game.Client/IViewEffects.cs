using Source.Common;
using Source.Common.Mathematics;

using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;

using static System.Runtime.InteropServices.JavaScript.JSType;

namespace Game.Client;

public interface IViewEffects
{
	void Init();
	// Initialize after each level change
	void LevelInit();
	// Called each frame to determine the current view fade parameters ( color and alpha )
	void GetFadeParams(out byte r, out byte g, out byte b, out byte a, out bool blend);
	// Apply directscreen shake
	void Shake(in ScreenShake data);
	// Apply direct screen fade
	void Fade(in ScreenFade data);
	// Clear all permanent fades in our fade list
	void ClearPermanentFades();
	// Clear all fades in our fade list
	void ClearAllFades();
	// Compute screen shake values for this frame
	void CalcShake();
	// Apply those values to the passed in vector(s).
	void ApplyShake(ref Vector3 origin, ref QAngle angles, float factor);
	// Save / Restore
	void Save(ISave save);
	void Restore(IRestore restore, bool _);
}
