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
	public const int VOICE_NUM_CHANNELS = 5; // review
}

public class AutoGain
{

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

		// todo: AutoGain.Reset(128, voice_maxgain.GetFloat(), voice_avggain.GetFloat(), voice_scale.GetFloat());
	}

	public int Entity;
	public readonly SizedCircularBuffer<byte> Buffer = new(VOICE_RECEIVE_BUFFER_SIZE);
	public double LastFraction;
	public short LastSample;
	public bool Starved;
	public float TimePad;
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

public class VoiceWriter
{
	public void Flush() {

	}
	public void Finish() {

	}

	public void AddDecompressedData(VoiceChannel ch, ReadOnlySpan<byte> data) {

	}
}

[EngineComponent]
public static class Voice
{
	public static readonly VoiceChannel[] VoiceChannels = new VoiceChannel[VOICE_NUM_CHANNELS];
	static Voice() {
		VoiceChannels.Initialize();
	}

	public static byte[]? UncompressedFileData = null;
	public static string? UncompressedDataFilename = null;

	public static byte[]? DecompressedFileData = null;
	public static string? DecompressedDataFilename = null;

	public static byte[]? MicInputFileData = null;
	public static int CurMicInputFileByte;
	public static double MicStartTime;

	public static readonly ConVar voice_writevoices = new("voice_writevoices", "0", 0, "Saves each speaker's voice data into separate .wav files");
	public static readonly ConVar voice_buffer_ms = new("voice_buffer_ms", "100", 0, "How many milliseconds of voice to buffer to avoid dropouts due to jitter and frame time differences.");
	public static readonly ConVar voice_enable = new("voice_enable", "1", FCvar.Archive);      // Globally enable or disable voice.
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
	public static int RequestedSampleRate = 0;

	internal static bool IsRecording() => VoiceRecording && !InTweakMode;

	public static bool Init(ReadOnlySpan<char> codecName, int sampleRate) {
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

		SteamAPI.Init();

		if (UsingSteamVoice) {
			// TODO: Check if Steam API available...
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
			throw new Exception("Non-Steam voice codecs are not yet implemented");
		}

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

			if (channel.VoiceCodec != null)
				channel.VoiceCodec = null;
		}

		if (EncodeCodec != null)
			EncodeCodec = null;

		if (VoiceRecord != null)
			VoiceRecord = null;

		VoiceSE.Term();

		VoiceAtLeastPartiallyInitted = false;
		VoiceCodec = null;
		RequestedSampleRate = -1;
		UsingSteamVoice = false;
	}

	internal static bool Record_Start(ReadOnlySpan<char> uncompressedFile, ReadOnlySpan<char> decompressedFile, ReadOnlySpan<char> micInputFile) {
		if (UsingSteamVoice) {
			SteamUser.StartVoiceRecording();
			return true;
		}
		else if (VoiceRecord != null)
			return VoiceRecord.RecordStart();

		return false;
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
		if (UsingSteamVoice)
			SteamUser.StopVoiceRecording();
		else if (VoiceRecord != null)
			VoiceRecord.RecordStop();
	}

	internal static bool RecordStop() {
		if (MicInputFileData != null)
			MicInputFileData = null;

		if (UncompressedFileData != null)
			UncompressedFileData = null;

		if (DecompressedFileData != null)
			DecompressedFileData = null;

		VoiceWriter.Finish();

		Record_Stop();

		if (VoiceRecording)
			SteamFriends.SetInGameVoiceSpeaking(SteamUser.GetSteamID(), false);

		VoiceRecording = false;
		VoiceRecordStopping = false;
		return (true);
	}


	// this sucks
	[DllImport("steam_api64", CallingConvention = CallingConvention.Cdecl, EntryPoint = "SteamAPI_ISteamUser_GetVoice")]
	static unsafe extern EVoiceResult ISteamUser_GetVoice(IntPtr instancePtr, [MarshalAs(UnmanagedType.I1)] bool bWantCompressed, byte* pDestBuffer, uint cbDestBufferSize, out uint nBytesWritten, [MarshalAs(UnmanagedType.I1)] bool bWantUncompressed_Deprecated, IntPtr pUncompressedDestBuffer_Deprecated, uint cbUncompressedDestBufferSize_Deprecated, IntPtr nUncompressBytesWritten_Deprecated, uint nUncompressedVoiceDesiredSampleRate_Deprecated);

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
					result = ISteamUser_GetVoice(GetSteamUser(), true, destPtr, (uint)dest.Length, out compressedWritten, false, 0, 0, 0, 0);
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
			throw new Exception("Not implemented");
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

	private static int SamplesPerSec() {
		int rate = g_VoiceSampleFormat_SamplesPerSec;
		EngineTool.OverrideSampleRate(ref rate);
		return rate;
	}

	private static int AvgBytesPerSec() {
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
		if (!voice_enable.GetBool())
			return;

		ReadOnlySpan<char> voiceCodec;
		if (sv_use_steam_voice.GetBool())
			voiceCodec = "steam";
		else
			throw new Exception("Non-steam voice codec, todo");

		if (!InitWithDefault(voiceCodec))
			InitWithDefault(VOICE_FALLBACK_CODEC);

	}

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
				VoiceTweakSpeakingVolume = Math.Max((int)Math.Abs(shortData[i]), VoiceTweakSpeakingVolume);

			VoiceTweakSpeakingVolume &= 0xFE00;
		}

		// TODO: channel.AutoGain.ProcessSamples(reinterpret<byte, short>(decompressed), nDecompressed);

		channel.LastFraction = UpsampleIntoBuffer(reinterpret<byte, short>(decompressed),
													   nDecompressed,
													   channel.Buffer,
													   channel.LastFraction,
													   (double)Voice.SamplesPerSec() / g_VoiceSampleFormat_SamplesPerSec);
		channel.LastSample = decompressed[nDecompressed];

		VoiceWriter.AddDecompressedData(channel, decompressed[..(nDecompressed * 2)]);

		if (voice_showincoming.GetInt() != 0)
			Msg("Voice - %d incoming samples added to channel %d\n", nDecompressed, nChannel);

		return nChannel;
	}
	public static double UpsampleIntoBuffer(ReadOnlySpan<short> src, int srcSamples, SizedCircularBuffer<byte> buffer, double startFraction, double rate) {
		double maxFraction = srcSamples - 1;

		while (true) {
			if (startFraction >= maxFraction)
				break;

			int sample = (int)startFraction;
			double frac = startFraction - Math.Floor(startFraction);

			double val1 = src[sample];
			double val2 = src[sample + 1];
			short newSample = (short)(val1 + (val2 - val1) * frac);
			buffer.PushFront(new ReadOnlySpan<short>(in newSample).Cast<short, byte>());

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
	internal static void LocalPlayerTalkingAck() {
		if (!bLocalPlayerTalkingAck)
			g_SoundServices.OnChangeVoiceStatus(-2, true);
		bLocalPlayerTalkingAck = true;
	}
}
