using Source.Common;
using Source.Common.Audio;
using Source.Common.Client;
using Source.Common.Commands;
using Source.Common.Filesystem;
using Source.Common.Mathematics;
using Source.Common.Networking;
using Source.Engine.Client;
using Source.Engine.Server;

using System;
using System.Diagnostics.Tracing;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Threading.Channels;

using static Source.Common.Networking.svc_ClassInfo;

using static Source.Engine.SoundGlobals;
namespace Source.Engine;

[EngineComponent]
public static class SoundGlobals
{

	public static readonly Dictionary<FileNameHandle_t, SfxTable> Sounds = [];
	[Dependency] public static IFileSystem filesystem = null!;

	[Dependency] public static GameServer sv = null!;
	[Dependency] public static ClientState cl = null!;
	[Dependency] public static IAudioSourceCache audiosourcecache = null!;
}

public class SfxTable : ISfxTable
{
	public AudioSource? Source { get; set; }
	public bool UseErrorFilename { get; set; }
	public bool IsUISound { get; set; }
	public bool IsLateLoad { get; set; }
	public bool MixGroupsCached { get; set; }
	public byte MixGroupCount { get; set; }

	public FileNameHandle_t NamePoolIndex;
	public void SetNamePoolIndex(FileNameHandle_t handle) {
		NamePoolIndex = handle;
		if (NamePoolIndex != FileNameHandle_t.MaxValue) {
			// on name changed todo
		}
	}
	public ReadOnlySpan<char> GetName() {
		if (Sounds.ContainsKey(NamePoolIndex)) {
			ReadOnlySpan<char> str = filesystem.String(NamePoolIndex);
			return str;
		}
		return null;
	}
	public ReadOnlySpan<char> GetFileName() {
		ReadOnlySpan<char> name = GetName();
		return !name.IsEmpty ? SoundCharsUtils.SkipSoundChars(name) : null;
	}

	public bool IsPrecachedSound() {
		ReadOnlySpan<char> name = GetName();
		if (sv.IsActive())
			return false; // Todo

		return cl.LookupSoundIndex(name) != -1 ? true : false;
	}
}

public partial class Sound
{
	readonly ConVar snd_surround = new("snd_surround_speakers", "-1", FCvar.InternalUse);
	readonly ConVar snd_legacy_surround = new("snd_legacy_surround", "0", FCvar.Archive);
	readonly ConVar snd_noextraupdate = new("snd_noextraupdate", "0", 0);
	readonly ConVar snd_show = new("snd_show", "0", FCvar.Cheat, "Show sounds info");
	readonly ConVar snd_visualize = new("snd_visualize", "0", FCvar.Cheat, "Show sounds location in world");
	readonly ConVar snd_pitchquality = new("snd_pitchquality", "1", FCvar.Archive);      // 1) use high quality pitch shifters

	readonly static ConVar volume = new("volume", "1.0", FCvar.Archive, "Sound volume", 0.0, 1.0);
	readonly ConVar snd_musicvolume = new("snd_musicvolume", "1.0", FCvar.Archive, "Music volume", 0.0, 1.0);

	readonly ConVar snd_mixahead = new("snd_mixahead", "0.1", FCvar.Archive);
	readonly ConVar snd_mix_async = new("snd_mix_async", "0", 0);

	public bool Initialized;
	public IAudioDevice? AudioDevice;

	public void Init() {
		if (AudioDevice == null || AudioDevice == Audio.GetNullDevice()) {
			AudioDevice = Audio.AutoDetectInit(false);
			if (AudioDevice == null) {
				Error("Unable to init audio");
			}
		}
	}

	readonly IFileSystem fileSystem;
	readonly ISoundServices soundServices;

	public Sound(IFileSystem fileSystem, ISoundServices soundServices) {
		ActiveChannels = new(this);
		this.fileSystem = fileSystem;
		this.soundServices = soundServices;
	}

	public void Startup() {

	}
	public SfxTable? PrecacheSound(ReadOnlySpan<char> name) {
		if (AudioDevice == null)
			return null;

		if (!AudioDevice.IsActive())
			return null;

		SfxTable? sfx = FindName(name, out _);
		if (sfx != null)
			LoadSound(sfx);
		else
			AssertMsg(false, "Sound.PrecacheSound: Failed to create sfx");

		return sfx;
	}

	private void LoadSound(SfxTable sfx) {
		LoadSound(sfx, null);
	}

	public static readonly ClassMemoryPool<SfxTable> SoundPool = new();
	double accumulatedSoundLoadTime = 0;

	private AudioSource? LoadSound(SfxTable sfx, AudioChannel? ch) {
		if (sfx.Source == null) {
			double st = Platform.Time;

			bool bUserVox = false;

			bool bStream = SoundCharsUtils.TestSoundChar(sfx.GetName(), SoundChars.Stream);
			if (!bStream)
				bUserVox = SoundCharsUtils.TestSoundChar(sfx.GetName(), SoundChars.UserVox);

			if (bStream)
				sfx.Source = Audio_CreateStreamedWave(sfx);
			else {
				if (bUserVox)
					sfx.Source = Voice_SetupAudioSource(ch!.SoundSource, ch.EntChannel);
				else // load all into memory directly
					sfx.Source = Audio_CreateMemoryWave(sfx);
			}

			double ed = Platform.Time;
			accumulatedSoundLoadTime += (ed - st);
		}
		else {
			sfx.Source.CheckAudioSourceCache();
		}

		if (sfx.Source == null)
			return null;


		// first time to load?  Create the mixer
		if (ch != null && ch.Mixer == null) {
			ch.Mixer = sfx.Source.CreateMixer(ch.InitialStreamPosition);
			if (ch.Mixer == null) {
				return null;
			}
		}

		return sfx.Source;
	}


	private SfxTable? FindName(ReadOnlySpan<char> inName, out bool inCache) {
		SfxTable? sfx = null;

		if (inName.IsEmpty)
			Error("Sound.FindName: NULL\n");

		ReadOnlySpan<char> name = inName;

		// see if already loaded
		FileNameHandle_t fnHandle = fileSystem.FindOrAddFileName(name);

		if (Sounds.TryGetValue(fnHandle, out sfx)) {
			inCache = (sfx.Source != null && sfx.Source.IsCached()) ? true : false;
			return sfx;
		}
		else {
			Sounds[fnHandle] = SoundPool.Alloc();
			sfx = Sounds[fnHandle];

			sfx.SetNamePoolIndex(fnHandle);
			sfx.Source = null;

			inCache = false;
		}

		return sfx;
	}

	public void MarkUISound(SfxTable sound) {
		sound.IsUISound = true;
	}

	internal long StartSound(in StartSoundParams parms) {
		if (parms.Sfx == null)
			return 0;

		if (parms.StaticSound)
			return StartStaticSound(parms);
		else
			return StartDynamicSound(parms);
	}
	public readonly ActiveChannels ActiveChannels;
	public readonly AudioChannel[] Channels = new AudioChannel[AudioChannel.MAX_CHANNELS].InstantiateArray();

	readonly ConVar snd_showstart = new("snd_showstart", "0", FCvar.Cheat);  // showstart always skips info on player footsteps!
																			 // 1 - show sound name, channel, volume, time 
																			 // 2 - show dspmix, distmix, dspface, l/r/f/r vols
																			 // 3 - show sound origin coords
																			 // 4 - show gain of dsp_room
																			 // 5 - show dB loss due to obscured sound
																			 // 6 - reserved
																			 // 7 - show 2 and total gain & dist in ft. to sound source

	static bool IsValidSampleRate(int rate) => rate == IAudioDevice.SOUND_11k || rate == IAudioDevice.SOUND_22k || rate == IAudioDevice.SOUND_44k;

	private long StartDynamicSound(StartSoundParams parms) {
		AudioChannel? target_chan;
		int vol;

		if (AudioDevice == null || !AudioDevice.IsActive())
			return 0;

		if (parms.Sfx == null)
			return 0;

		// For debugging to see the actual name of the sound...
		Span<char> sndname = stackalloc char[MAX_PATH];
		strcpy(sndname, parms.Sfx.GetName());

		if (SoundCharsUtils.TestSoundChar(sndname, SoundChars.Stream) && parms.EntChannel != SoundEntityChannel.Voice && parms.EntChannel != SoundEntityChannel.Voice2)
			parms.EntChannel = SoundEntityChannel.Stream;

		vol = (int)(parms.Volume * 255);

		if (vol > 255) {
			DevMsg($"S_StartDynamicSound: {sndname} volume > 255\n");
			vol = 255;
		}

		if ((parms.Flags & (SoundFlags.Stop | SoundFlags.ChangeVolume | SoundFlags.ChangePitch)) != 0) {
			if (AlterChannel(parms.SoundSource, parms.EntChannel, (SfxTable)parms.Sfx, vol, parms.Pitch, parms.Flags))
				return 0;
			if ((parms.Flags & SoundFlags.Stop) != 0)
				return 0;
			// fall through - if we're not trying to stop the sound, 
			// and we didn't find it (it's not playing), go ahead and start it up
		}

		if (parms.Pitch == 0) {
			DevMsg($"Warning: S_StartDynamicSound ({sndname}) Ignored, called with pitch 0\n");
			return 0;
		}

		// pick a channel to play on
		target_chan = PickDynamicChannel(parms.SoundSource, parms.EntChannel, parms.Origin, (SfxTable)parms.Sfx, parms.Delay, (parms.Flags & SoundFlags.DoNotOverwriteExistingOnChannel) != 0, out int channelIndex);
		if (target_chan == null)
			return 0;

		AudioDevice.ChannelReset(parms.SoundSource, channelIndex, target_chan.DistMult);

		bool bIsSentence = SoundCharsUtils.TestSoundChar(sndname, SoundChars.Sentence);

		ActivateChannel(target_chan);
		ChannelClearVolumes(target_chan);

		target_chan.UserData = parms.UserData;
		target_chan.InitialStreamPosition = parms.InitialStreamPosition;

		target_chan.Origin = parms.Origin;
		target_chan.Direction = parms.Direction;
		// never update positions if source entity is 0
		target_chan.UpdatePositions = parms.UpdatePositions && parms.SoundSource != 0;

		// reference_dist / (reference_power_level / actual_power_level)
		target_chan.CompatibilityAttenuation = SNDLEVEL_IS_COMPATIBILITY_MODE(parms.SoundLevel);
		if (target_chan.CompatibilityAttenuation) {
			// Translate soundlevel from its 'encoded' value to a real soundlevel that we can use in the sound system.
			parms.SoundLevel = SNDLEVEL_FROM_COMPATIBILITY_MODE(parms.SoundLevel);
		}

		target_chan.DistMult = SNDLVL_TO_DIST_MULT(parms.SoundLevel);

		SetChannelWavtype(target_chan, (SfxTable)parms.Sfx);

		target_chan.MasterVolume = (short)vol;
		target_chan.SoundSource = parms.SoundSource;
		target_chan.EntChannel = parms.EntChannel;
		target_chan.BasePitch = (short)parms.Pitch;
		target_chan.IsSentence = false;
		target_chan.Radius = 0;
		target_chan.Sfx = parms.Sfx;
		target_chan.SpecialDSP = parms.SpecialDSP;
		target_chan.FromServer = parms.FromServer;
		target_chan.Speaker = (parms.Flags & SoundFlags.Speaker) != 0;
		target_chan.SpeakerEntity = parms.SpeakerEntity;

		target_chan.ShouldPause = (parms.Flags & SoundFlags.ShouldPause) != 0;

		// initialize dsp room mixing parms
		target_chan.DSPMixMin = -1;
		target_chan.DSPMixMax = -1;

		AudioSource? pSource = null;
		if (bIsSentence) {
			// this is a sentence
			// link all words and load the first word

			// NOTE: sentence names stored in the cache lookup are
			// prepended with a '!'.  Sentence names stored in the
			// sentence file do not have a leading '!'. 
			VOX_LoadSound(target_chan, SoundCharsUtils.SkipSoundChars(sndname));
		}
		else {
			// regular or streamed sound fx
			pSource = LoadSound((SfxTable)parms.Sfx, target_chan);
			if (pSource != null && !IsValidSampleRate(pSource.SampleRate()))
				Warning($"*** Invalid sample rate ({pSource.SampleRate()}) for sound '{sndname}'.\n");

			if (pSource != null && !parms.Sfx.IsLateLoad)
				Warning($"Failed to load sound \"{sndname}\", file probably missing from disk/repository\n");
		}

		if (target_chan.Mixer == null) {
			// couldn't load the sound's data, or sentence has 0 words (this is not an error)
			FreeChannel(target_chan);
			return 0;
		}

		int nSndShowStart = snd_showstart.GetInt();

		// TODO: Support looping sounds through speakers.
		// If the sound is from a speaker, and it's looping, ignore it.
		if (target_chan.Speaker) {
			if (parms.Sfx.Source != null && parms.Sfx.Source.IsLooped()) {
				if (nSndShowStart > 0 && nSndShowStart < 7 && nSndShowStart != 4)
					DevMsg($"DynamicSound : Speaker ignored looping sound: {sndname}\n");

				FreeChannel(target_chan);
				return 0;
			}
		}

		SetChannelStereo(target_chan, pSource);

		if (nSndShowStart == 5) {
			snd_showstart.SetValue(6);
			nSndShowStart = 6;
		}

		// get sound type before we spatialize
		MXR_GetMixGroupFromSoundsource(target_chan, parms.SoundSource, parms.SoundLevel);

		// skip the trace on the first spatialization.  This channel may be stolen
		// by another sound played this frame.  Defer the trace to the mix loop
		SpatializeFirstFrameNoTrace(target_chan);

		if (nSndShowStart > 0 && nSndShowStart < 7 && nSndShowStart != 4) {
			AudioChannel? pTargetChan = target_chan;

			DevMsg($"DynamicSound {sndname} : src {parms.SoundSource} : channel {parms.EntChannel} : {parms.SoundLevel} dB : vol {parms.Volume:0.00} : time {soundServices.GetHostTime():0.000}\n");
			if (nSndShowStart == 2 || nSndShowStart == 5)
				DevMsg($"\t dspmix {pTargetChan.DSPMix:0.00} : distmix {pTargetChan.DistMix:0.00} : dspface {pTargetChan.DSPFace:0.00} : lvol {pTargetChan.Volume[IFRONT_LEFT]:0.00} : cvol {pTargetChan.Volume[IFRONT_CENTER]:0.00} : rvol {pTargetChan.Volume[IFRONT_RIGHT]:0.00} : rlvol {pTargetChan.Volume[IREAR_LEFT]:0.00} : rrvol {pTargetChan.Volume[IREAR_RIGHT]:0.00}\n");
			if (nSndShowStart == 3)
				DevMsg($"\t x: {pTargetChan.Origin.X:0.0000} y: {pTargetChan.Origin.Y:0.0000} z: {pTargetChan.Origin.Z:0.0000}\n");

			// if (snd_visualize.GetInt() != 0)
			// DebugOverlay.AddTextOverlay(pTargetChan->origin, 2.0f, sndname);
		}

		// If a client can't hear a sound when they FIRST receive the StartSound message,
		// the client will never be able to hear that sound. This is so that out of 
		// range sounds don't fill the playback buffer.  For streaming sounds, we bypass this optimization.

		if (BChannelLowVolume(target_chan, 0)) {
			// Looping sounds don't use this optimization because they should stick around until they're killed.
			// Also bypass for speech (GetSentence)
			if (parms.Sfx.Source == null || (!parms.Sfx.Source.IsLooped() && parms.Sfx.Source.GetSentence().IsEmpty)) {
				// if this is long sound, play the whole thing.
				if (!SND_IsLongWave(target_chan)) {
					// DevMsg("S_StartDynamicSound: spatialized to 0 vol & ignored %s", sndname);
					FreeChannel(target_chan);
					return 0;       // not audible at all
				}
			}
		}

		// Init client entity mouth movement vars
		target_chan.IgnorePhonemes = (parms.Flags & SoundFlags.IgnorePhonemes) != 0;
		// SND_InitMouth(target_chan);

		// Pre-startup delay.  Compute # of samples over which to mix in zeros from data source before
		//  actually reading first set of samples
		if (parms.Delay != 0.0f) {
			Assert(target_chan.Sfx != null);
			Assert(target_chan.Sfx.Source != null);

			int rate = target_chan.Sfx.Source!.SampleRate();
			int delaySamples = (int)(parms.Delay * rate);

			if (parms.Delay > 0) {
				target_chan.Mixer.SetStartupDelaySamples(delaySamples);
				target_chan.DelayedStart = true;
			}
			else {
				int skipSamples = -delaySamples;
				int totalSamples = target_chan.Sfx.Source!.SampleCount();

				if (target_chan.Sfx.Source.IsLooped())
					skipSamples = skipSamples % totalSamples;

				if (skipSamples >= totalSamples) {
					FreeChannel(target_chan);
					return 0;
				}
				target_chan.Pitch = target_chan.BasePitch * 0.01f;
				target_chan.Mixer.SkipSamples(target_chan, skipSamples, rate, 0);
				target_chan.OBGainTarget = 1.0f;
				target_chan.OBGain = 1.0f;
				target_chan.OBGainInc = 0.0f;
				target_chan.FirstPass = false;
				target_chan.DelayedStart = true;
			}
		}

		soundServices.OnSoundStarted(target_chan.GUID, ref parms, sndname);
		return target_chan.GUID;
	}

	private void MXR_GetMixGroupFromSoundsource(AudioChannel target_chan, int soundSource, SoundLevel soundLevel) {
		// todo
	}

	readonly ConVar snd_defer_trace = new("snd_defer_trace", "1", 0);

	private void SpatializeFirstFrameNoTrace(AudioChannel channel) {
		if (snd_defer_trace.GetBool()) {
			// set up tracing state to be non-obstructed
			channel.FirstPass = false;
			channel.Traced = true;
			channel.OBGain = 1.0f;
			channel.OBGainInc = 1.0f;
			channel.OBGainTarget = 1.0f;
			// now spatialize without tracing
			Spatialize(channel);
			// now reset tracing state to firstpass so the trace gets done on next spatialize
			channel.OBGain = 0.0f;
			channel.OBGainInc = 0.0f;
			channel.OBGainTarget = 0.0f;
			channel.FirstPass = true;
			channel.Traced = false;
		}
		else {
			channel.OBGain = 0.0f;
			channel.OBGainInc = 0.0f;
			channel.OBGainTarget = 0.0f;
			channel.FirstPass = true;
			channel.Traced = false;
			Spatialize(channel);
		}
	}

	private void Spatialize(AudioChannel channel) {
		// todo
	}

	private bool SND_IsLongWave(AudioChannel channel) {
		AudioSource? source = channel.Sfx?.Source;
		if (source != null)
			if (source.IsStreaming())
				return true;

		return false;
	}

	private bool BChannelLowVolume(AudioChannel ch, int volMin) {
		int max = -1;
		int max_target = -1;
		int vol;
		int vol_target;

		for (int i = 0; i < CCHANVOLUMES; i++) {
			vol = (int)(ch.Volume[i]);
			vol_target = (int)(ch.VolumeTarget[i]);

			if (vol > max)
				max = vol;

			if (vol_target > max_target)
				max_target = vol_target;
		}

		return (max <= volMin && max_target <= volMin);
	}

	private void SetChannelStereo(AudioChannel target_chan, AudioSource? pSource) {
		throw new NotImplementedException();
	}

	private void FreeChannel(AudioChannel ch) {
		if (ch.IsFreeingChannel)
			return;
		ch.IsFreeingChannel = true;

		CloseMouth(ch);

		soundServices.OnSoundStopped(ch.GUID, ch.SoundSource, ch.EntChannel, ch.Sfx!.GetName());

		ch.IsSentence = false;

		ch.Mixer?.Free();
		ch.Mixer = null;
		ch.Sfx = null;

		// zero all data in channel
		ActiveChannels.Remove(ch);
		ch.ClearInstantiatedReference();
	}

	private void CloseMouth(AudioChannel ch) {

	}

	private void SetChannelWavtype(AudioChannel channel, SfxTable sfx) {
		if (SoundCharsUtils.TestSoundChar(sfx.GetName(), SoundChars.DryMix))
			channel.Dry = true;
		else
			channel.Dry = false;

		if (SoundCharsUtils.TestSoundChar(sfx.GetName(), SoundChars.FastPitch))
			channel.FastPitch= true;
		else
			channel.FastPitch = false;

		// get sound spatialization encoding

		channel.WavType = 0;

		if (SoundCharsUtils.TestSoundChar(sfx.GetName(), SoundChars.Doppler))
			channel.WavType = SoundChars.Doppler;

		if (SoundCharsUtils.TestSoundChar(sfx.GetName(), SoundChars.Directional))
			channel.WavType = SoundChars.Directional;

		if (SoundCharsUtils.TestSoundChar(sfx.GetName(), SoundChars.DistVariant))
			channel.WavType = SoundChars.DistVariant;

		if (SoundCharsUtils.TestSoundChar(sfx.GetName(), SoundChars.Omni))
			channel.WavType = SoundChars.Omni;

		if (SoundCharsUtils.TestSoundChar(sfx.GetName(), SoundChars.SpatialStereo))
			channel.WavType = SoundChars.SpatialStereo;
	}

	static long SoundGuid;

	private void ActivateChannel(AudioChannel channel) {
		channel.ClearInstantiatedReference();
		ActiveChannels.Add(channel);
		channel.GUID = ++SoundGuid;
	}

	private void ChannelClearVolumes(AudioChannel ch) {
		for (int i = 0; i < CCHANVOLUMES; i++) {
			ch.Volume[i] = 0.0f;
			ch.VolumeTarget[i] = 0.0f;
			ch.VolumeInc[i] = 0.0f;
		}
	}

	private AudioChannel? PickDynamicChannel(int soundsource, SoundEntityChannel entchannel, Vector3 origin, SfxTable sfx, float delay, bool doNotOverwriteExisting, out int channelIndex) {
		AudioChannel? channel = StealDynamicChannel(soundsource, entchannel, origin, sfx, delay, doNotOverwriteExisting, out channelIndex);
		if (channel == null)
			return null;

		if (channel.Sfx != null) {
			// Don't restart looping sounds for the same entity
			AudioSource? source = channel.Sfx.Source;
			if (source != null) {
				if (source.IsLooped()) {
					if (channel.SoundSource == soundsource && channel.EntChannel == entchannel && channel.Sfx == sfx) {
						// same looping sound, same ent, same channel, don't restart the sound
						return null;
					}
				}
			}
			FreeChannel(channel);
		}

		return channel;
	}
	readonly ConVar voice_steal = new("voice_steal", "2", 0);

	private AudioChannel? StealDynamicChannel(int soundsource, SoundEntityChannel entchannel, Vector3 origin, SfxTable sfx, float delay, bool doNotOverwriteExisting, out int channelIndex) {
		channelIndex = -1;
		Span<int> canSteal = stackalloc int[AudioChannel.MAX_DYNAMIC_CHANNELS];
		int canStealCount = 0;

		int sameSoundCount = 0;
		uint sameSoundRemaining = 0xFFFFFFFF;
		int sameSoundIndex = -1;
		int sameVol = 0xFFFF;
		int availableChannel = -1;
		bool delaySame = false;

		Span<int> exactMatch = stackalloc int[AudioChannel.MAX_DYNAMIC_CHANNELS];
		int nExactCount = 0;
		// first pass to replace sounds on same ent/channel, and search for free or stealable channels otherwise
		for (int ch_idx = 0; ch_idx < AudioChannel.MAX_DYNAMIC_CHANNELS; ch_idx++) {
			AudioChannel ch = Channels[ch_idx];

			if (ch.ActiveIndex != 0) {
				// channel CHAN_AUTO never overrides sounds on same channel
				if (entchannel != SoundEntityChannel.Auto) {
					SoundEntityChannel checkChannel = entchannel;
					if (checkChannel == SoundEntityChannel.Replace) {
						if (ch.EntChannel != SoundEntityChannel.Stream && ch.EntChannel != SoundEntityChannel.Voice && ch.EntChannel != SoundEntityChannel.Voice2) {
							checkChannel = ch.EntChannel;
						}
					}
					if (ch.SoundSource == soundsource && (soundsource != -1) && ch.EntChannel == checkChannel) {
						// we found an exact match for this entity and this channel, but the sound we want to play is considered
						// low priority so instead of stomping this entry pretend we couldn't find a free slot to play and let
						// the existing sound keep going
						if (doNotOverwriteExisting)
							return null;

						if (ch.DelayedStart) {
							exactMatch[nExactCount] = ch_idx;
							nExactCount++;
							continue;
						}
						channelIndex = ch_idx;
						return ch;  // always override sound from same entity
					}
				}

				// Never steal the channel of a streaming sound that is currently playing or
				// voice over IP data that is playing or any sound on CHAN_VOICE( acting )
				if (ch.EntChannel == SoundEntityChannel.Stream || ch.EntChannel == SoundEntityChannel.Voice || ch.EntChannel == SoundEntityChannel.Voice2)
					continue;

				// don't let monster sounds override player sounds
				if (soundServices.IsPlayer(ch.SoundSource) && !soundServices.IsPlayer(soundsource))
					continue;

				if (ch.Sfx == sfx) {
					delaySame = ch.DelayedStart ? true : delaySame;
					sameSoundCount++;
					int maxVolume = ChannelGetMaxVol(ch);
					uint remaining = RemainingSamples(ch);
					if (maxVolume < sameVol || (maxVolume == sameVol && remaining < sameSoundRemaining)) {
						sameSoundIndex = ch_idx;
						sameVol = maxVolume;
						sameSoundRemaining = remaining;
					}
				}
				canSteal[canStealCount++] = ch_idx;
			}
			else {
				if (availableChannel < 0) {
					availableChannel = ch_idx;
				}
			}
		}


		// coalesce the timeline for this channel
		if (nExactCount > 0) {
			uint nFreeSampleTime = (uint)(paintedtime + (delay * IAudioDevice.SOUND_DMA_SPEED));
			AudioChannel pReturn = Channels[exactMatch[0]];
			uint nMinRemaining = RemainingSamples(pReturn);
			if (pReturn.FreeChannelAtSampleTime == 0 || pReturn.FreeChannelAtSampleTime > nFreeSampleTime) 
				pReturn.FreeChannelAtSampleTime = nFreeSampleTime;
			for (int i = 1; i < nExactCount; i++) {
				AudioChannel pChannel = Channels[exactMatch[i]];
				if (pChannel.FreeChannelAtSampleTime == 0 || pChannel.FreeChannelAtSampleTime > nFreeSampleTime) 
					pChannel.FreeChannelAtSampleTime = nFreeSampleTime;
				uint nRemain = RemainingSamples(pChannel);
				if (nRemain < nMinRemaining) {
					pReturn = pChannel;
					nMinRemaining = nRemain;
				}
			}
			// if there's only one, mark it to be freed but don't reuse it.
			// otherwise mark all others to be freed and use the closest one to being done
			if (nExactCount > 1) {
				channelIndex = exactMatch[0];
				return pReturn;
			}
		}

		// Limit the number of times a given sfx/wave can play simultaneously
		if (voice_steal.GetInt() > 1 && sameSoundIndex >= 0) {
			// if sounds of this type are normally delayed, then add an extra slot for stealing
			// NOTE: In HL2 these are usually NPC gunshot sounds - and stealing too soon will cut
			// them off early.  This is a safe heuristic to avoid that problem.  There's probably a better
			// long-term solution involving only counting channels that are actually going to play (delay included)
			// at the same time as this one.
			int maxSameSounds = delaySame ? 5 : 4;
			float distSqr = 0.0f;
			if (sfx.Source != null) {
				distSqr = origin.DistToSqr(ListenerOrigin);
				if (sfx.Source.IsLooped()) 
					maxSameSounds = 3;
			}

			// don't play more than N copies of the same sound, steal the quietest & closest one otherwise
			if (sameSoundCount >= maxSameSounds) {
				AudioChannel ch = Channels[sameSoundIndex];
				// you're already playing a closer version of this sound, don't steal
				if (distSqr > 0.0f && ch.Origin.DistToSqr(ListenerOrigin) < distSqr && entchannel != SoundEntityChannel.Weapon)
					return null;

				//Msg("Sound playing %d copies, stole %s (%d)\n", sameSoundCount, ch->sfx->getname(), sameVol );
				channelIndex = sameSoundIndex;
				return ch;
			}
		}

		// if there's a free channel, just take that one - don't steal
		if (availableChannel >= 0) {
			channelIndex = availableChannel;
			return Channels[availableChannel];
		}
		// Still haven't found a suitable channel, so choose the one with the least amount of time left to play
		float life_left = float.MaxValue;
		int first_to_die = -1;
		bool bAllowVoiceSteal = voice_steal.GetBool();

		for (int i = 0; i < canStealCount; i++) {
			int ch_idx = canSteal[i];
			AudioChannel ch = Channels[ch_idx];
			float timeleft = 0;
			if (bAllowVoiceSteal) {
				int maxVolume = ChannelGetMaxVol(ch);
				if (maxVolume < 5) {
					//Msg("Sound quiet, stole %s for %s\n", ch->sfx->getname(), sfx->getname() );
					channelIndex = ch_idx;
					return ch;
				}

				if (ch.Sfx != null && ch.Sfx.Source != null) {
					uint sampleCount = RemainingSamples(ch);
					timeleft = (float)sampleCount / (float)ch.Sfx.Source.SampleRate();
				}
			}
			else {
				// UNDONE: Kill this when voice_steal 0,1,2 has been tested
				// UNDONE: This is the old buggy code that we're trying to replace
				if (ch.Sfx != null) {
					// basically steals the first one you come to
					timeleft = 1;   //ch->end - paintedtime
				}
			}

			if (timeleft < life_left) {
				life_left = timeleft;
				first_to_die = ch_idx;
			}
		}
		if (first_to_die >= 0) {
			//Msg("Stole %s, timeleft %d\n", channels[first_to_die].sfx->getname(), life_left );
			channelIndex = first_to_die;
			return Channels[first_to_die];
		}

		return null;
	}

	private static uint RemainingSamples(AudioChannel channel) {
		if (channel == null || channel.Sfx == null || channel.Sfx.Source == null)
			return 0;

		uint timeleft = (uint)channel.Sfx.Source.SampleCount();

		if (channel.Sfx.Source.IsLooped()) 
			return (uint)channel.Sfx.Source.SampleRate();

		if (channel.Mixer != null)
			timeleft -= (uint)channel.Mixer.GetSamplePosition();

		return timeleft;
	}

	private static int ChannelGetMaxVol(AudioChannel ch) {
		float max = 0.0f;

		for (int i = 0; i < CCHANVOLUMES; i++) 
			if (ch.Volume[i] > max)
				max = ch.Volume[i];

		return (int)max;
	}

	private bool AlterChannel(int soundSource, SoundEntityChannel entChannel, SfxTable sfx, int vol, int pitch, SoundFlags flags) {
		throw new NotImplementedException();
	}

	private int StartStaticSound(in StartSoundParams parms) {
		throw new NotImplementedException();
	}

	internal void Shutdown() {

	}

	Vector3 ListenerOrigin;
	Vector3 ListenerForward;
	Vector3 ListenerRight;
	Vector3 ListenerUp;
	bool IsListenerUnderwater;

	internal void Update() {
		if (!AudioDevice!.IsActive())
			return;

		UpdateSoundFade();

		ListenerOrigin = vec3_origin;
		ListenerForward = vec3_origin;
		ListenerRight = vec3_origin;
		ListenerUp = vec3_origin;
		IsListenerUnderwater = false;

		PerformUpdate();
	}

	internal void Update(in AudioState audioState) {
		if (!AudioDevice!.IsActive())
			return;

		UpdateSoundFade();

		ListenerOrigin = audioState.Origin;
		MathLib.AngleVectors(in audioState.Angles, out ListenerForward, out ListenerRight, out ListenerUp);
		IsListenerUnderwater = audioState.IsUnderwater;

		PerformUpdate();
	}

	TimeUnit_t LastSoundFrame;
	TimeUnit_t LastMixTime;
	TimeUnit_t EstFrameTime;

	private void PerformUpdate() {
		// Something should've set up the ListenerOrigin/ListenerDirection/IsListenerUnderwater variables before calling this method!

		AudioDevice!.UpdateListener(ListenerOrigin, ListenerForward, ListenerRight, ListenerUp);
		int voiceChannelCount = 0;
		int voiceChannelMaxVolume = 0;

		TimeUnit_t now = Platform.Time;
		LastSoundFrame = now;
		LastMixTime = now;
		EstFrameTime = (EstFrameTime * 0.9f) + (soundServices.GetHostFrametime() * 0.1f);
		Update_(EstFrameTime + snd_mixahead.GetDouble());
	}

	private void Update_(double mixAheadTime) {
		// TODO: snd_mix_async?

		ShutdownMixThread();
		UpdateGuts(mixAheadTime);
	}

	private void ShutdownMixThread() { }

	int oldSampleOutCount;
	int buffers;
	int paintedtime;
	int soundtime;
	void GetSoundTime() {
		int fullsamples = AudioDevice!.DeviceSampleCount() / AudioDevice.DeviceChannels();
		int sampleOutCount = AudioDevice.GetOutputPosition();
		if (sampleOutCount < oldSampleOutCount) {
			// buffer wrapped
			buffers++;
			if (paintedtime > 0x70000000) {
				// time to chop things off to avoid 32 bit limits
				buffers = 0;
				paintedtime = fullsamples;
				StopAllSounds(true);
			}
		}

		oldSampleOutCount = sampleOutCount;

		// No cl_movieinfo/replay rn
		soundtime = buffers * fullsamples + sampleOutCount;
	}

	private void StopAllSounds(bool clear) {
		if (AudioDevice == null)
			return;

		if (!AudioDevice.IsActive())
			return;

		// todo
	}

	private void UpdateGuts(double mixAheadTime) {
		GetSoundTime();

		uint endtime = (uint)AudioDevice!.PaintBegin(mixAheadTime, soundtime, paintedtime);
		int samples = (int)(endtime - paintedtime);
		samples = samples < 0 ? 0 : samples;
		if (samples != 0) {
			MIX_PaintChannels(endtime, IsListenerUnderwater);
			MXR_DebugShowMixVolumes();
			MXR_UpdateAllDuckerVolumes();
		}

		AudioDevice!.PaintEnd();
	}

	private void UpdateSoundFade() {

	}

	// todo: everything else here
	// Further research is needed on audio systems
}
