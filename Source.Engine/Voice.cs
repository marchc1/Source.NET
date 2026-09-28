global using static Source.Engine.VoiceGlobals;

using CommunityToolkit.HighPerformance;

using DStruct.Tries;

using Source.Common;
using Source.Common.Audio;
using Source.Common.Commands;
using Source.Common.Utilities;

using Steamworks;

using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Channels;

namespace Source.Engine;

public static class VoiceGlobals
{
	public const int VOICE_OUTPUT_SAMPLE_RATE_LOW = 11025;  // Sample rate that we feed to the mixer.
	public const int VOICE_OUTPUT_SAMPLE_RATE_HIGH = 22050; // Sample rate that we feed to the mixer.
	public const int VOICE_OUTPUT_SAMPLE_RATE_MAX = 22050;  // Sample rate that we feed to the mixer.
	public const int BYTES_PER_SAMPLE = 2;

	public const int TWEAKMODE_ENTITYINDEX = -500;
	public const int TWEAKMODE_CHANNELINDEX = -100;

	public const int VOICE_CHANNEL_ERROR = -1;
	public const int VOICE_CHANNEL_IN_TWEAK_MODE = -2;

	public const int VOICE_RECEIVE_BUFFER_SIZE = VOICE_OUTPUT_SAMPLE_RATE_MAX * BYTES_PER_SAMPLE;
}

// ----------------------------------------------------------------------- //
// AutoGain is fed samples and figures out a gain to apply to blocks of samples.

// Right now, this class applies gain one block behind. The assumption is that the blocks are
// small enough that gain settings for one block will usually be right for the next block.

// The ideal way to implement this class would be to have a delay the size of a block
// so it can apply the right gain to the actual block it was calculated for.
// ----------------------------------------------------------------------- //
public class AutoGain
{
	const int AG_FIX_SHIFT = 7;

	// Parameters affecting the algorithm.
	int BlockSize;           // Derive gain from blocks of this size.
	float MaxGain;
	float AvgToMaxVal;

	// These are calculated as samples are passed in.
	int CurBlockOffset;
	int CurTotal;                // Total of sample values in current block.
	int CurMax;              // Highest (absolute) sample value.

	float Scale;             // All samples are scaled by this amount.

	float CurrentGain;           // Gain at sample 0 in this block.
	float NextGain;              // Gain at the last sample in this block.

	int FixedCurrentGain;        // Fixed-point m_CurrentGain.
	int GainMultiplier;      // (m_NextGain - m_CurrentGain) / (m_BlockSize - 1).

	public AutoGain() {
		Reset(128, 5.0f, 0.5f, 1);
	}

	// maxGain and avgToMaxVal are used to derive the gain amount for each block of samples.
	// All samples are scaled by scale.
	public void Reset(int blockSize, float maxGain, float avgToMaxVal, float scale) {
		BlockSize = blockSize;
		MaxGain = maxGain;
		AvgToMaxVal = avgToMaxVal;

		CurBlockOffset = 0;
		CurTotal = 0;
		CurMax = 0;

		CurrentGain = 1;
		NextGain = 1;

		Scale = scale;

		GainMultiplier = 0;
		FixedCurrentGain = 1 << AG_FIX_SHIFT;
	}

	// Process the specified samples and apply gain to them.
	public void ProcessSamples(Span<short> samples, int nSamples) {
		int curPos = 0;
		int nSamplesLeft = nSamples;

		// Continue until we hit the end of this block.
		while (nSamplesLeft != 0) {
			int nToProcess = Math.Min(nSamplesLeft, (BlockSize - CurBlockOffset));
			for (int iSample = 0; iSample < nToProcess; iSample++) {
				// Update running totals..
				CurTotal += Math.Abs((int)samples[curPos + iSample]);
				CurMax = Math.Max(CurMax, Math.Abs((int)samples[curPos + iSample]));

				// Apply gain on this sample.
				int gain = FixedCurrentGain + CurBlockOffset * GainMultiplier;
				CurBlockOffset++;

				int newval = ((int)samples[curPos + iSample] * gain) >> AG_FIX_SHIFT;
				newval = Math.Min(32767, Math.Max(newval, -32768));
				samples[curPos + iSample] = (short)newval;
			}
			curPos += nToProcess;
			nSamplesLeft -= nToProcess;

			// Did we just end a block? Update our next gain.
			if ((CurBlockOffset % BlockSize) == 0) {
				// Now we've interpolated to our next gain, make it our current gain.
				CurrentGain = NextGain * Scale;
				FixedCurrentGain = (int)((double)CurrentGain * (1 << AG_FIX_SHIFT));

				// Figure out the next gain (the gain we'll interpolate to).
				int avg = CurTotal / BlockSize;
				float modifiedMax = avg + (CurMax - avg) * AvgToMaxVal;
				NextGain = Math.Min(32767.0f / modifiedMax, MaxGain) * Scale;

				// Setup the interpolation multiplier.
				float fGainMultiplier = (NextGain - CurrentGain) / (BlockSize - 1);
				GainMultiplier = (int)((double)fGainMultiplier * (1 << AG_FIX_SHIFT));

				// Reset counters.
				CurTotal = 0;
				CurMax = 0;
				CurBlockOffset = 0;
			}
		}
	}
}

public static class VoiceWaveFile
{
	public static bool ReadWaveFile(ReadOnlySpan<char> filename, out byte[]? data, out int nDataBytes, out int bitsPerSample, out int nChannels, out int nSamplesPerSec) {
		data = null;
		nDataBytes = bitsPerSample = nChannels = nSamplesPerSec = 0;

		FileStream fp;
		try {
			fp = new FileStream(new string(filename), FileMode.Open, FileAccess.Read);
		}
		catch {
			return false;
		}

		using (fp) {
			using BinaryReader br = new(fp);
			fp.Seek(22, SeekOrigin.Begin);

			nChannels = br.ReadUInt16();
			nSamplesPerSec = (int)br.ReadUInt32();

			fp.Seek(34, SeekOrigin.Begin);
			bitsPerSample = br.ReadUInt16();

			fp.Seek(40, SeekOrigin.Begin);
			nDataBytes = (int)br.ReadUInt32();
			br.ReadUInt32();
			data = new byte[nDataBytes];
			fp.ReadAtLeast(data, nDataBytes, false);
		}
		return true;
	}

	public static bool WriteWaveFile(ReadOnlySpan<char> filename, ReadOnlySpan<byte> data, int nBytes, int bitsPerSample, int nChannels, int nSamplesPerSec) {
		FileStream fp;
		try {
			fp = new FileStream(new string(filename), FileMode.Create, FileAccess.Write);
		}
		catch {
			return false;
		}

		using (fp) {
			using BinaryWriter bw = new(fp);

			// Write the RIFF chunk.
			bw.Write("RIFF"u8);
			bw.Write(0u);
			bw.Write("WAVE"u8);


			// Write the FORMAT chunk.
			bw.Write("fmt "u8);

			bw.Write(0x10u);
			bw.Write((ushort)1);   // WAVE_FORMAT_PCM
			bw.Write((ushort)nChannels);
			bw.Write((uint)nSamplesPerSec);
			bw.Write((uint)((bitsPerSample / 8) * nChannels * nSamplesPerSec));
			bw.Write((ushort)((bitsPerSample / 8) * nChannels));
			bw.Write((ushort)bitsPerSample);

			// Write the DATA chunk.
			bw.Write("data"u8);
			bw.Write((uint)nBytes);
			bw.Write(data[..nBytes]);


			// Go back and write the length of the riff file.
			uint dwVal = (uint)(fp.Position - 8);
			fp.Seek(4, SeekOrigin.Begin);
			bw.Write(dwVal);
		}
		return true;
	}
}

public enum MixerControl
{
	// Microphone boost is a boolean switch that sound cards support which boosts the input signal by about +20dB.
	// If this isn't on, the mic is usually way too quiet.
	MicBoost = 0,

	// Volume values are 0-1.
	MicVolume,

	// Mic playback muting. You usually want this set to false, otherwise the sound card echoes whatever you say into the mic.
	MicMute,

	NumControls
}

public interface IMixerControls
{
	bool GetValue_Float(MixerControl control, ref float value);
	bool SetValue_Float(MixerControl control, float value);

	// Apps like RealJukebox will switch the waveIn input to use CD audio
	// rather than the microphone. This should be called at startup to set it back.
	bool SelectMicrophoneForWaveInput();
}

public class MixerControls : IMixerControls
{
	public static IMixerControls? g_pMixerControls = null;

	public void Release() { }
	public bool GetValue_Float(MixerControl control, ref float value) => false;
	public bool SetValue_Float(MixerControl control, float value) => false;
	public bool SelectMicrophoneForWaveInput() => false;
	public ReadOnlySpan<char> GetMixerName() => "SDL";

	// Allocates a set of mixer controls.
	public static void InitMixerControls() {
		if (g_pMixerControls == null)
			g_pMixerControls = new MixerControls();
	}

	public static void ShutdownMixerControls() {
		g_pMixerControls = null;
	}
}

public class VoiceChannel
{
	public void Init(int entity) {
		Entity = entity;
		Starved = false;
		Buffer.Flush();
		TimePad = Math.Clamp(Voice.voice_buffer_ms.GetFloat(), 1f, 5000f) / 1000f;
		LastSample = 0;
		LastFraction = 0.999;

		AutoGain.Reset(128, Voice.voice_maxgain.GetFloat(), Voice.voice_avggain.GetFloat(), Voice.voice_scale.GetFloat());
	}

	public int Entity;
	public readonly CircularBuffer Buffer = new(VOICE_RECEIVE_BUFFER_SIZE);
	public double LastFraction;
	public short LastSample;
	public bool Starved;
	public TimeUnit_t TimePad;
	public IVoiceCodec? VoiceCodec;
	public readonly AutoGain AutoGain = new();
	public VoiceChannel? Next;
	public bool Proximity;
	public int ViewEntityIndex;
	public int SoundGuid;

	public VoiceChannel() {
		Entity = -1;
		VoiceCodec = null;
		ViewEntityIndex = -1;
		SoundGuid = -1;
	}
}

public class VoiceWriterData
{
	public VoiceChannel? Channel;
	public int Count;
	public readonly List<byte> Buffer = [];
}

public class VoiceWriter
{
	readonly List<VoiceWriterData> Writer = [];

	public void Flush() {
		foreach (VoiceWriterData data in Writer) {
			if (data.Buffer.Count <= 0)
				continue;
			data.Buffer.Clear();
		}
	}

	public void Finish() {
		if (!g_SoundServices.IsConnected()) {
			Flush();
			return;
		}

		foreach (VoiceWriterData data in Writer) {
			if (data.Buffer.Count <= 0)
				continue;

			int index = Array.IndexOf(Voice.VoiceChannels, data.Channel);
			Assert(index >= 0 && index < Voice.VoiceChannels.Length);

			string path = $"{g_SoundServices.GetGameDir()}/voice";
			g_pFileSystem.CreateDirHierarchy(path);

			string fn = $"{path}/pl{index:D2}_slot{data.Count}-time{(int)g_SoundServices.GetClientTime()}.wav";

			VoiceWaveFile.WriteWaveFile(fn, CollectionsMarshal.AsSpan(data.Buffer), data.Buffer.Count, Voice.g_VoiceSampleFormat_BitsPerSample, Voice.g_VoiceSampleFormat_Channels, Voice.SamplesPerSec());

			Msg($"Writing file {fn}\n");

			++data.Count;
			data.Buffer.Clear();
		}
	}

	public void AddDecompressedData(VoiceChannel ch, ReadOnlySpan<byte> data) {
		if (!Voice.voice_writevoices.GetBool())
			return;

		VoiceWriterData? slot = null;
		foreach (VoiceWriterData search in Writer) {
			if (search.Channel == ch) {
				slot = search;
				break;
			}
		}
		if (slot == null) {
			slot = new() { Channel = ch };
			Writer.Add(slot);
		}

		slot.Buffer.AddRange(data);
	}
}

[EngineComponent]
public static class Voice
{
	[Dependency] public static Sound Sound { get; set; } = null!;
	public static readonly VoiceChannel[] VoiceChannels = new VoiceChannel[VOICE_NUM_CHANNELS];
	static Voice() {
		for (int i = 0; i < VoiceChannels.Length; i++)
			VoiceChannels[i] = new();
	}

	const int MAX_WAVEFILEDATA_LEN = 1024 * 1024;

	public static byte[]? UncompressedFileData = null;
	public static int UncompressedDataBytes = 0;
	public static string? UncompressedDataFilename = null;

	public static byte[]? DecompressedFileData = null;
	public static int DecompressedDataBytes = 0;
	public static string? DecompressedDataFilename = null;

	public static byte[]? MicInputFileData = null;
	public static int MicInputFileBytes = 0;
	public static int CurMicInputFileByte;
	public static double MicStartTime;

	public static readonly ConVar voice_avggain = new("voice_avggain", "0.5");
	public static readonly ConVar voice_maxgain = new("voice_maxgain", "10");
	public static readonly ConVar voice_scale = new("voice_scale", "1", FCvar.Archive);

	public static readonly ConVar voice_writevoices = new("voice_writevoices", "0", 0, "Saves each speaker's voice data into separate .wav files\n");
	// This should not be lower than the maximum difference between clients' frame durations (due to cmdrate/updaterate),
	// plus some jitter allowance.
	public static readonly ConVar voice_buffer_ms = new("voice_buffer_ms", "100", FCvar.InternalUse, "How many milliseconds of voice to buffer to avoid dropouts due to jitter and frame time differences.");
	public static readonly ConVar voice_enable = new("voice_enable", "1", FCvar.Archive);      // Globally enable or disable voice.
	public static readonly ConVar voice_fadeouttime = new("voice_fadeouttime", "0.1");  // It fades to no sound at the tail end of your voice data when you release the key.
	public static readonly ConVar voice_loopback = new("voice_loopback", "0", FCvar.UserInfo);

	// Have it force your mixer control settings so waveIn comes from the microphone.
	// CD rippers change your waveIn to come from the CD drive
	public static readonly ConVar voice_forcemicrecord = new("voice_forcemicrecord", "1", FCvar.Archive);

	// Timing info for each frame.
	static double CompressTime = 0;
	static double DecompressTime = 0;
	static double GainTime = 0;
	static double UpsampleTime = 0;

	struct VoiceTimer
	{
		public void Start() {
			if (voice_profile.GetInt() != 0)
				StartTime = Platform.Time;
		}

		public readonly void End(ref double @out) {
			if (voice_profile.GetInt() != 0)
				@out += Platform.Time - StartTime;
		}

		double StartTime;
	}
	public static readonly ConVar sv_use_steam_voice = new("sv_use_steam_voice", "1", FCvar.Replicated, "Enable/disable using Steam Voice instead of the old voice codec");
	static readonly VoiceWriter VoiceWriter = new();

	public static IVoiceRecord? VoiceRecord = null;
	public static IVoiceCodec? EncodeCodec = null;
	public static string? VoiceCodec = null;

	public static bool VoiceAtLeastPartiallyInitted = false;
	public static bool InTweakMode = false;
	public static int VoiceTweakSpeakingVolume = 0;
	public static bool VoiceRecording = false;
	public static bool VoiceRecordStopping = false;
	public static bool UsingSteamVoice = false;
	static bool SteamAPIContextInitted = false;
	public static int RequestedSampleRate = 0;

	internal static bool IsRecording() => VoiceRecording && !InTweakMode;

	public static bool Init(ReadOnlySpan<char> codecName, int sampleRate) {
#if !SWDS
		if (voice_enable.GetInt() == 0)
			return false;

		if (codecName.IsStringEmpty)
			return false;

		bool isSpeex = stricmp(codecName, "vaudio_speex") == 0;
		bool isCelt = stricmp(codecName, "vaudio_celt") == 0;
		bool isSteam = stricmp(codecName, "steam") == 0;

		if (!(isSpeex || isCelt || isSteam)) {
			Msg($"Voice_Init Failed: invalid voice codec {codecName}.\n");
			return false;
		}

		Deinit();

		VoiceAtLeastPartiallyInitted = true;
		VoiceCodec = new(codecName.SliceNullTerminatedString());
		RequestedSampleRate = sampleRate;

		UsingSteamVoice = isSteam;

		if (!SteamAPIContextInitted)
			SteamAPIContextInitted = SteamAPI.Init();

		if (UsingSteamVoice) {
			if (!SteamAPIContextInitted) {
				Msg("Voice_Init: Requested Steam voice, but cannot access API.  Voice will not function\n");
				return false;
			}
		}

		// For steam, nSampleRate 0 means "use optimal steam sample rate".
		if (isSteam && sampleRate == 0) {
			// dimhotepus: NO_STEAM
			Msg($"Voice_Init: Using Steam voice optimal sample rate {SteamUser.GetVoiceOptimalSampleRate()}\n");

			// Steam's sample rate may change and not be supported by our rather unflexible sound engine. However, steam
			// will resample as necessary in DecompressVoice, so we can pretend we're outputting at native rates.
			//
			// Behind the scenes, we'll request steam give us the encoded stream at its "optimal" rate, then we'll try to
			// decompress the output at this rate, making it transparent to us that the encoded stream is not at our output
			// rate.
			SetSampleRate(44100); // SOUND_DMA_SPEED
		}
		else
			SetSampleRate(sampleRate);

		if (!VoiceSE.Init())
			return false;

		// Get the voice input device.
		VoiceRecord = g_AudioSystem.CreateVoiceRecord(SamplesPerSec());
		if (VoiceRecord == null)
			Msg("Unable to initialize sound capture. You won't be able to speak to other players.\n");

		// Init codec DLL for non-steam
		if (!isSteam) {
			// CELT's qualities are 0-3, we historically just passed 4 to the other two even though they don't really map to the
			// same thing.
			//
			// Changing the quality level we use here will require either extending SVC_VoiceInit to pass down which quality is
			// in use or using a different codec name (vaudio_celtHD!) for backwards compatibility
			// dimhotepus: Breaking change. Always use good quality.
			VoiceCodecQuality quality = VoiceCodecQuality.Good;

			// Get the codec.
			Func<IVoiceCodec>? createCodecFn = VoiceCodecFactory.GetFactory(codecName);

			if (createCodecFn == null || (EncodeCodec = createCodecFn()) == null || !EncodeCodec.Init(quality)) {
				Msg($"Unable to init voice codec '{codecName}' with quaiity level {(int)quality}. Voice disabled.\n");
				Deinit();
				return false;
			}

			for (int i = 0; i < VOICE_NUM_CHANNELS; i++) {
				VoiceChannel channel = VoiceChannels[i];

				if ((channel.VoiceCodec = createCodecFn()) == null || !channel.VoiceCodec.Init(quality)) {
					Deinit();
					return false;
				}
			}
		}

		// XXX(JohnS): These don't do much in Steam codec mode, but code below uses their presence to mean 'voice fully
		//             initialized' and other things assume they will succeed.
		MixerControls.InitMixerControls();

		// Steam mode uses steam for raw input so this isn't meaningful and could have side-effects
		if (voice_forcemicrecord.GetInt() != 0 && !isSteam) {
			MixerControls.g_pMixerControls?.SelectMicrophoneForWaveInput();
		}
#endif
		return true;
	}

	internal static void EndChannel(int idx) {
		Assert(idx >= 0 && idx < VOICE_NUM_CHANNELS);

		VoiceChannel channel = VoiceChannels[idx];

		if (channel.Entity != -1) {
			int ent = channel.Entity;
			channel.Entity = -1;

			if (channel.Proximity == true)
				VoiceSE.EndChannel(idx, ent);
			else
				VoiceSE.EndChannel(idx, channel.ViewEntityIndex);

			g_SoundServices.OnChangeVoiceStatus(ent, false);
			VoiceSE.CloseMouth(ent);

			channel.ViewEntityIndex = -1;
			channel.SoundGuid = -1;

			// If the tweak mode channel is ending
			if (idx == 0 && InTweakMode)
				Tweak_EndVoiceTweakMode();
		}
	}

	private static void Tweak_EndVoiceTweakMode() {
		if (!InTweakMode) {
			AssertMsg(false, "Voice.Tweak_EndVoiceTweakMode called when not in tweak mode.");
			return;
		}

		InTweakMode = false;
		RecordStop();
	}

	internal static void EndAllChannels() {
		for (int i = 0; i < VOICE_NUM_CHANNELS; i++)
			EndChannel(i);

	}

	internal static void Deinit() {
		if (!VoiceAtLeastPartiallyInitted)
			return;

		if (EngineTool.SuppressDeInit())
			return;

		EndAllChannels();

		RecordStop();

		for (int i = 0; i < VOICE_NUM_CHANNELS; i++) {
			VoiceChannel channel = VoiceChannels[i];

			if (channel.VoiceCodec != null) {
				channel.VoiceCodec.Release();
				channel.VoiceCodec = null;
			}
		}

		if (EncodeCodec != null) {
			EncodeCodec.Release();
			EncodeCodec = null;
		}

		if (VoiceRecord != null) {
			VoiceRecord.Release();
			VoiceRecord = null;
		}

		VoiceSE.Term();

		VoiceAtLeastPartiallyInitted = false;
		VoiceCodec = null;
		RequestedSampleRate = -1;
		UsingSteamVoice = false;
	}

	static bool VoiceRecord_Start() {
		if (UsingSteamVoice) {
			if (SteamAPIContextInitted) {
				SteamUser.StartVoiceRecording();
				return true;
			}
		}
		else if (VoiceRecord != null)
			return VoiceRecord.RecordStart();

		return false;
	}

	internal static bool Record_Start(ReadOnlySpan<char> uncompressedFile, ReadOnlySpan<char> decompressedFile, ReadOnlySpan<char> micInputFile) {
		if (EncodeCodec == null && !UsingSteamVoice)
			return false;

		VoiceWriter.Flush();

		RecordStop();

		if (!UsingSteamVoice)
			EncodeCodec!.ResetState();

		if (!micInputFile.IsEmpty) {
			VoiceWaveFile.ReadWaveFile(micInputFile, out MicInputFileData, out MicInputFileBytes, out _, out _, out _);
			CurMicInputFileByte = 0;
			MicStartTime = Platform.Time;
		}

		if (!uncompressedFile.IsEmpty) {
			UncompressedFileData = new byte[MAX_WAVEFILEDATA_LEN];
			UncompressedDataBytes = 0;
			UncompressedDataFilename = new(uncompressedFile);
		}

		if (!decompressedFile.IsEmpty) {
			DecompressedFileData = new byte[MAX_WAVEFILEDATA_LEN];
			DecompressedDataBytes = 0;
			DecompressedDataFilename = new(decompressedFile);
		}

		VoiceRecording = false;
		if (VoiceRecord != null) {
			VoiceRecording = VoiceRecord_Start();
			if (VoiceRecording) {
				if (SteamAPIContextInitted) {
					// Tell Friends' Voice chat that the local user has started speaking
					SteamFriends.SetInGameVoiceSpeaking(SteamUser.GetSteamID(), true);
				}

				g_SoundServices.OnChangeVoiceStatus(-1, true);      // Tell the client DLL.
			}
		}

		return VoiceRecording;
	}
	internal static void UserDesiresStop() {
		if (VoiceRecordStopping)
			return;

		VoiceRecordStopping = true;
		g_SoundServices.OnChangeVoiceStatus(-1, false);       // Tell the client DLL.

		// If we're using Steam voice, we'll keep recording until Steam tells us we
		// received all the data.
		if (UsingSteamVoice)
			SteamUser.StopVoiceRecording();
		else
			Record_Stop();
	}

	internal static void Record_Stop() {
		if (UsingSteamVoice) {
			if (SteamAPIContextInitted)
				SteamUser.StopVoiceRecording();
		}
		else if (VoiceRecord != null)
			VoiceRecord.RecordStop();
	}

	internal static bool RecordStop() {
		// Write the files out for debugging.
		if (MicInputFileData != null)
			MicInputFileData = null;

		if (UncompressedFileData != null) {
			VoiceWaveFile.WriteWaveFile(UncompressedDataFilename, UncompressedFileData, UncompressedDataBytes, g_VoiceSampleFormat_BitsPerSample, g_VoiceSampleFormat_Channels, SamplesPerSec());
			UncompressedFileData = null;
		}

		if (DecompressedFileData != null) {
			VoiceWaveFile.WriteWaveFile(DecompressedDataFilename, DecompressedFileData, DecompressedDataBytes, g_VoiceSampleFormat_BitsPerSample, g_VoiceSampleFormat_Channels, SamplesPerSec());
			DecompressedFileData = null;
		}

		VoiceWriter.Finish();

		Record_Stop();

		if (VoiceRecording) {
			if (SteamAPIContextInitted) {
				// Tell Friends' Voice chat that the local user has stopped speaking
				SteamFriends.SetInGameVoiceSpeaking(SteamUser.GetSteamID(), false);
			}
		}

		VoiceRecording = false;
		VoiceRecordStopping = false;
		return (true);
	}


	// this sucks
	[DllImport("steam_api64", CallingConvention = CallingConvention.Cdecl, EntryPoint = "SteamAPI_ISteamUser_GetVoice")]
	static unsafe extern EVoiceResult ISteamUser_GetVoice(IntPtr instancePtr, [MarshalAs(UnmanagedType.I1)] bool bWantCompressed, byte* pDestBuffer, uint cbDestBufferSize, out uint nBytesWritten, [MarshalAs(UnmanagedType.I1)] bool bWantUncompressed_Deprecated, byte* pUncompressedDestBuffer_Deprecated, uint cbUncompressedDestBufferSize_Deprecated, uint* nUncompressBytesWritten_Deprecated, uint nUncompressedVoiceDesiredSampleRate_Deprecated);

	[DllImport("steam_api64", CallingConvention = CallingConvention.Cdecl, EntryPoint = "SteamAPI_ISteamUser_DecompressVoice")]
	static unsafe extern EVoiceResult ISteamUser_DecompressVoice(IntPtr instancePtr, byte* pCompressed, uint cbCompressed, byte* pDestBuffer, uint cbDestBufferSize, out uint nBytesWritten, uint nDesiredSampleRate);


	internal unsafe static int GetCompressedData(Span<byte> dest, bool final) {
		fixed (byte* destPtr = dest)
			if (UsingSteamVoice || VoiceRecordStopping) {
				uint compressedWritten = 0;

				// dimhotepus: NO_STEAM
				uint uncompressedWritten = 0;
				uint compressed = 0;
				uint uncompressed = 0;

				// We're going to always request steam give us the encoded stream at the optimal rate, unless our final output
				// rate is lower than it.  We'll pass our output rate when we actually extract the data, which Steam will
				// happily upsample from its optimal rate for us.
				int nEncodeRate = Math.Min((int)SteamUser.GetVoiceOptimalSampleRate(), SamplesPerSec());
				EVoiceResult result = SteamUser.GetAvailableVoice(out uncompressed);
				if (result == EVoiceResult.k_EVoiceResultOK) {
					fixed (byte* uncompressedPtr = UncompressedFileData) {
						result = ISteamUser_GetVoice(GetSteamUser(), true, destPtr, (uint)dest.Length, out compressedWritten,
													 UncompressedFileData != null, uncompressedPtr == null ? null : uncompressedPtr + UncompressedDataBytes,
													 (uint)(MAX_WAVEFILEDATA_LEN - UncompressedDataBytes),
													 &uncompressedWritten, (uint)nEncodeRate);
					}

					if (UncompressedFileData != null)
						UncompressedDataBytes += (int)uncompressedWritten;

					g_SoundServices.OnChangeVoiceStatus(-3, true);
				}
				else {
					if (result == EVoiceResult.k_EVoiceResultNotRecording && Voice.VoiceRecording)
						Voice.RecordStop();

					g_SoundServices.OnChangeVoiceStatus(-3, false);
				}

				return (int)compressedWritten;
			}

		IVoiceCodec? pCodec = Voice.EncodeCodec;
		if (Voice.VoiceRecord != null && pCodec != null) {
			Span<short> tempData = stackalloc short[8192];
			int samplesWanted = Math.Min(dest.Length / BYTES_PER_SAMPLE, tempData.Length);
			int gotten = Voice.VoiceRecord.GetRecordedData(tempData[..samplesWanted]);

			// If they want to get the data from a file instead of the mic, use that.
			if (MicInputFileData != null) {
				double curtime = Platform.Time;
				int nShouldGet = (int)((curtime - MicStartTime) * SamplesPerSec());
				gotten = Math.Min(tempData.Length,
					Math.Min(nShouldGet, (MicInputFileBytes - CurMicInputFileByte) / BYTES_PER_SAMPLE));
				MemoryMarshal.Cast<byte, short>(MicInputFileData.AsSpan(CurMicInputFileByte, gotten * BYTES_PER_SAMPLE)).CopyTo(tempData);
				CurMicInputFileByte += gotten * BYTES_PER_SAMPLE;
				MicStartTime = curtime;
			}

			int nCompressedBytes = pCodec.Compress(MemoryMarshal.AsBytes(tempData[..gotten]), dest, final);

			// Write to our file buffers..
			if (UncompressedFileData != null) {
				int nToWrite = Math.Min(gotten * BYTES_PER_SAMPLE, MAX_WAVEFILEDATA_LEN - UncompressedDataBytes);
				MemoryMarshal.AsBytes(tempData[..gotten])[..nToWrite].CopyTo(UncompressedFileData.AsSpan(UncompressedDataBytes));
				UncompressedDataBytes += nToWrite;
			}
			return nCompressedBytes;
		}
		else {
			return 0;
		}
	}

	// TODO: struct
	// it's a Windows API struct so I don't know if I want to copy it or not yet here
	public static int g_VoiceSampleFormat_FormatTag = 1;
	public static int g_VoiceSampleFormat_Channels = 1;
	public static int g_VoiceSampleFormat_SamplesPerSec = VOICE_OUTPUT_SAMPLE_RATE_LOW;
	public static int g_VoiceSampleFormat_AvgBytesPerSec = VOICE_OUTPUT_SAMPLE_RATE_LOW * 2;
	public static int g_VoiceSampleFormat_BlockAlign = 2;
	public static int g_VoiceSampleFormat_BitsPerSample = 16;

	public static bool SetSampleRate(int rate) {
		if (g_VoiceSampleFormat_SamplesPerSec != rate || g_VoiceSampleFormat_AvgBytesPerSec != rate * 2) {
			g_VoiceSampleFormat_SamplesPerSec = rate;
			g_VoiceSampleFormat_AvgBytesPerSec = rate * 2;
			return true;
		}

		return false;
	}

	internal static int SamplesPerSec() {
		int rate = g_VoiceSampleFormat_SamplesPerSec;
		EngineTool.OverrideSampleRate(ref rate);
		return rate;
	}

	internal static int AvgBytesPerSec() {
		int rate = g_VoiceSampleFormat_SamplesPerSec;
		EngineTool.OverrideSampleRate(ref rate);
		return (rate * g_VoiceSampleFormat_BitsPerSample) >> 3;
	}

	internal static ReadOnlySpan<char> ConfiguredCodec() => VoiceCodec;
	internal static int ConfiguredSampleRate() => RequestedSampleRate;

	public const string VOICE_FALLBACK_CODEC = "vaudio_celt";

	public static int GetDefaultSampleRate(ReadOnlySpan<char> codec) {
		switch (codec) {
			case "vaudio_speex": return VOICE_OUTPUT_SAMPLE_RATE_LOW;
			case "steam": return 0;
			default: return VOICE_OUTPUT_SAMPLE_RATE_HIGH;
		}
	}
	public static bool InitWithDefault(ReadOnlySpan<char> codecName) {
		if (codecName.IsStringEmpty)
			return false;

		int rate = GetDefaultSampleRate(codecName);
		if (rate < 0) {
			Msg($"Voice_InitWithDefault: Unable to determine defaults for codec \"{codecName}\"\n");
			return false;
		}

		return Init(codecName, rate);
	}
	public static void ForceInit() {
		if (MixerControls.g_pMixerControls != null || !voice_enable.GetBool()) {
			// Nothing to do
			return;
		}

		// Lacking a better default, just peak at what the server's sv_voicecodec is set to
		ConVarRef sv_voicecodec = new("sv_voicecodec");
		ReadOnlySpan<char> voiceCodec = sv_use_steam_voice.GetBool() ? "steam" : sv_voicecodec.GetString();
		if (!InitWithDefault(voiceCodec)) {
			// Try ultimate fallback
			InitWithDefault(VOICE_FALLBACK_CODEC);
		}
	}

	//------------------------------------------------------------------------------
	// IVoiceTweak implementation.
	//------------------------------------------------------------------------------

	public static int VoiceTweak_StartVoiceTweakMode() {
		// If we're already in voice tweak mode, return an error.
		if (InTweakMode) {
			AssertMsg(false, "VoiceTweak_StartVoiceTweakMode called while already in tweak mode.");
			return 0;
		}

		if (MixerControls.g_pMixerControls == null && voice_enable.GetBool())
			ForceInit();

		if (MixerControls.g_pMixerControls == null)
			return 0;

		EndAllChannels();
		Record_Start(null, null, null);
		AssignChannel(TWEAKMODE_ENTITYINDEX, false);
		InTweakMode = true;
		MixerControls.InitMixerControls();

		return 1;
	}

	public static void VoiceTweak_SetControlFloat(VoiceTweakControl control, float value) {
		if (MixerControls.g_pMixerControls == null)
			return;

		if (control == VoiceTweakControl.MicrophoneVolume)
			MixerControls.g_pMixerControls.SetValue_Float(MixerControl.MicVolume, value);
		else if (control == VoiceTweakControl.MicBoost)
			MixerControls.g_pMixerControls.SetValue_Float(MixerControl.MicBoost, value);
		else if (control == VoiceTweakControl.OtherSpeakerScale)
			voice_scale.SetValue(value);
	}

	public static float VoiceTweak_GetControlFloat(VoiceTweakControl control) {
		ForceInit();

		if (MixerControls.g_pMixerControls == null)
			return 0;

		if (control == VoiceTweakControl.MicrophoneVolume) {
			float value = 1;
			MixerControls.g_pMixerControls.GetValue_Float(MixerControl.MicVolume, ref value);
			return value;
		}
		else if (control == VoiceTweakControl.OtherSpeakerScale)
			return voice_scale.GetFloat();
		else if (control == VoiceTweakControl.SpeakingVolume)
			return VoiceTweakSpeakingVolume * 1.0f / 32768;
		else if (control == VoiceTweakControl.MicBoost) {
			float value = 1;
			MixerControls.g_pMixerControls.GetValue_Float(MixerControl.MicBoost, ref value);
			return value;
		}
		else
			return 1;
	}

	public static IVoiceTweak g_VoiceTweakAPI = new() {
		StartVoiceTweakMode = VoiceTweak_StartVoiceTweakMode,
		EndVoiceTweakMode = VoiceTweak_EndVoiceTweakMode,
		SetControlFloat = VoiceTweak_SetControlFloat,
		GetControlFloat = VoiceTweak_GetControlFloat,
		IsStillTweaking = VoiceTweak_IsStillTweaking,
	};

	internal static bool Enabled() => voice_enable.GetBool();

	internal static int GetChannel(int entity) {
		for (int i = 0; i < VOICE_NUM_CHANNELS; i++)
			if (VoiceChannels[i].Entity == entity)
				return i;

		return VOICE_CHANNEL_ERROR;
	}

	internal static int AssignChannel(int entity, bool proximity) {
		if (InTweakMode)
			return VOICE_CHANNEL_IN_TWEAK_MODE;

		int free = -1;
		for (int i = 0; i < VOICE_NUM_CHANNELS; i++) {
			VoiceChannel channel = VoiceChannels[i];

			if (channel.Entity == entity)
				return i;
			else if (channel.Entity == -1 && (channel.VoiceCodec != null || UsingSteamVoice)) {
				channel.VoiceCodec?.ResetState();

				free = i;
				break;
			}
		}

		if (free == -1)
			return VOICE_CHANNEL_ERROR;

		VoiceChannel newChannel = VoiceChannels[free];
		newChannel.Init(entity);
		newChannel.Proximity = proximity;
		VoiceSE.StartOverdrive();

		return free;
	}

	public static readonly ConVar voice_profile = new("voice_profile", "0");
	public static readonly ConVar voice_showchannels = new("voice_showchannels", "0");
	public static readonly ConVar voice_showincoming = new("voice_showincoming", "0");

	internal static int AddIncomingData(int nChannel, Span<byte> data, int count, int sequenceNumber) {
		VoiceChannel? channel;

		if (InTweakMode) {
			if (nChannel == TWEAKMODE_CHANNELINDEX)
				nChannel = 0;
			else
				return 0;
		}

		if ((channel = GetVoiceChannel(nChannel)) == null || (!UsingSteamVoice && channel.VoiceCodec == null))
			return 0;


		channel.Starved = false;

		Span<byte> decompressed = stackalloc byte[22528];


		int nDecompressed = 0;
		if (UsingSteamVoice) {
			// dimhotepus: NO_STEAM
			uint nBytesWritten = 0;
			unsafe {
				fixed (byte* pData = data)
				fixed (byte* pDecompressed = decompressed) {
					EVoiceResult result = ISteamUser_DecompressVoice(GetSteamUser(), pData, (uint)count, pDecompressed, (uint)decompressed.Length, out nBytesWritten, (uint)Voice.SamplesPerSec());
					if (result == EVoiceResult.k_EVoiceResultOK)
						nDecompressed = (int)(nBytesWritten / BYTES_PER_SAMPLE);
				}
			}
		}
		else
			nDecompressed = channel.VoiceCodec.Decompress(data[..count], decompressed);

		if (InTweakMode) {
			Span<short> shortData = reinterpret<byte, short>(decompressed);
			VoiceTweakSpeakingVolume = 0;

			for (int i = 0; i < nDecompressed; ++i)
				VoiceTweakSpeakingVolume = unchecked((short)Math.Max((long)Math.Abs((int)shortData[i]), (long)VoiceTweakSpeakingVolume));
			VoiceTweakSpeakingVolume &= 0xFE00;
		}

		channel.AutoGain.ProcessSamples(reinterpret<byte, short>(decompressed), nDecompressed);

		// Upsample into the dest buffer. We could do this in a mixer but it complicates the mixer.
		channel.LastFraction = UpsampleIntoBuffer(reinterpret<byte, short>(decompressed),
													   nDecompressed,
													   channel.Buffer,
													   channel.LastFraction,
													   (double)Voice.SamplesPerSec() / g_VoiceSampleFormat_SamplesPerSec);
		channel.LastSample = decompressed[nDecompressed];

		// Write to our file buffer..
		if (DecompressedFileData != null) {
			int nToWrite = Math.Min(nDecompressed * 2, MAX_WAVEFILEDATA_LEN - DecompressedDataBytes);
			decompressed[..nToWrite].CopyTo(DecompressedFileData.AsSpan(DecompressedDataBytes));
			DecompressedDataBytes += nToWrite;
		}

		VoiceWriter.AddDecompressedData(channel, decompressed[..(nDecompressed * 2)]);

		if (voice_showincoming.GetInt() != 0)
			Msg($"Voice - {nDecompressed} incoming samples added to channel {nChannel}\n");

		return nChannel;
	}
	public static double UpsampleIntoBuffer(ReadOnlySpan<short> src, int srcSamples, CircularBuffer buffer, double startFraction, double rate) {
		double maxFraction = srcSamples - 1;

		while (true) {
			if (startFraction >= maxFraction)
				break;

			int sample = (int)startFraction;
			double frac = startFraction - Math.Floor(startFraction);

			double val1 = src[sample];
			double val2 = src[sample + 1];
			short newSample = (short)(val1 + (val2 - val1) * frac);
			buffer.Write(MemoryMarshal.AsBytes(new ReadOnlySpan<short>(in newSample)), sizeof(short));

			startFraction += rate;
		}

		return startFraction - Math.Floor(startFraction);
	}
	private static VoiceChannel? GetVoiceChannel(int channel, bool assert = true) {
		if (channel < 0 || channel >= VOICE_NUM_CHANNELS) {
			if (assert)
				Assert(false);

			return null;
		}
		else
			return VoiceChannels[channel];
	}

	public static bool bLocalPlayerTalkingAck;
	public static TimeUnit_t LocalPlayerTalkingTimeout;
	internal static void LocalPlayerTalkingAck() {
		if (!bLocalPlayerTalkingAck)
			g_SoundServices.OnChangeVoiceStatus(-2, true);
		bLocalPlayerTalkingAck = true;
		LocalPlayerTalkingTimeout = 0;
	}

	const TimeUnit_t LOCALPLAYERTALKING_TIMEOUT = (TimeUnit_t)0.2;

	static int FadeSamples;
	static TimeUnit_t FadeMul;

	internal static void Idle(TimeUnit_t frametime) {
		if (voice_enable.GetInt() == 0) {
			Deinit();
			return;
		}

		if (bLocalPlayerTalkingAck) {
			LocalPlayerTalkingTimeout += frametime;
			if (LocalPlayerTalkingTimeout > LOCALPLAYERTALKING_TIMEOUT) {
				bLocalPlayerTalkingAck = false;

				// Tell the client DLL.
				g_SoundServices.OnChangeVoiceStatus(-2, false);
			}
		}

		// Precalculate these to speedup the voice fadeout.
		FadeSamples = Math.Max((int)(voice_fadeouttime.GetFloat() * g_VoiceSampleFormat_SamplesPerSec), 2);
		FadeMul = 1.0f / (FadeSamples - 1);

		VoiceRecord?.Idle();

		// If we're in voice tweak mode, feed our own data back to us.
		UpdateVoiceTweakMode();

		// Age the channels.
		int nActive = 0;
		for (int i = 0; i < VOICE_NUM_CHANNELS; i++) {
			VoiceChannel channel = VoiceChannels[i];

			if (channel.Entity != -1) {
				if (channel.Starved) {
					Voice.EndChannel(i);
					channel.SoundGuid = -1;
				}
				else {
					TimeUnit_t oldpad = channel.TimePad;
					channel.TimePad -= frametime;
					if (oldpad > 0 && channel.TimePad <= 0) {
						// Start its audio.
						channel.ViewEntityIndex = g_SoundServices.GetViewEntity();
						channel.SoundGuid = VoiceSE.StartChannel(i, channel.Entity, channel.Proximity, channel.ViewEntityIndex);
						g_SoundServices.OnChangeVoiceStatus(channel.Entity, true);

						VoiceSE.InitMouth(channel.Entity);
					}

					++nActive;
				}
			}
		}

		if (nActive == 0)
			VoiceSE.EndOverdrive();

		VoiceSE.Idle(frametime);

		// voice_showchannels.
		if (voice_showchannels.GetInt() >= 1) {
			for (int i = 0; i < VOICE_NUM_CHANNELS; i++) {
				VoiceChannel channel = VoiceChannels[i];

				if (channel.Entity == -1)
					continue;

				Msg($"Voice - chan {i}, ent {channel.Entity}, bufsize: {channel.Buffer.GetReadAvailable()}\n");
			}
		}

		// Show profiling data?
		if (voice_profile.GetInt() != 0) {
			Msg($"Voice - compress: {CompressTime * 1000000.0,7:F2}u, decompress: {DecompressTime * 1000000.0,7:F2}u, gain: {GainTime * 1000000.0,7:F2}u, upsample: {UpsampleTime * 1000000.0,7:F2}u, total: {(CompressTime + DecompressTime + GainTime + UpsampleTime) * 1000000.0,7:F2}u\n");

			CompressTime = DecompressTime = GainTime = UpsampleTime = 0;
		}
	}

	internal static bool GetLoopback() => voice_loopback.GetInt() != 0;

	static void ApplyFadeToSamples(Span<short> pSamples, int nSamples, int fadeOffset, float fadeMul) {
		for (int i = 0; i < nSamples; i++) {
			float percent = (i + fadeOffset) * fadeMul;
			pSamples[i] = (short)(pSamples[i] * (1 - percent));
		}
	}

	internal static int GetOutputData(
		int iChannel,           //! The voice channel it wants samples from.
		Span<byte> copyBufBytes,    //! The buffer to copy the samples into.
		int copyBufSize,        //! Maximum size of copyBuf.
		int samplePosition, //! Which sample to start at.
		int sampleCount     //! How many samples to get.
	) {
		VoiceChannel pChannel = VoiceChannels[iChannel];
		Span<short> pCopyBuf = MemoryMarshal.Cast<byte, short>(copyBufBytes);


		int maxOutSamples = copyBufSize / BYTES_PER_SAMPLE;

		// Find out how much we want and get it from the received data channel.	
		CircularBuffer pBuffer = pChannel.Buffer;
		int nBytesToRead = pBuffer.GetReadAvailable();
		nBytesToRead = Math.Min(Math.Min(nBytesToRead, (int)maxOutSamples), sampleCount * BYTES_PER_SAMPLE);
		int nSamplesGotten = pBuffer.Read(copyBufBytes, nBytesToRead) / BYTES_PER_SAMPLE;

		// Are we at the end of the buffer's data? If so, fade data to silence so it doesn't clip.
		int readSamplesAvail = pBuffer.GetReadAvailable() / BYTES_PER_SAMPLE;
		if (readSamplesAvail < FadeSamples) {
			int bufferFadeOffset = Math.Max((readSamplesAvail + nSamplesGotten) - FadeSamples, 0);
			int globalFadeOffset = Math.Max(FadeSamples - (readSamplesAvail + nSamplesGotten), 0);

			ApplyFadeToSamples(
				pCopyBuf[bufferFadeOffset..],
				nSamplesGotten - bufferFadeOffset,
				globalFadeOffset,
				(float)FadeMul);
		}

		// If there weren't enough samples in the received data channel, 
		//   pad it with a copy of the most recent data, and if there 
		//   isn't any, then use zeros.
		if (nSamplesGotten < sampleCount) {
			int wantedSampleCount = Math.Min(sampleCount, maxOutSamples);
			int nAdditionalNeeded = (wantedSampleCount - nSamplesGotten);
			if (nSamplesGotten > 0) {
				int nSamplesToDuplicate = Math.Min(nSamplesGotten, nAdditionalNeeded);
				pCopyBuf.Slice(nSamplesGotten - nSamplesToDuplicate, nSamplesToDuplicate).CopyTo(pCopyBuf[nSamplesGotten..]);

				//Msg( "duplicating %d samples\n", nSamplesToDuplicate );

				nAdditionalNeeded -= nSamplesToDuplicate;
				if (nAdditionalNeeded > 0) {
					pCopyBuf.Slice(nSamplesGotten + nSamplesToDuplicate, nAdditionalNeeded).Clear();

					// Msg( "zeroing %d samples\n", nAdditionalNeeded );

					Assert((nAdditionalNeeded + nSamplesGotten + nSamplesToDuplicate) == wantedSampleCount);
				}
			}
			else
				pCopyBuf.Slice(nSamplesGotten, nAdditionalNeeded).Clear();
			nSamplesGotten = wantedSampleCount;
		}

		// If the buffer is out of data, mark this channel to go away.
		if (pBuffer.GetReadAvailable() == 0)
			pChannel.Starved = true;

		if (voice_showchannels.GetInt() >= 2)
			Msg($"Voice - mixed {nSamplesGotten} samples from channel {iChannel}\n");

		VoiceSE.MoveMouth(pChannel.Entity, pCopyBuf, nSamplesGotten);
		return nSamplesGotten;
	}


	internal static void OnAudioSourceShutdown(int iChannel) {
		EndChannel(iChannel);
	}

	public static bool VoiceTweak_IsStillTweaking() => InTweakMode;
	public static void VoiceTweak_EndVoiceTweakMode() => Tweak_EndVoiceTweakMode();

	// Only does anything for voice tweak channel so if view entity changes it doesn't fade out to zero volume
	internal static void Spatialize(int guid, ref int soundsource) {
		if (!InTweakMode)
			return;

		// Place the tweak mode sound back at the view entity
		VoiceChannel? pVoiceChannel = GetVoiceChannel(0);
		Assert(pVoiceChannel);
		if (pVoiceChannel == null)
			return;

		if (pVoiceChannel.SoundGuid != guid)
			return;

		// No change
		if (g_SoundServices.GetViewEntity() == pVoiceChannel.ViewEntityIndex)
			return;

		DevMsg(1, $"Voice_Spatialize changing voice tweak entity from {pVoiceChannel.ViewEntityIndex} to {g_SoundServices.GetViewEntity()}\n");

		pVoiceChannel.ViewEntityIndex = g_SoundServices.GetViewEntity();
		soundsource = pVoiceChannel.ViewEntityIndex;
	}

	private static void UpdateVoiceTweakMode() {
		if (!InTweakMode || VoiceRecord == null)
			return;

		VoiceChannel tweakChannel = GetVoiceChannel(0)!;

		if (tweakChannel.SoundGuid != -1 && !Sound.IsSoundStillPlaying(tweakChannel.SoundGuid)) {
			Tweak_EndVoiceTweakMode();
			return;
		}

		Span<byte> uchVoiceData = stackalloc byte[4096];
		bool bFinal = false;
		int nDataLength = GetCompressedData(uchVoiceData, bFinal);

		AddIncomingData(TWEAKMODE_CHANNELINDEX, uchVoiceData, nDataLength, 0);
	}
}
