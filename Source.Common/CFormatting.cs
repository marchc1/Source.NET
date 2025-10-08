using Steamworks;

using System.Formats.Asn1;
using System.Globalization;
using System.Runtime.CompilerServices;

namespace Source.Common;
public enum VariableType
{
	NotApplicable = '\0',
	SignedDecimalInteger = 'i',
	UnsignedDecimalInteger = 'u',
	UnsignedOctal = 'o',
	UnsignedHexadecimalInteger = 'x',
	DecimalFloatingPoint = 'f',
	ScientificNotation = 'e',
	ShortestRepresentation = 'g',
	HexadecimalFloatingPoint = 'a',
	Character = 'c',
	StringOfCharacters = 's',
	PointerAddress = 'p',
	Nothing = 'n'
}

public ref struct SpanWriter<T>
{
	Span<T> input;
	int writePtr;
	bool overflowed;
	public SpanWriter(Span<T> input) {
		this.input = input;
	}

	public SpanWriter<T> Write(in T value) {
		if (writePtr >= input.Length)
			return this;
		input[writePtr++] = value;
		return this;
	}
	public SpanWriter<T> Write(ReadOnlySpan<T> span) {
		if ((writePtr + span.Length) >= input.Length) {
			// We'll clamped-copy it anyway, but set overflow flag
			overflowed = true;
		}
		writePtr += span.ClampedCopyTo(input[writePtr..]);
		return this;
	}

	public readonly int Length => writePtr;
	public readonly bool Overflowed => overflowed;

	public Span<T> ToSpan() => input[..writePtr];
	public static implicit operator Span<T>(SpanWriter<T> self) => self.input[..self.writePtr];
}

public ref struct PrintF
{
	SpanWriter<char> input;
	CFormatReader reader;
	public PrintF(Span<char> input, ReadOnlySpan<char> format) {
		this.input = new(input);
		this.reader = new(format);
		// This gets called to flush any literals so we're immediately at a variable
		// (and any variable writes will do the same)
		WriteAnyLiterals();
	}

	private unsafe void WriteAnyLiterals() {
		Span<char> literalBufferTarget = stackalloc char[64];
		while (!reader.Overflowed()) {
			int read = reader.ReadLiteral(literalBufferTarget);
			if (read > 0) {
				input.Write(literalBufferTarget[..read]);
				continue;
			}

			break;
		}
	}

	public readonly int Length => input.Length;
	public readonly bool Overflowed => input.Overflowed;
	public Span<char> ToSpan() => input.ToSpan();

	public static implicit operator Span<char>(PrintF self) => self.input;
	public static implicit operator int(PrintF self) => self.input.Length;

	public unsafe PrintF D(int i) {
		Span<char> buffer = stackalloc char[11];
		if (i.TryFormat(buffer, out int written))
			input.Write(buffer);

		WriteAnyLiterals();
		return this;
	}
	[MethodImpl(MethodImplOptions.AggressiveInlining)]public unsafe PrintF I(int i) => D(i);
	public PrintF S(ReadOnlySpan<char> str) {
		if (reader.ReadVariable(out char type, out int variableIdx)) {
			input.Write(str);
			WriteAnyLiterals();
		}
		return this;
	}
}


public ref struct ScanF
{
	public CFormatReader Format;
	ReadOnlySpan<char> input;
	int readArguments = 0;
	public readonly int ReadArguments => readArguments;
	public readonly bool ReadAny => readArguments > 0;

	public ScanF(ReadOnlySpan<char> input, ReadOnlySpan<char> format) {
		Format = new(format);
		this.input = input;
	}
	// NOTE: Only unsafe because C# can't tell we're never going to lose track of the stack allocated buffer target
	// (which is needed to read out a literal)
	private unsafe bool TryReadVariable(Span<char> incoming, out char type, out int len) {
		type = '\0';
		len = 0;
		if (Format.Overflowed() || input.Length <= 0)
			return false;

		// Read until a variable or overflow
		Span<char> literalBufferTarget = stackalloc char[64];
		while (!Format.Overflowed() && input.Length > 0) {
			int read = Format.ReadLiteral(literalBufferTarget);
			if (read > 0) {
				input = input[read..]; // slice so we're aligned
				continue;
			}

			break;
		}

		if (Format.ReadVariable(out type, out int variableIdx)) {
			readArguments = variableIdx + 1;
			int writePtr = 0;
			bool hasWarned = false;
			while (input.Length > 0) {
				char c = input[0];
				if (c == ' ')
					break;
				input = input[1..];
				if (writePtr < incoming.Length) // don't overflow but continue to read to not break further arguments
					incoming[writePtr++] = c;
				else if (!hasWarned) {
					Warning($"Overflowed scanf (max: {incoming.Length}). Consider increasing max here! Truncated argument will be provided.\n");
					hasWarned = true;
				}
			}
			len = writePtr;
			return true;
		}

		return false;
	}
	public const int DEFAULT_SCANF_MAX = 256;
	public unsafe ScanF Read(out int i, int max = DEFAULT_SCANF_MAX) {
		Span<char> incoming = stackalloc char[max];
		i = default;
		if (TryReadVariable(incoming, out char type, out int len))
			switch (type) {
				case 'd':
				case (char)VariableType.SignedDecimalInteger:
					i = int.TryParse(incoming[..len], out int iAttempt) ? iAttempt : default;
					break;
				default: throw new NotImplementedException();
			}

		return this;
	}
}

public ref struct CFormatReader
{
	int formatReader = 0;
	bool haltedAtVariable;
	int variableIdx;
	readonly ReadOnlySpan<char> format;

	public CFormatReader(ReadOnlySpan<char> format) {
		this.format = format;
	}

	public bool Overflowed() => formatReader >= format.Length || format[formatReader] == '\0';

	/// <summary>
	/// Reads one literal into a read target buffer.
	/// </summary>
	/// <param name="readTarget">A target buffer</param>
	/// <returns>How much was written to the target. >0 if any literal was read at all. 0 if a halt break occured due to an incoming variable or end-of-stream.</returns>
	public int ReadLiteral(Span<char> readTarget) {
		if (haltedAtVariable)
			return 0;

		int chars = 0;
		for (int i = 0; i < readTarget.Length; i++) {
			if (Overflowed())
				return i;

			char c = format[formatReader];
			if (c == '%') {
				i++;
				if (format[formatReader + 1] != '%') {
					// We have to stop, a variable was reached
					formatReader++;
					haltedAtVariable = true;
					break; // We wrote something if i > 0, even if we guarded here
				}
			}
			// Otherwise, write
			readTarget[chars++] = c;
			formatReader++;
		}

		return chars;
	}
	/// <summary>
	/// Tries to read one variable type. ReadLiteral must have triggered a halt before this function will return true.
	/// </summary>
	/// <param name="type">Out variable type</param>
	/// <param name="variableIdx">Out variable index</param>
	/// <returns>True if a variable could be read. False if a halt break hasn't occured, or if the format pointer overflowed</returns>
	public bool ReadVariable(out char type, out int variableIdx) {
		type = (char)VariableType.NotApplicable;
		variableIdx = -1;
		if (!haltedAtVariable)
			return false;
		if (Overflowed())
			return false;

		type = char.ToLowerInvariant(format[formatReader++]);
		variableIdx = this.variableIdx;
		this.variableIdx++;
		haltedAtVariable = false;
		return true;
	}
}

public static class CFormatUtils
{
	public static int WriteFormattedObject(Span<char> writeBuffer, char type, object? arg) {
		int writeSize = 0;
		bool useWriteBuffer = false;

		VariableType varType;
		if (type == 'd')
			varType = VariableType.SignedDecimalInteger;
		else
			varType = (VariableType)type;

		switch (arg) {
			case int v when varType == VariableType.SignedDecimalInteger:
				v.TryFormat(writeBuffer, out writeSize);
				useWriteBuffer = true;
				break;
			case uint v when varType == VariableType.UnsignedDecimalInteger:
				v.TryFormat(writeBuffer, out writeSize);
				useWriteBuffer = true;
				break;
			case uint v when varType == VariableType.UnsignedOctal:
				string uoctal = Convert.ToString(v, 8); // ugh
				uoctal.CopyTo(writeBuffer);
				writeSize = uoctal.Length;
				break;
			case uint v when varType == VariableType.UnsignedHexadecimalInteger:
				v.TryFormat(writeBuffer, out writeSize, "x");
				useWriteBuffer = true;
				break;
			case double v when varType == VariableType.DecimalFloatingPoint:
				v.TryFormat(writeBuffer, out writeSize, "F");
				useWriteBuffer = true;
				break;
			case double v when varType == VariableType.ScientificNotation:
				v.TryFormat(writeBuffer, out writeSize, "E");
				useWriteBuffer = true;
				break;
			case double v when varType == VariableType.ShortestRepresentation:
				v.TryFormat(writeBuffer, out writeSize, "G");
				useWriteBuffer = true;
				break;
			case double v when varType == VariableType.HexadecimalFloatingPoint:
				var bits = BitConverter.DoubleToInt64Bits(v);
				writeSize = bits.TryFormat(writeBuffer, out var written, "X") ? written : 0;
				useWriteBuffer = true;
				break;
			case char v when varType == VariableType.Character:
				writeBuffer[0] = v;
				writeSize = 1;
				useWriteBuffer = true;
				break;
			case string v when varType == VariableType.StringOfCharacters:
				v.AsSpan().CopyTo(writeBuffer);
				writeSize = v.Length;
				useWriteBuffer = true;
				break;
			case object v when varType == VariableType.PointerAddress:
				var ptrStr = $"0x{v.GetHashCode():X}";
				ptrStr.AsSpan().CopyTo(writeBuffer);
				writeSize = ptrStr.Length;
				useWriteBuffer = true;
				break;
			case object _ when varType == VariableType.Nothing:
				useWriteBuffer = false;
				break;
			default:
				useWriteBuffer = false;
				break;
		}
		return writeSize;
	}
}

public static class CFormatting
{
	/// <summary>
	/// Performs the following:
	/// <br/>
	/// 1. Determines the null terminator of <paramref name="str"/>
	/// <br/>
	/// 2. Determines the maximum length (<paramref name="target"/>.Length, <paramref name="str"/>.Length)
	/// <br/>
	/// 3. Copies str[..maxLen] to target fully
	/// <br/>
	/// 4. Writes '\0' at target[maxLen]
	/// <br/>
	/// 5. Returns maxLen
	/// </summary>
	/// <param name="target"></param>
	/// <param name="str"></param>
	/// <returns></returns>
	public static float strtof(ReadOnlySpan<char> input, out ReadOnlySpan<char> output) {
		Span<char> outputBuffer = stackalloc char[input.Length];
		int i = 0;
		while (input[i] switch { '0' or '1' or '2' or '3' or '4' or '5' or '6' or '7' or '8' or '9' or '.' => true, _ => false }) {
			outputBuffer[i] = input[i];
			i++;
		}
		if (float.TryParse(outputBuffer[..i], NumberStyles.Float, CultureInfo.InvariantCulture, out float ret)) {
			output = input[i..];
			return ret;
		}
		output = input;
		return 0;
	}
	public static int strcpy(Span<char> target, ReadOnlySpan<char> str) {
		str = str.SliceNullTerminatedString();
		int len = Math.Min(target.Length, str.Length);
		str[..len].CopyTo(target);
		if (target.Length < len)
			target[len] = '\0';
		return len;
	}
	// This needs to go in the future, but Dbg currently relies on it.
	public static unsafe int sprintf(Span<char> target, ref CFormatReader reader, params object?[] args) {
		int originalSize = target.Length;

		Span<char> buffer = stackalloc char[256];
		while (!reader.Overflowed()) {
			// Try reading literal
			Span<char> read = buffer[0..reader.ReadLiteral(buffer)];
			if (read.Length > 0) {
				read.CopyTo(target);
				target = target[read.Length..];
			}
			// Try reading a variable?
			else if (reader.ReadVariable(out char type, out int idx)) {
				if (idx < args.Length) {
					int writtenVar = CFormatUtils.WriteFormattedObject(target, type, args[idx]);
					target = target[writtenVar..];
				}
				else {
					// Skip it
					target[0] = '%';
					target[1] = type;
					target = target[2..];
				}
			}
			else {
				if (!reader.Overflowed())
					throw new FormatException("Unexpected non-overflow, non-variable, and non-literal condition met...");
			}
		}

		return originalSize - target.Length; // Should return the delta length
	}
	public static PrintF sprintf(Span<char> target, ReadOnlySpan<char> format) => new(target, format);
}
