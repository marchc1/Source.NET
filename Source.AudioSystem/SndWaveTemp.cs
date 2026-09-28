global using static Source.AudioSystem.SndWaveTemp;

using Source.Common;
using Source.Common.Commands;
using Source.Common.Filesystem;

using System.Buffers.Binary;

using static Source.Common.Formats.RiffConstants;

namespace Source.AudioSystem;

//-----------------------------------------------------------------------------
// Purpose: Create an output wave stream.  Used to record audio for in-engine movies or
// mixer debugging.
//-----------------------------------------------------------------------------
public static class SndWaveTemp
{
	const int PCMWAVEFORMAT_SIZE = 16;

	static string TmpFileName(ReadOnlySpan<char> filename) {
		Span<char> tmpfilename = stackalloc char[MAX_PATH];
		StrTools.StripExtension(filename, tmpfilename);
		StrTools.DefaultExtension(tmpfilename, ".WAV");
		return new(tmpfilename.SliceNullTerminatedString());
	}

	// Create a wave file
	public static void WaveCreateTmpFile(ReadOnlySpan<char> filename, int rate, int bits, int channels) {
		string tmpfilename = TmpFileName(filename);

		IFileHandle? file = filesystem.Open(tmpfilename, FileOpenOptions.Write | FileOpenOptions.Binary);
		if (file == null)
			return;

		using (file) {
			Span<byte> header = stackalloc byte[12 + 8 + PCMWAVEFORMAT_SIZE + 8];
			BinaryPrimitives.WriteUInt32LittleEndian(header, RIFF_ID);
			BinaryPrimitives.WriteInt32LittleEndian(header[4..], 0);

			BinaryPrimitives.WriteUInt32LittleEndian(header[8..], RIFF_WAVE);

			// create a 16-bit PCM stereo output file
			BinaryPrimitives.WriteUInt32LittleEndian(header[12..], WAVE_FMT);
			BinaryPrimitives.WriteInt32LittleEndian(header[16..], PCMWAVEFORMAT_SIZE);
			BinaryPrimitives.WriteInt16LittleEndian(header[20..], (short)WAVE_FORMAT_PCM);
			BinaryPrimitives.WriteInt16LittleEndian(header[22..], (short)channels);
			BinaryPrimitives.WriteInt32LittleEndian(header[24..], rate);
			BinaryPrimitives.WriteInt32LittleEndian(header[28..], rate * bits * channels / 8);
			BinaryPrimitives.WriteInt16LittleEndian(header[32..], (short)(2 * channels));
			BinaryPrimitives.WriteInt16LittleEndian(header[34..], (short)bits);

			BinaryPrimitives.WriteUInt32LittleEndian(header[36..], WAVE_DATA);
			BinaryPrimitives.WriteInt32LittleEndian(header[40..], 0);

			file.Stream.Write(header);
		}
	}

	public static void WaveAppendTmpFile(ReadOnlySpan<char> filename, ReadOnlySpan<byte> buffer, int sampleBits, int numSamples) {
		string tmpfilename = TmpFileName(filename);

		IFileHandle? file = filesystem.Open(tmpfilename, FileOpenOptions.Append | FileOpenOptions.Binary);
		if (file == null)
			return;

		using (file) {
			file.Stream.Seek(0, SeekOrigin.End);
			file.Stream.Write(buffer[..(numSamples * sampleBits / 8)]);
		}
	}

	public static void WaveFixupTmpFile(ReadOnlySpan<char> filename) {
		string tmpfilename = TmpFileName(filename);

		IFileHandle? file = filesystem.Open(tmpfilename, FileOpenOptions.Append | FileOpenOptions.Binary);
		if (file == null) {
			Warning($"WaveFixupTmpFile( '{tmpfilename}' ) failed to open file for editing\n");
			return;
		}

		using (file) {
			// file size goes in RIFF chunk
			int size = (int)file.Stream.Length - 2 * sizeof(int);
			// offset to data chunk
			int headerSize = sizeof(int) * 5 + PCMWAVEFORMAT_SIZE;
			// size of data chunk
			int dataSize = size - headerSize;

			Span<byte> value = stackalloc byte[4];
			BinaryPrimitives.WriteInt32LittleEndian(value, size);
			file.Stream.Seek(sizeof(int), SeekOrigin.Begin);
			file.Stream.Write(value);

			// skip the header and the 4-byte chunk tag and write the size
			BinaryPrimitives.WriteInt32LittleEndian(value, dataSize);
			file.Stream.Seek(headerSize + sizeof(int), SeekOrigin.Begin);
			file.Stream.Write(value);
		}
	}

	[ConCommand(helpText: "Fixup corrupted .wav file if engine crashed during startmovie/endmovie, etc.")]
	static void movie_fixwave(in TokenizedCommand args) {
		if (args.ArgC() != 2) {
			Msg("Usage: movie_fixwave wavname\n");
			return;
		}

		ReadOnlySpan<char> wavname = args.Arg(1);
		if (!filesystem.FileExists(wavname)) {
			Warning($"movie_fixwave: File '{wavname}' does not exist\n");
			return;
		}

		Span<char> tmpfilename = stackalloc char[256];
		StrTools.StripExtension(wavname, tmpfilename);
		strcat(tmpfilename, "_fixed");
		StrTools.DefaultExtension(tmpfilename, ".wav");
		string fixedName = new(tmpfilename.SliceNullTerminatedString());

		// Now copy the file
		Msg($"Copying '{wavname}' to '{fixedName}'\n");
		IFileHandle? src = filesystem.Open(wavname, FileOpenOptions.Read | FileOpenOptions.Binary);
		IFileHandle? dst = filesystem.Open(fixedName, FileOpenOptions.Write | FileOpenOptions.Binary);
		if (src != null && dst != null)
			src.Stream.CopyTo(dst.Stream);
		src?.Dispose();
		dst?.Dispose();

		Msg($"Performing fixup on '{fixedName}'\n");
		WaveFixupTmpFile(fixedName);
	}
}
