using System.Buffers.Binary;
using System.Runtime.CompilerServices;

namespace Source.Common.Formats;

public static class RiffConstants
{
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static uint MAKEID(char d, char c, char b, char a) => ((uint)a << 24) | ((uint)b << 16) | ((uint)c << 8) | (uint)d;

	public static readonly uint RIFF_ID = MAKEID('R', 'I', 'F', 'F');
	public static readonly uint RIFF_WAVE = MAKEID('W', 'A', 'V', 'E');
	public static readonly uint WAVE_FMT = MAKEID('f', 'm', 't', ' ');
	public static readonly uint WAVE_DATA = MAKEID('d', 'a', 't', 'a');
	public static readonly uint WAVE_FACT = MAKEID('f', 'a', 'c', 't');
	public static readonly uint WAVE_CUE = MAKEID('c', 'u', 'e', ' ');
	public static readonly uint WAVE_SAMPLER = MAKEID('s', 'm', 'p', 'l');
	public static readonly uint WAVE_VALVEDATA = MAKEID('V', 'D', 'A', 'T');
	public static readonly uint WAVE_PADD = MAKEID('P', 'A', 'D', 'D');
	public static readonly uint WAVE_LIST = MAKEID('L', 'I', 'S', 'T');

	public const int WAVE_FORMAT_PCM = 0x0001;
	public const int WAVE_FORMAT_ADPCM = 0x0002;
	public const int WAVE_FORMAT_XMA = 0x0165;
}

public interface IFileReadBinary
{
	object? Open(ReadOnlySpan<char> fileName);
	int Read(Span<byte> output, object? file);
	void Close(object? file);
	void Seek(object? file, int pos);
	uint Tell(object? file);
	uint Size(object? file);
}

public class InFileRIFF : IDisposable
{
	readonly IFileReadBinary io;
	object? file;
	uint riffName;
	uint riffSize;

	public InFileRIFF(ReadOnlySpan<char> fileName, IFileReadBinary io) {
		this.io = io;
		file = io.Open(fileName);

		int riff = 0;
		if (file == null) {
			riffSize = 0;
			riffName = 0;
			return;
		}

		riff = ReadInt();
		if (riff != (int)RiffConstants.RIFF_ID) {
			Console.WriteLine($"Not a RIFF File [{fileName}]");
			riffSize = 0;
		}
		else {
			// we store size as size of all chunks
			// subtract off the RIFF form type (e.g. 'WAVE', 4 bytes)
			riffSize = (uint)(ReadInt() - 4);
			riffName = (uint)ReadInt();

			// HACKHACK: LWV files don't obey the RIFF format!!!
			// Do this or miss the linguistic chunks at the end. Lame!
			// subtract off 12 bytes for (RIFF, size, WAVE)
			riffSize = io.Size(file) - 12;
		}
	}

	public void Dispose() {
		io.Close(file);
		file = null;
		GC.SuppressFinalize(this);
	}

	public uint RIFFName() => riffName;
	public uint RIFFSize() => riffSize;
	public bool IsValid() => file != null;

	public int ReadInt() {
		Span<byte> tmp = stackalloc byte[4];
		tmp.Clear();
		io.Read(tmp, file);
		return BinaryPrimitives.ReadInt32LittleEndian(tmp);
	}

	public int ReadData(Span<byte> output) {
		int count = io.Read(output, file);

		return count;
	}

	public int PositionGet() {
		return (int)io.Tell(file);
	}

	public void PositionSet(int position) {
		io.Seek(file, position);
	}
}

public class IterateRIFF
{
	readonly InFileRIFF riff;
	int start;
	readonly int size;
	uint chunkName;
	int chunkSize;
	int chunkPosition;

	public IterateRIFF(InFileRIFF riff, int size) {
		this.riff = riff;
		this.size = size;

		if (riff.RIFFSize() == 0) {
			// bad file, just be an empty iterator
			ChunkClear();
			return;
		}

		// get the position and parse a chunk
		start = riff.PositionGet();
		ChunkSetup();
	}

	public IterateRIFF(IterateRIFF parent) {
		riff = parent.riff;
		size = (int)parent.ChunkSize();
		start = parent.ChunkFilePosition();
		ChunkSetup();
	}

	public int ChunkFilePosition() => chunkPosition;

	void ChunkSetup() {
		chunkPosition = riff.PositionGet();

		chunkName = (uint)riff.ReadInt();
		chunkSize = riff.ReadInt();
	}

	void ChunkClear() {
		chunkSize = -1;
	}

	public bool ChunkAvailable() {
		if (chunkSize != -1 && chunkSize < 0x10000000)
			return true;

		return false;
	}

	public bool ChunkNext() {
		if (!ChunkAvailable())
			return false;

		int nextPos = chunkPosition + 8 + chunkSize;

		// chunks are aligned
		nextPos += chunkSize & 1;

		if (nextPos >= (start + size)) {
			ChunkClear();
			return false;
		}

		riff.PositionSet(nextPos);

		ChunkSetup();
		return ChunkAvailable();

	}

	public uint ChunkName() => chunkName;

	public uint ChunkSize() => (uint)chunkSize;

	public int ChunkRead(Span<byte> output) {
		return riff.ReadData(output[..(int)ChunkSize()]);
	}

	public int ChunkReadPartial(Span<byte> output) {
		return riff.ReadData(output);
	}

	public int ChunkReadInt() {
		return riff.ReadInt();
	}
}
