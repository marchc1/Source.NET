global using static Source.Engine.VoiceGlobals;

using Source.Common;
using Source.Common.Audio;
using Source.Common.Commands;
using Source.Common.Utilities;

using Steamworks;

using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;

namespace Source.Engine;

public static class VoiceGlobals
{
	public const int VOICE_OUTPUT_SAMPLE_RATE_LOW = 11025;  // Sample rate that we feed to the mixer.
	public const int VOICE_OUTPUT_SAMPLE_RATE_HIGH = 22050; // Sample rate that we feed to the mixer.
	public const int VOICE_OUTPUT_SAMPLE_RATE_MAX = 22050;  // Sample rate that we feed to the mixer.
	public const int BYTES_PER_SAMPLE = 2;

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

	public void AddDecompressedData() {

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

	static readonly VoiceWriter VoiceWriter = new();

	public static IVoiceRecord? VoiceRecord = null;
	public static IVoiceCodec? EncodeCodec = null;
	public static string? VoiceCodec = null;

	public static bool VoiceAtLeastPartiallyInitted = false;
	public static bool InTweakMode = false;
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

	private static void Deinit() {
		throw new NotImplementedException();
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
		// SoundServices->OnChangeVoiceStatus(-1, false);       // Tell the client DLL.

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
					// TODO: g_pSoundServices->OnChangeVoiceStatus(-3, true);
				}
				else {
					if (result == EVoiceResult.k_EVoiceResultNotRecording && Voice.VoiceRecording)
						Voice.RecordStop();

					// TODO: g_pSoundServices->OnChangeVoiceStatus(-3, false);
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
}
