global using static Source.AudioSystem.SndDevice;

using Source.Common.Mathematics;

using System.Numerics;

namespace Source.AudioSystem;

public static class SndDevice
{
	// sound engine rate defines
	public const int SOUND_DMA_SPEED = 44100;       // hardware playback rate

	public const int SOUND_11k = 11025;             // 11khz sample rate
	public const int SOUND_22k = 22050;             // 22khz sample rate
	public const int SOUND_44k = 44100;             // 44khz sample rate
	public const int SOUND_ALL_RATES = 1;           // mix all sample rates

	public const int SOUND_MIX_WET = 0;             // mix only samples that don't have channel set to 'dry' or 'speaker' (default)
	public const int SOUND_MIX_DRY = 1;             // mix only samples with channel set to 'dry' (ie: music)
	public const int SOUND_MIX_SPEAKER = 2;         // mix only samples with channel set to 'speaker'
	public const int SOUND_MIX_SPECIAL_DSP = 3;     // mix only samples with channel set to 'special dsp'

	public const int SOUND_BUSS_ROOM = 1 << 0;          // mix samples using channel dspmix value (based on distance from player)
	public const int SOUND_BUSS_FACING = 1 << 1;        // mix samples using channel dspface value (source facing)
	public const int SOUND_BUSS_FACINGAWAY = 1 << 2;    // mix samples using 1-dspface
	public const int SOUND_BUSS_SPEAKER = 1 << 3;       // mix ch->bspeaker samples in mono to speaker buffer
	public const int SOUND_BUSS_DRY = 1 << 4;           // mix ch->bdry samples into dry buffer
	public const int SOUND_BUSS_SPECIAL_DSP = 1 << 5;   // mix ch->bspecialdsp samples into special dsp buffer

	public const int SAMPLE_16BIT_SHIFT = 1;

	static readonly AudioDeviceNull nullDevice = new();

	public static IAudioDevice Audio_GetNullDevice() {
		// singeton device here
		return nullDevice;
	}
}

// General interface to an audio device
public interface IAudioDevice
{
	// This is needed by some of the routines to avoid doing work when you've got a null device
	bool IsActive();
	// This initializes the sound hardware.  true on success, false on failure
	bool Init();
	// This releases all sound hardware
	void Shutdown();
	// stop outputting sound, but be ready to resume on UnPause
	void Pause();
	// return to normal operation after a Pause()
	void UnPause();
	// The volume of the "dry" mix (no effects).
	// This should return 0 on all implementations that don't need a separate dry mix
	float MixDryVolume();
	// Should we mix sounds to a 3D (quadraphonic) sound buffer (front/rear both stereo)
	bool Should3DMix();

	// This is called when the application stops all sounds
	// NOTE: Stopping each channel and clearing the sound buffer are done separately
	void StopAllSounds();

	// Called before painting channels, must calculated the endtime and return it (once per frame)
	int PaintBegin(float mixAheadTime, int soundtime, int paintedtime);
	// Called when all channels are painted (once per frame)
	void PaintEnd();

	// Called to set the volumes on a channel with the given gain & dot parameters
	void SpatializeChannel(Span<int> volume, int master_vol, in Vector3 sourceDir, float gain, float mono);

	// The device should apply DSP up to endtime in the current paint buffer
	// this is called during painting
	void ApplyDSPEffects(int idsp, PortableSamplePair[] pbuffront, PortableSamplePair[]? pbufrear, PortableSamplePair[]? pbufcenter, int samplecount);

	// replaces SNDDMA_GetDMAPos, gets the output sample position for tracking
	int GetOutputPosition();

	// Fill the output buffer with silence (e.g. during pause)
	void ClearBuffer();

	// Called each frame with the listener's coordinate system
	void UpdateListener(in Vector3 position, in Vector3 forward, in Vector3 right, in Vector3 up);

	// Called each time a new paint buffer is mixed (may be multiple times per frame)
	void MixBegin(int sampleCount);
	void MixUpsample(int sampleCount, int filtertype);

	// sink sound data
	void Mix8Mono(Channel channel, ReadOnlySpan<byte> data, int outputOffset, int inputOffset, fixedint rateScaleFix, int outCount, int timecompress);
	void Mix8Stereo(Channel channel, ReadOnlySpan<byte> data, int outputOffset, int inputOffset, fixedint rateScaleFix, int outCount, int timecompress);
	void Mix16Mono(Channel channel, ReadOnlySpan<short> data, int outputOffset, int inputOffset, fixedint rateScaleFix, int outCount, int timecompress);
	void Mix16Stereo(Channel channel, ReadOnlySpan<short> data, int outputOffset, int inputOffset, fixedint rateScaleFix, int outCount, int timecompress);

	// Reset a channel
	void ChannelReset(int entnum, int channelIndex, float distanceMod);
	void TransferSamples(int end);

	// device parameters
	ReadOnlySpan<char> DeviceName();
	int DeviceChannels();       // 1 = mono, 2 = stereo
	int DeviceSampleBits();     // bits per sample (8 or 16)
	int DeviceSampleBytes();    // above / 8
	int DeviceDmaSpeed();       // Actual DMA speed
	int DeviceSampleCount();    // Total samples in buffer

	bool IsSurround();          // surround enabled, could be quad or 5.1
	bool IsSurroundCenter();    // surround enabled as 5.1
	bool IsHeadphone();
}

public class AudioDeviceBase : IAudioDevice
{
	const int ISPEAKER_RIGHT_FRONT = 0;
	const int ISPEAKER_LEFT_FRONT = 1;
	const int ISPEAKER_RIGHT_REAR = 2;
	const int ISPEAKER_LEFT_REAR = 3;
	const int ISPEAKER_CENTER_FRONT = 4;

	public virtual bool IsActive() => false;
	public virtual bool Init() => false;
	public virtual void Shutdown() { }
	public virtual void Pause() { }
	public virtual void UnPause() { }
	public virtual float MixDryVolume() => 0;
	public virtual bool Should3DMix() => Surround;
	public virtual void StopAllSounds() { }

	public virtual int PaintBegin(float mixAheadTime, int soundtime, int paintedtime) => 0;
	public virtual void PaintEnd() { }

	public virtual int GetOutputPosition() => 0;
	public virtual void ClearBuffer() { }
	public virtual void UpdateListener(in Vector3 position, in Vector3 forward, in Vector3 right, in Vector3 up) { }

	public virtual void ChannelReset(int entnum, int channelIndex, float distanceMod) { }
	public virtual void TransferSamples(int end) { }

	public virtual ReadOnlySpan<char> DeviceName() => null;
	public virtual int DeviceChannels() => 0;
	public virtual int DeviceSampleBits() => 0;
	public virtual int DeviceSampleBytes() => 0;
	public virtual int DeviceDmaSpeed() => 1;
	public virtual int DeviceSampleCount() => 0;

	public virtual bool IsSurround() => Surround;
	public virtual bool IsSurroundCenter() => SurroundCenter;
	public virtual bool IsHeadphone() => Headphone;

	public bool Surround;
	public bool SurroundCenter;
	public bool Headphone;

	static bool FVolumeFrontNonZero(ReadOnlySpan<int> pvol) => pvol[IFRONT_RIGHT] != 0 || pvol[IFRONT_LEFT] != 0;
	static bool FVolumeRearNonZero(ReadOnlySpan<int> pvol) => pvol[IREAR_RIGHT] != 0 || pvol[IREAR_LEFT] != 0;
	static bool FVolumeCenterNonZero(ReadOnlySpan<int> pvol) => pvol[IFRONT_CENTER] != 0;

	// fade speaker volumes to mono, based on xfade value.
	// ie: xfade 1.0 is full mono.
	// ispeaker is speaker index, cspeaker is total # of speakers
	// fmix2channels causes mono mix for 4 channel mix to mix down to 2 channels
	//    this is used for the 2 speaker outpu case, which uses recombined 4 channel front/rear mixing

	static float XfadeSpeakerVolToMono(float scale, float xfade, int ispeaker, int cspeaker, bool fmix2channels) {
		float scale_out;
		float scale_target;

		if (cspeaker == 4) {
			// mono sound distribution:
			ReadOnlySpan<float> scale_targets = [0.9F, 0.9F, 0.9F, 0.9F];    // RF, LF, RR, LR
			ReadOnlySpan<float> scale_targets2ch = [0.9F, 0.9F, 0.0F, 0.0F]; // RF, LF, RR, LR

			if (fmix2channels)
				scale_target = scale_targets2ch[Math.Clamp(ispeaker, 0, 3)];
			else
				scale_target = scale_targets[Math.Clamp(ispeaker, 0, 3)];

			goto XfadeExit;
		}

		if (cspeaker == 5) {
			// mono sound distribution:
			ReadOnlySpan<float> scale_targets = [0.9F, 0.9F, 0.5F, 0.5F, 0.9F];  // RF, LF, RR, LR, FC
			scale_target = scale_targets[Math.Clamp(ispeaker, 0, 4)];
			goto XfadeExit;
		}

		// if (cspeaker == 2 )
		scale_target = 0.9F; // front 2 speakers in stereo each get 50% of total volume in mono case

	XfadeExit:
		scale_out = scale + (scale_target - scale) * xfade;
		return scale_out;
	}

	// given:
	//  2d yaw angle to sound source (0-360), where 0 is listener_right
	//  pitch angle to source
	//  angle to speaker position (0-360), where 0 is listener_right
	//  speaker index
	//  speaker total count,
	// return: scale from 0-1.0 for speaker volume.
	// NOTE: as pitch angle goes to +/- 90, sound goes to mono, all speakers.

	const float PITCH_ANGLE_THRESHOLD = 45.0F;
	const float REAR_VOL_DROP = 0.5F;
	const float VOLCURVEPOWER = 1.5F;       // 1.0 is a linear crossfade of volume between speakers.
											// 1.5 provides a smoother, nonlinear volume transition - this is done
											// because a volume of 255 played in a single speaker is
											// percieved as louder than 128 + 128 in two speakers
											// separated by at least 45 degrees.  The nonlinear curve
											// gives the volume boost needed.

	static float GetSpeakerVol(float yaw_source, float pitch_source, float mono, float yaw_speaker, int ispeaker, int cspeaker, bool fmix2channels) {
		float adif = MathF.Abs(yaw_source - yaw_speaker);
		float pitch_angle = pitch_source;
		float scale = 0.0F;
		float xfade = 0.0F;

		if (adif > 180)
			adif = 360 - adif;

		// mono goes from 0.0 to 1.0 as listener moves into 'mono' radius of sound source.
		// Also, as pitch_angle to sound source approaches 90 (sound above/below listener), sounds become mono.

		// convert pitch angle to 0-90 absolute pitch
		if (pitch_angle < 0)
			pitch_angle += 360;

		if (pitch_angle > 180)
			pitch_angle = 360 - pitch_angle;

		if (pitch_angle > 90)
			pitch_angle = 90 - (pitch_angle - 90);

		// calculate additional mono crossfade due to pitch angle
		if (pitch_angle > PITCH_ANGLE_THRESHOLD) {
			xfade = (pitch_angle - PITCH_ANGLE_THRESHOLD) / (90.0F - PITCH_ANGLE_THRESHOLD);   // 0.0 -> 1.0 as angle 45->90

			mono += xfade;
			mono = Math.Clamp(mono, 0.0f, 1.0f);
		}

		if (cspeaker == 2) {
			// 2 speaker (headphone) mix: speakers opposing, at 0 & 180 degrees

			scale = 1.0F - MathF.Pow(adif / 180.0F, VOLCURVEPOWER);

			goto GetVolExit;
		}

		if (adif >= 90.0)
			goto GetVolExit;    // 0.0 scale

		if (cspeaker == 4) {
			// 4 ch surround: all speakers on 90 degree angles,
			// scale ranges from 0.0 (at 90 degree difference between source and speaker)
			// to 1.0 (0 degree difference between source and speaker)

			scale = 1.0F - MathF.Pow(adif / 90.0F, VOLCURVEPOWER);

			goto GetVolExit;
		}

		// 5 ch surround:

		// rear speakers are on 90 degree angles and return 0.0->1.0 range over +/- 90 degrees each
		// center speaker is on 45 degree angle to left/right front speaker
		// center speaker has 0.0->1.0 range over 45 degrees

		switch (ispeaker) {
			default:
			case ISPEAKER_RIGHT_REAR:
			case ISPEAKER_LEFT_REAR: {
					// rear speakers get +/- 90 degrees of linear scaling...
					scale = 1.0F - MathF.Pow(adif / 90.0F, VOLCURVEPOWER);
					break;
				}

			case ISPEAKER_CENTER_FRONT: {
					// center speaker gets +/- 45 degrees of linear scaling...
					if (adif > 45.0F)
						goto GetVolExit;    // 0.0 scale

					scale = 1.0F - MathF.Pow(adif / 45.0F, VOLCURVEPOWER);
					break;
				}
			case ISPEAKER_RIGHT_FRONT: {
					if (yaw_source > yaw_speaker) {
						// if sound source is between right front speaker and center speaker,
						// apply scaling over 75 degrees...

						if (adif > 75.0)
							goto GetVolExit;    // 0.0 scale

						scale = 1.0F - MathF.Pow(adif / 75.0F, VOLCURVEPOWER);
					}
					else {
						// sound source is CW from right speaker, apply scaling over 90 degrees...
						scale = 1.0F - MathF.Pow(adif / 90.0F, VOLCURVEPOWER);
					}

					break;
				}

			case ISPEAKER_LEFT_FRONT: {
					if (yaw_source < yaw_speaker) {
						// if sound source is between left front speaker and center speaker,
						// apply scaling over 75 degrees...

						if (adif > 75.0F)
							goto GetVolExit;    // 0.0 scale

						scale = 1.0F - MathF.Pow(adif / 75.0F, VOLCURVEPOWER);

					}
					else {
						// sound source is CW from right speaker, apply scaling over 90 degrees...
						scale = 1.0F - MathF.Pow(adif / 90.0F, VOLCURVEPOWER);
					}
					break;
				}
		}

	GetVolExit:
		Assert(mono <= 1.0 && mono >= 0.0);
		Assert(scale <= 1.0 && scale >= 0.0);

		// crossfade speaker volumes towards mono with increased pitch angle of sound source

		scale = XfadeSpeakerVolToMono(scale, mono, ispeaker, cspeaker, fmix2channels);

		Assert(scale <= 1.0 && scale >= 0.0);

		return scale;
	}

	// given unit vector from listener to sound source,
	// determine proportion of volume for sound in FL, FC, FR, RL, RR quadrants
	// Scale this proportion by the distance scalar 'gain'
	// If sound has 'mono' radius, blend sound to mono over 50% of radius.
	public virtual void SpatializeChannel(Span<int> volume, int master_vol, in Vector3 sourceDir, float gain, float mono) {
		float rfscale, rrscale, lfscale, lrscale, fcscale;

		fcscale = rfscale = lfscale = rrscale = lrscale = 0.0f;

		// clear volumes

		for (int i = 0; i < CCHANVOLUMES / 2; i++)
			volume[i] = 0;

		// linear crossfader for 2, 4 or 5 speakers, using polar coord. separation angle as linear basis

		// get pitch & yaw angle from listener origin to sound source

		QAngle angles;
		float pitch;
		float source_yaw;
		float yaw;

		MathLib.VectorAngles(sourceDir, out angles);

		pitch = angles[PITCH];
		source_yaw = angles[YAW];

		// get 2d listener yaw angle from listener right

		QAngle angles2d;
		Vector3 source2d;
		float listener_yaw;

		source2d.X = listener_right.X;
		source2d.Y = listener_right.Y;
		source2d.Z = 0.0f;

		MathLib.VectorNormalize(ref source2d);

		// convert right vector to euler angles (yaw & pitch)

		MathLib.VectorAngles(source2d, out angles2d);

		listener_yaw = angles2d[YAW];

		// get yaw of sound source, with listener_yaw as reference 0.

		yaw = source_yaw - listener_yaw;

		if (yaw < 0)
			yaw += 360;

		if (!Surround) {
			// 2 ch stereo mixing

			if (Headphone) {
				// headphone mix: (NO HRTF)

				rfscale = GetSpeakerVol(yaw, pitch, mono, 0.0f, ISPEAKER_RIGHT_FRONT, 2, false);
				lfscale = GetSpeakerVol(yaw, pitch, mono, 180.0f, ISPEAKER_LEFT_FRONT, 2, false);
			}
			else {
				// stereo speakers at 45 & 135 degrees: (mono sounds mix down to 2 channels)

				rfscale = GetSpeakerVol(yaw, pitch, mono, 45.0f, ISPEAKER_RIGHT_FRONT, 4, true);
				lfscale = GetSpeakerVol(yaw, pitch, mono, 135.0f, ISPEAKER_LEFT_FRONT, 4, true);
				rrscale = GetSpeakerVol(yaw, pitch, mono, 315.0f, ISPEAKER_RIGHT_REAR, 4, true);
				lrscale = GetSpeakerVol(yaw, pitch, mono, 225.0f, ISPEAKER_LEFT_REAR, 4, true);

				// add sounds coming from rear (quieter)

				rfscale = Math.Clamp(rfscale + rrscale * 0.75F, 0.0F, 1.0F);
				lfscale = Math.Clamp(lfscale + lrscale * 0.75F, 0.0F, 1.0F);

				rrscale = 0;
				lrscale = 0;
			}
			goto SpatialExit;
		}

		if (Surround && !SurroundCenter) {
			// 4 ch surround

			// linearly scale with radial distance from asource to FR, FL, RR, RL
			// where FR = 45 degrees, FL = 135, RR = 315 (-45), RL = 225 (-135)

			rfscale = GetSpeakerVol(yaw, pitch, mono, 45.0f, ISPEAKER_RIGHT_FRONT, 4, false);
			lfscale = GetSpeakerVol(yaw, pitch, mono, 135.0f, ISPEAKER_LEFT_FRONT, 4, false);
			rrscale = GetSpeakerVol(yaw, pitch, mono, 315.0f, ISPEAKER_RIGHT_REAR, 4, false);
			lrscale = GetSpeakerVol(yaw, pitch, mono, 225.0f, ISPEAKER_LEFT_REAR, 4, false);

			goto SpatialExit;
		}

		if (Surround && SurroundCenter) {
			// 5 ch surround

			// linearly scale with radial distance from asource to FR, FC, FL, RR, RL
			// where FR = 45 degrees, FC = 90, FL = 135, RR = 315 (-45), RL = 225 (-135)

			rfscale = GetSpeakerVol(yaw, pitch, mono, 45.0f, ISPEAKER_RIGHT_FRONT, 5, false);
			fcscale = GetSpeakerVol(yaw, pitch, mono, 90.0f, ISPEAKER_CENTER_FRONT, 5, false);
			lfscale = GetSpeakerVol(yaw, pitch, mono, 135.0f, ISPEAKER_LEFT_FRONT, 5, false);
			rrscale = GetSpeakerVol(yaw, pitch, mono, 315.0f, ISPEAKER_RIGHT_REAR, 5, false);
			lrscale = GetSpeakerVol(yaw, pitch, mono, 225.0f, ISPEAKER_LEFT_REAR, 5, false);

			goto SpatialExit;
		}

	SpatialExit:

		// scale volumes in each quadrant by distance attenuation.

		// volumes are 0-255:
		// gain is 0.0->1.0, rscale is 0.0->1.0, so scale is 0.0->1.0
		// master_vol is 0->255, so rightvol is 0->255

		volume[IFRONT_RIGHT] = (int)(master_vol * gain * rfscale);
		volume[IFRONT_LEFT] = (int)(master_vol * gain * lfscale);

		volume[IFRONT_RIGHT] = Math.Clamp(volume[IFRONT_RIGHT], 0, 255);
		volume[IFRONT_LEFT] = Math.Clamp(volume[IFRONT_LEFT], 0, 255);

		if (Surround) {
			volume[IREAR_RIGHT] = (int)(master_vol * gain * rrscale);
			volume[IREAR_LEFT] = (int)(master_vol * gain * lrscale);

			volume[IREAR_RIGHT] = Math.Clamp(volume[IREAR_RIGHT], 0, 255);
			volume[IREAR_LEFT] = Math.Clamp(volume[IREAR_LEFT], 0, 255);

			if (SurroundCenter) {
				volume[IFRONT_CENTER] = (int)(master_vol * gain * fcscale);
				volume[IFRONT_CENTER0] = 0;

				volume[IFRONT_CENTER] = Math.Clamp(volume[IFRONT_CENTER], 0, 255);
			}
		}
	}

	public virtual void ApplyDSPEffects(int idsp, PortableSamplePair[] pbuffront, PortableSamplePair[]? pbufrear, PortableSamplePair[]? pbufcenter, int samplecount) {
		DEBUG_StartSoundMeasure(1, samplecount);

		DSP_Process(idsp, pbuffront, pbufrear, pbufcenter, samplecount);

		DEBUG_StopSoundMeasure(1, samplecount);
	}

	public virtual void MixBegin(int sampleCount) {
		MIX_ClearAllPaintBuffers(sampleCount, false);
	}

	public virtual void MixUpsample(int sampleCount, int filtertype) {
		PaintBuffer paint = MIX_GetCurrentPaintbufferPtr();
		int ifilter = paint.IFilter;

		Assert(ifilter < CPAINTFILTERS);

		S_MixBufferUpsample2x(sampleCount, paint.Buf, paint.GetFltMem(ifilter), CPAINTFILTERMEM, filtertype);

		if (paint.Surround) {
			Assert(paint.BufRear != null);
			S_MixBufferUpsample2x(sampleCount, paint.BufRear!, paint.GetFltMemRear(ifilter), CPAINTFILTERMEM, filtertype);

			if (paint.SurroundCenter) {
				Assert(paint.BufCenter != null);
				S_MixBufferUpsample2x(sampleCount, paint.BufCenter!, paint.GetFltMemCenter(ifilter), CPAINTFILTERMEM, filtertype);
			}
		}

		// make sure on next upsample pass for this paintbuffer, new filter memory is used
		paint.IFilter++;
	}

	public virtual void Mix8Mono(Channel channel, ReadOnlySpan<byte> data, int outputOffset, int inputOffset, fixedint rateScaleFix, int outCount, int timecompress) {
		Span<int> volume = stackalloc int[CCHANVOLUMES];

		PaintBuffer paint = MIX_GetCurrentPaintbufferPtr();

		if (!MIX_ScaleChannelVolume(paint, channel, volume, 1))
			return;

		if (FVolumeFrontNonZero(volume))
			Mix8MonoWavtype(channel, paint.Buf.AsSpan(outputOffset), volume, data, inputOffset, rateScaleFix, outCount);

		if (paint.Surround) {
			if (FVolumeRearNonZero(volume)) {
				Assert(paint.BufRear != null);
				Mix8MonoWavtype(channel, paint.BufRear!.AsSpan(outputOffset), volume[IREAR_LEFT..], data, inputOffset, rateScaleFix, outCount);
			}

			if (paint.SurroundCenter && FVolumeCenterNonZero(volume)) {
				Assert(paint.BufCenter != null);
				Mix8MonoWavtype(channel, paint.BufCenter!.AsSpan(outputOffset), volume[IFRONT_CENTER..], data, inputOffset, rateScaleFix, outCount);
			}
		}
	}

	public virtual void Mix8Stereo(Channel channel, ReadOnlySpan<byte> data, int outputOffset, int inputOffset, fixedint rateScaleFix, int outCount, int timecompress) {
		Span<int> volume = stackalloc int[CCHANVOLUMES];

		PaintBuffer paint = MIX_GetCurrentPaintbufferPtr();

		if (!MIX_ScaleChannelVolume(paint, channel, volume, 2))
			return;

		if (FVolumeFrontNonZero(volume))
			Mix8StereoWavtype(channel, paint.Buf.AsSpan(outputOffset), volume, data, inputOffset, rateScaleFix, outCount);

		if (paint.Surround) {
			if (FVolumeRearNonZero(volume)) {
				Assert(paint.BufRear != null);
				Mix8StereoWavtype(channel, paint.BufRear!.AsSpan(outputOffset), volume[IREAR_LEFT..], data, inputOffset, rateScaleFix, outCount);
			}

			if (paint.SurroundCenter && FVolumeCenterNonZero(volume)) {
				Assert(paint.BufCenter != null);
				Mix8StereoWavtype(channel, paint.BufCenter!.AsSpan(outputOffset), volume[IFRONT_CENTER..], data, inputOffset, rateScaleFix, outCount);
			}
		}
	}

	public virtual void Mix16Mono(Channel channel, ReadOnlySpan<short> data, int outputOffset, int inputOffset, fixedint rateScaleFix, int outCount, int timecompress) {
		Span<int> volume = stackalloc int[CCHANVOLUMES];

		PaintBuffer paint = MIX_GetCurrentPaintbufferPtr();

		if (!MIX_ScaleChannelVolume(paint, channel, volume, 1))
			return;

		if (FVolumeFrontNonZero(volume))
			Mix16MonoWavtype(channel, paint.Buf.AsSpan(outputOffset), volume, data, inputOffset, rateScaleFix, outCount);

		if (paint.Surround) {
			if (FVolumeRearNonZero(volume)) {
				Assert(paint.BufRear != null);
				Mix16MonoWavtype(channel, paint.BufRear!.AsSpan(outputOffset), volume[IREAR_LEFT..], data, inputOffset, rateScaleFix, outCount);
			}

			if (paint.SurroundCenter && FVolumeCenterNonZero(volume)) {
				Assert(paint.BufCenter != null);
				Mix16MonoWavtype(channel, paint.BufCenter!.AsSpan(outputOffset), volume[IFRONT_CENTER..], data, inputOffset, rateScaleFix, outCount);
			}
		}
	}

	public virtual void Mix16Stereo(Channel channel, ReadOnlySpan<short> data, int outputOffset, int inputOffset, fixedint rateScaleFix, int outCount, int timecompress) {
		Span<int> volume = stackalloc int[CCHANVOLUMES];

		PaintBuffer paint = MIX_GetCurrentPaintbufferPtr();

		if (!MIX_ScaleChannelVolume(paint, channel, volume, 2))
			return;

		if (FVolumeFrontNonZero(volume))
			Mix16StereoWavtype(channel, paint.Buf.AsSpan(outputOffset), volume, data, inputOffset, rateScaleFix, outCount);

		if (paint.Surround) {
			if (FVolumeRearNonZero(volume)) {
				Assert(paint.BufRear != null);
				Mix16StereoWavtype(channel, paint.BufRear!.AsSpan(outputOffset), volume[IREAR_LEFT..], data, inputOffset, rateScaleFix, outCount);
			}

			if (paint.SurroundCenter && FVolumeCenterNonZero(volume)) {
				Assert(paint.BufCenter != null);
				Mix16StereoWavtype(channel, paint.BufCenter!.AsSpan(outputOffset), volume[IFRONT_CENTER..], data, inputOffset, rateScaleFix, outCount);
			}
		}
	}
}

// Null Audio Device
public class AudioDeviceNull : AudioDeviceBase
{
	public override bool IsActive() => false;
	public override bool Init() => true;
	public override void Shutdown() { }
	public override void Pause() { }
	public override void UnPause() { }
	public override float MixDryVolume() => 0;
	public override bool Should3DMix() => false;
	public override void StopAllSounds() { }

	public override int PaintBegin(float mixAheadTime, int soundtime, int paintedtime) => 0;
	public override void PaintEnd() { }

	public override void SpatializeChannel(Span<int> volume, int master_vol, in Vector3 sourceDir, float gain, float mono) { }
	public override void ApplyDSPEffects(int idsp, PortableSamplePair[] pbuffront, PortableSamplePair[]? pbufrear, PortableSamplePair[]? pbufcenter, int samplecount) { }
	public override int GetOutputPosition() => 0;
	public override void ClearBuffer() { }
	public override void UpdateListener(in Vector3 position, in Vector3 forward, in Vector3 right, in Vector3 up) { }

	public override void MixBegin(int sampleCount) { }
	public override void MixUpsample(int sampleCount, int filtertype) { }

	public override void Mix8Mono(Channel channel, ReadOnlySpan<byte> data, int outputOffset, int inputOffset, fixedint rateScaleFix, int outCount, int timecompress) { }
	public override void Mix8Stereo(Channel channel, ReadOnlySpan<byte> data, int outputOffset, int inputOffset, fixedint rateScaleFix, int outCount, int timecompress) { }
	public override void Mix16Mono(Channel channel, ReadOnlySpan<short> data, int outputOffset, int inputOffset, fixedint rateScaleFix, int outCount, int timecompress) { }
	public override void Mix16Stereo(Channel channel, ReadOnlySpan<short> data, int outputOffset, int inputOffset, fixedint rateScaleFix, int outCount, int timecompress) { }

	public override void ChannelReset(int entnum, int channelIndex, float distanceMod) { }
	public override void TransferSamples(int end) { }

	public override ReadOnlySpan<char> DeviceName() => "Audio Disabled";
	public override int DeviceChannels() => 2;
	public override int DeviceSampleBits() => 16;
	public override int DeviceSampleBytes() => 2;
	public override int DeviceDmaSpeed() => SOUND_DMA_SPEED;
	public override int DeviceSampleCount() => 0;

	public override bool IsSurround() => false;
	public override bool IsSurroundCenter() => false;
	public override bool IsHeadphone() => false;
}
