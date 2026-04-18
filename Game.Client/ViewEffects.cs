using Source;
using Source.Common;
using Source.Common.Bitbuffers;
using Source.Common.Commands;
using Source.Common.Mathematics;

using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;

namespace Game.Client;

public class ActiveScreenFade
{
	public TimeUnit_t Speed;
	public TimeUnit_t End;
	public TimeUnit_t Reset;
	public Color Color;
	public FadeFlags Flags;
}

public class ActiveScreenShake
{
	public TimeUnit_t EndTime;
	public TimeUnit_t Duration;
	public TimeUnit_t Amplitude;
	public TimeUnit_t Frequency;
	public TimeUnit_t NextShake;
	public Vector3 Offset;
	public float Angle;
	public ShakeCommand Command;
}

public class ViewEffects : IViewEffects
{
	public static readonly ViewEffects g_ViewEffects = new();

	readonly static ConVar shake_show = new( "shake_show", "0", 0, "Displays a list of the active screen shakes." );
	readonly static ConCommand shake_stop = new("shake_stop", CC_Shake_Stop, "Stops all active screen shakes.\n", FCvar.Cheat );
	private static void CC_Shake_Stop() => g_ViewEffects.ClearAllShakes();

	public void ApplyShake(ref Vector3 origin, ref QAngle angles, float factor) {
		MathLib.VectorMA(origin, factor, ShakeAppliedOffset, out origin);
		angles.Z += ShakeAppliedAngle * factor;
	}

	public void CalcShake() {
		float fraction, freq;

		// We'll accumulate the aggregate shake for this frame into these data members.
		ShakeAppliedOffset.Init(0, 0, 0);
		ShakeAppliedAngle = 0;
		float flRumbleAngle = 0;

		// NVNT - haptic shake effect amplitude
		float hapticShakeAmp = 0;

		bool bShow = shake_show.GetBool();

		int nShakeCount = ShakeList.Count;

		for (int nShake = nShakeCount - 1; nShake >= 0; nShake--) {
			ActiveScreenShake? shake = ShakeList[nShake];

			if (shake.EndTime == 0) {
				// Shouldn't be any such shakes in the list.
				Assert(false);
				continue;
			}

			if ((gpGlobals.CurTime > shake.EndTime) ||
				shake.Duration <= 0 ||
				shake.Amplitude <= 0 ||
				shake.Frequency <= 0) {
				// Retire this shake.
				ShakeList.RemoveAt(nShake);
				continue;
			}

			if (bShow) {
				Con_NPrint_s np = default;
				np.TimeToLive = 2.0f;
				np.FixedWidthFont= true;
				np.Color[0] = 1.0f;
				np.Color[1] = 0.8f;
				np.Color[2] = 0.1f;
				np.Index = nShake + 2;

				engine.Con_NXPrintf(np, $"{nShake + 1}: dur({shake.Duration}) amp({shake.Amplitude}) freq({shake.Frequency})");
			}

			if (gpGlobals.CurTime > shake.NextShake) {
				// Higher frequency means we recalc the extents more often and perturb the display again
				shake.NextShake = gpGlobals.CurTime + (1.0f / shake.Frequency);

				// Compute random shake extents (the shake will settle down from this)
				for (int i = 0; i < 3; i++) 
					shake.Offset[i] = random.RandomFloat((float)(-shake.Amplitude), (float)(shake.Amplitude));

				shake.Angle = random.RandomFloat((float)(-shake.Amplitude * 0.25), (float)(shake.Amplitude * 0.25));
			}

			// Ramp down amplitude over duration (fraction goes from 1 to 0 linearly with slope 1/duration)
			fraction = (float)((shake.EndTime - gpGlobals.CurTime) / shake.Duration);

			// Ramp up frequency over duration
			if (fraction != 0)
				freq = (float)(shake.Frequency / fraction);
			else 
				freq = 0;

			// square fraction to approach zero more quickly
			fraction *= fraction;

			// Sine wave that slowly settles to zero
			double angle = gpGlobals.CurTime * freq;
			if (angle > 1e8) 
				angle = 1e8;
			
			fraction = fraction * MathF.Sin((float)angle);

			if (shake.Command != ShakeCommand.StartNoRumble) {
				// As long as this isn't a NO RUMBLE effect, then accumulate rumble
				flRumbleAngle += shake.Angle * fraction;
			}

			if (shake.Command != ShakeCommand.StartRumbleOnly) {
				// As long as this isn't a RUMBLE ONLY effect, then accumulate screen shake

				// Add to view origin
				ShakeAppliedOffset += shake.Offset * fraction;

				// Add to roll
				ShakeAppliedAngle += shake.Angle * fraction;
			}

			// Drop amplitude a bit, less for higher frequency shakes
			shake.Amplitude -= shake.Amplitude * (gpGlobals.FrameTime / (shake.Duration * shake.Frequency));
			// NVNT - update our amplitude.
			hapticShakeAmp += (float)(shake.Amplitude * fraction);
		}
		// TODO: haptics, rumble.
	}

	public void ClearAllFades() {
		throw new NotImplementedException();
	}

	public void ClearPermanentFades() {
		throw new NotImplementedException();
	}

	public void Fade(in ScreenFade data) {
		throw new NotImplementedException();
	}

	public void GetFadeParams(out byte r, out byte g, out byte b, out byte a, out bool blend) {
		throw new NotImplementedException();
	}

	public void Init() {
		usermessages.HookMessage("Shake", ShakeFn);
	}

	private void ShakeFn(bf_read msg) {
		ScreenShake shake;
		shake.Command = (ShakeCommand)msg.ReadByte();
		shake.Amplitude = msg.ReadFloat();
		shake.Frequency = msg.ReadFloat();
		shake.Duration = msg.ReadFloat();

		g_ViewEffects.Shake(in shake);
	}

	public void LevelInit() {
		throw new NotImplementedException();
	}

	public void Restore(IRestore restore, bool _) {
		throw new NotImplementedException();
	}

	public void Save(ISave save) {
		throw new NotImplementedException();
	}

	public const int MAX_SHAKES = 32;

	public void Shake(in ScreenShake data) {
		if ((data.Command == ShakeCommand.Start || data.Command == ShakeCommand.StartRumbleOnly) && (ShakeList.Count < MAX_SHAKES)) {
			ActiveScreenShake newShake = new();

			newShake.Amplitude = data.Amplitude;
			newShake.Frequency = data.Frequency;
			newShake.Duration = data.Duration;
			newShake.NextShake = 0;
			newShake.EndTime = gpGlobals.CurTime + data.Duration;
			newShake.Command = data.Command;

			ShakeList.Add(newShake);
		}
		else if (data.Command == ShakeCommand.Stop)
			ClearAllShakes();
		else if (data.Command == ShakeCommand.Amplitude) {
			// Look for the most likely shake to modify.
			ActiveScreenShake? shake = FindLongestShake();
			shake?.Amplitude = data.Amplitude;
		}
		else if (data.Command == ShakeCommand.Frequency) {
			// Look for the most likely shake to modify.
			ActiveScreenShake? shake = FindLongestShake();
			shake?.Frequency = data.Frequency;
		}
	}

	public ActiveScreenShake? FindLongestShake() {
		ActiveScreenShake? longestShake = null;

		int nShakeCount = ShakeList.Count;
		for (int i = 0; i < nShakeCount; i++) {
			ActiveScreenShake shake = ShakeList[i];
			if (shake != null && (longestShake == null || (shake.Duration > longestShake.Duration)))
				longestShake = shake;
		}

		return longestShake;
	}

	public void ClearAllShakes() => ShakeList.Clear();

	readonly List<ActiveScreenFade> FadeList = [];
	readonly List<ActiveScreenShake> ShakeList = [];

	public Vector3 ShakeAppliedOffset;
	public float ShakeAppliedAngle;
}
