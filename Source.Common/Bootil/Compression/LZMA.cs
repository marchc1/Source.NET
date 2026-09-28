using CommunityToolkit.HighPerformance;

using SevenZip;

namespace Bootil.Compression;

public delegate void ProgressCallback();

public static class LZMA
{
	static readonly SevenZip.Compression.LZMA.Encoder encoder = new();
	static readonly SevenZip.Compression.LZMA.Decoder decoder = new();
	static readonly CoderPropID[] encodePropertyKeys = new CoderPropID[7];
	static readonly object[] encodePropertyValues = new object[7];
	static readonly byte[] decodeProperties = new byte[LZMA_PROPS_SIZE];

	const int LZMA_PROPS_SIZE = 5;

	static void ConfigureEncoder(int dictSize) {
		encodePropertyKeys[0] = CoderPropID.DictionarySize; encodePropertyValues[0] = dictSize;
		encodePropertyKeys[1] = CoderPropID.LitContextBits; encodePropertyValues[1] = 3;
		encodePropertyKeys[2] = CoderPropID.LitPosBits; encodePropertyValues[2] = 0;
		encodePropertyKeys[3] = CoderPropID.PosStateBits; encodePropertyValues[3] = 2;
		encodePropertyKeys[4] = CoderPropID.NumFastBytes; encodePropertyValues[4] = 32;
		encodePropertyKeys[5] = CoderPropID.Algorithm; encodePropertyValues[5] = 2;
		encodePropertyKeys[6] = CoderPropID.MatchFinder; encodePropertyValues[6] = "BT4";
		encoder.SetCoderProperties(encodePropertyKeys, encodePropertyValues);
	}

	static void WriteHeader(Stream output, int inputLength, int dictSize) {
		ConfigureEncoder(dictSize);

		encoder.WriteCoderProperties(output);

		Span<byte> sizeStart = stackalloc byte[8];
		sizeStart.Clear();
		sizeStart[0] = (byte)(inputLength & 255);
		sizeStart[1] = (byte)((inputLength >> 8) & 255);
		sizeStart[2] = (byte)((inputLength >> 16) & 255);
		sizeStart[3] = (byte)((inputLength >> 24) & 255);
		output.Write(sizeStart);
	}

	static long ReadUncompressedSize(ReadOnlySpan<byte> size8) {
		return size8[0]
			 | ((long)size8[1] << 8)
			 | ((long)size8[2] << 16)
			 | ((long)size8[3] << 24);
	}

	public static bool Compress(Stream inputData, Stream output, int level, int dictSize) {
		WriteHeader(output, (int)inputData.Length, dictSize);
		encoder.Code(inputData, output, inputData.Length, -1, null);
		return true;
	}

	public static bool Compress(ReadOnlySpan<byte> inputData, Stream output, int level, int dictSize) {
		WriteHeader(output, inputData.Length, dictSize);

		unsafe {
			fixed (byte* d = inputData)
				using (UnmanagedMemoryStream ms = new UnmanagedMemoryStream(d, inputData.Length))
					encoder.Code(ms, output, inputData.Length, -1, null);
		}

		return true;
	}

	public static bool Extract(Stream data, Stream output, ProgressCallback? progress = null) {
		if (data.Read(decodeProperties) != LZMA_PROPS_SIZE)
			return false;

		Span<byte> size = stackalloc byte[8];
		if (data.Read(size) != 8)
			return false;

		long outSize = ReadUncompressedSize(size);

		decoder.SetDecoderProperties(decodeProperties);
		decoder.Code(data, output, -1, outSize, null);
		return true;
	}

	public static bool Extract(ReadOnlySpan<byte> data, Stream output, ProgressCallback? progress = null) {
		if (data.Length < LZMA_PROPS_SIZE + 8)
			return false;

		data[..LZMA_PROPS_SIZE].CopyTo(decodeProperties);
		data = data[LZMA_PROPS_SIZE..];

		long outSize = ReadUncompressedSize(data[..8]);
		data = data[8..];

		decoder.SetDecoderProperties(decodeProperties);

		unsafe {
			fixed (byte* d = data)
				using (UnmanagedMemoryStream ms = new UnmanagedMemoryStream(d, data.Length))
					decoder.Code(ms, output, data.Length, outSize, null);
		}

		return true;
	}

	public static bool Extract(Stream data, Span<byte> output, ProgressCallback? progress = null) {
		if (data.Read(decodeProperties) != LZMA_PROPS_SIZE)
			return false;

		Span<byte> size = stackalloc byte[8];
		if (data.Read(size) != 8)
			return false;

		long outSize = ReadUncompressedSize(size);

		decoder.SetDecoderProperties(decodeProperties);

		unsafe {
			fixed (byte* od = output)
				using (UnmanagedMemoryStream oms = new UnmanagedMemoryStream(od, output.Length, output.Length, FileAccess.Write))
					decoder.Code(data, oms, -1, outSize, null);
		}

		return true;
	}

	public static bool Extract(ReadOnlySpan<byte> data, Span<byte> output, ProgressCallback? progress = null) {
		if (data.Length < LZMA_PROPS_SIZE + 8)
			return false;

		data[..LZMA_PROPS_SIZE].CopyTo(decodeProperties);
		data = data[LZMA_PROPS_SIZE..];

		long outSize = ReadUncompressedSize(data[..8]);
		data = data[8..];

		decoder.SetDecoderProperties(decodeProperties);

		unsafe {
			fixed (byte* id = data)
			fixed (byte* od = output)
				using (UnmanagedMemoryStream ims = new UnmanagedMemoryStream(id, data.Length))
				using (UnmanagedMemoryStream oms = new UnmanagedMemoryStream(od, output.Length, output.Length, FileAccess.Write))
					decoder.Code(ims, oms, data.Length, outSize, null);
		}

		return true;
	}

	public interface IProgress
	{
		bool OnProgress(float progress);
	}

	class CodeProgress(IProgress progress, long outSize) : ICodeProgress
	{
		public void SetProgress(long inSize, long outProcessed) => progress.OnProgress((float)outProcessed / (float)outSize);
	}

	public static bool Extract(ReadOnlySpan<byte> data, ReadOnlySpan<char> outFile, IProgress? progress) {
		if (data.Length <= 0xd || data[0] >= 0xe1 || BitConverter.ToUInt32(data[1..5]) < 0x1000)
			return false;

		uint outSize = BitConverter.ToUInt32(data[5..9]);

		SevenZip.Compression.LZMA.Decoder decoder = new();
		decoder.SetDecoderProperties(data[..LZMA_PROPS_SIZE].ToArray());

		FileStream output;
		try {
			output = new FileStream(new string(outFile), FileMode.Create, FileAccess.Write);
		}
		catch {
			return false;
		}

		using (output) {
			data = data[0xd..];
			try {
				unsafe {
					fixed (byte* d = data)
						using (UnmanagedMemoryStream ms = new UnmanagedMemoryStream(d, data.Length))
							decoder.Code(ms, output, data.Length, outSize, progress == null ? null : new CodeProgress(progress, outSize));
				}
			}
			catch {
				return false;
			}

			return output.Length >= outSize;
		}
	}

	public class ExtractionThread : IProgress
	{
		readonly Lock mutex = new();
		readonly string outFile;
		byte[]? buffer;
		bool done;
		bool success;
		float progress;

		public ExtractionThread(ReadOnlySpan<byte> data, ReadOnlySpan<char> outFile) {
			buffer = data.ToArray();
			this.outFile = new(outFile);
			done = false;
			success = false;
			progress = 0;
			new Thread(Run) { IsBackground = true }.Start();
		}

		void Run() {
			bool result = Extract(buffer, outFile, this);
			buffer = null;
			lock (mutex) {
				done = true;
				success = result;
			}
		}

		public bool IsDone() {
			lock (mutex)
				return done;
		}

		public bool Success() {
			lock (mutex)
				return success;
		}

		public float GetProgress() {
			lock (mutex)
				return progress;
		}

		public bool OnProgress(float progress) {
			lock (mutex)
				this.progress = progress;
			return true;
		}
	}
}
