using System.Diagnostics.CodeAnalysis;

namespace Source.Common.Commands;

public struct TokenizedCommand
{
	const int COMMAND_MAX_ARGC = 64;
	const int COMMAND_MAX_LENGTH = 512;

	public static int MaxCommandLength => COMMAND_MAX_LENGTH - 1;
	public static readonly CharacterSet DefaultBreakSet = new("{}()':");

	int argCount;
	int strlen;
	
	char[]? argSBuffer;
	Range[] ppArgs;

	/// <summary>
	/// How many arguments are in the tokenized command? Note that this also contains the command itself. So a command
	/// executed with no arguments will return 1 here, for example.
	/// </summary>
	/// <returns></returns>
	public readonly int ArgC() => argCount;
	/// <summary>
	/// The argument buffer past the provided argument.
	/// </summary>
	/// <returns>All text, as a <see cref="ReadOnlySpan{char}"/> slice of the internal command buffer, after the provided arguments starting position (0 returning all text, 1 returning all after the initial command, etc..)</returns>
	public readonly ReadOnlySpan<char> ArgS(int startingArg = 1) {
		// Null/overflow checking
		if (argSBuffer == null)
			return [];
		if (argCount <= startingArg)
			return [];

		// Start at the first argument requested, and end at the last argument in ppArgs
		Index startIdx = ppArgs[startingArg].Start;
		Index endIdx = ppArgs[argCount - 1].End;

		return argSBuffer.AsSpan()[startIdx..endIdx];
	}

	/// <summary>
	/// Gets a single index from the command, and attempts to convert it to a 32-bit integer.
	/// </summary>
	/// <param name="index">A zero-indexed argument, zero will return the command name, and one is the start of the command arguments.</param>
	public readonly int Arg(int index, int def = default) {
		if (int.TryParse(Arg(index), null, out int r))
			return r;
		return def;
	}

	/// <summary>
	/// Gets a single index from the command, and attempts to convert it to a float.
	/// </summary>
	/// <param name="index">A zero-indexed argument, zero will return the command name, and one is the start of the command arguments.</param>
	public readonly float Arg(int index, float def = default) {
		if (float.TryParse(Arg(index), null, out float r))
			return r;
		return def;
	}

	/// <summary>
	/// Gets a single index from the command, and attempts to convert it to a double.
	/// </summary>
	/// <param name="index">A zero-indexed argument, zero will return the command name, and one is the start of the command arguments.</param>
	public readonly double Arg(int index, double def = default) {
		if (int.TryParse(Arg(index), null, out int r))
			return r;
		return def;
	}

	/// <summary>
	/// Gets a single index from the command.
	/// </summary>
	/// <param name="index">A zero-indexed argument, zero will return the command name, and one is the start of the command arguments.</param>
	/// <returns></returns>
	public readonly ReadOnlySpan<char> Arg(int index) {
		if (argSBuffer == null)
			return [];

		if (index < 0 || index >= argCount)
			return [];

		Range range = ppArgs[index];
		int start = range.Start.Value, end = range.End.Value;

		if (start < 0)
			start = 0;
		if (end >= argSBuffer.Length)
			end = argSBuffer.Length - 1;

		return argSBuffer.AsSpan()[start..end];
	}

	public readonly ReadOnlySpan<char> GetCommandString() => argSBuffer;

	public readonly void CopyTo(Span<char> target) {
		ArgS(0).CopyTo(target);
	}

	[MemberNotNull(nameof(argSBuffer))]
	[MemberNotNull(nameof(ppArgs))]
	public void Reset() {
		argCount = 0;
		strlen = 0;
		argSBuffer ??= new char[COMMAND_MAX_LENGTH];
		ppArgs ??= new Range[COMMAND_MAX_ARGC];
		for (int i = 0; i < COMMAND_MAX_LENGTH; i++) {
			argSBuffer[i] = '\0';
		}
		for (int i = 0; i < ppArgs.Length; i++) {
			ppArgs[i] = new Range(0, 0);
		}
	}

	public readonly ReadOnlySpan<char> this[int index] {
		get => Arg(index);
	}


	public bool Tokenize(ReadOnlySpan<char> command, CharacterSet? breakSet = null) {
		Reset();

		breakSet ??= DefaultBreakSet;

		nint readPos = 0;

		command.CopyTo(argSBuffer.AsSpan()[..command.Length]);
		strlen = command.Length;

		nint readOffset = 0;

		StringReader bufParse = new StringReader(new(argSBuffer, 0, command.Length));
		int argvbuffersize = 0;
		Span<char> argvBuf = stackalloc char[COMMAND_MAX_LENGTH];

		while (bufParse.IsValid() && (argCount < COMMAND_MAX_ARGC) && (readOffset < COMMAND_MAX_LENGTH)) {
			int maxLen = COMMAND_MAX_LENGTH - argvbuffersize;
			bufParse.EatWhiteSpace();
			int start = bufParse.TellGet();
			if (start == -1)
				break;

			if (bufParse.PeekChar() == '\0')
				break;

			bool quoteStart = bufParse.PeekChar() == '"';
			if (quoteStart)
				start += 1;

			int size = bufParse.ParseToken(breakSet, argvBuf[..maxLen]);
			if (size < 0)
				break;

			if (maxLen == size) {
				Reset();
				return false;
			}

			while (size > 0 && argvBuf[size - 1] == '\0')
				size--;

			ppArgs[argCount++] = new(start, start + size);

			if (argCount >= COMMAND_MAX_ARGC)
				Dbg.Warning("CCommand::Tokenize: Encountered command which overflows the argument buffer.. Clamped!\n");

			argvbuffersize += size + 1;
			Dbg.Assert(argvbuffersize <= COMMAND_MAX_LENGTH);
		}

		return true;
	}
	public readonly ReadOnlySpan<char> FindArg(ReadOnlySpan<char> name) {
		for (int i = 1; i < argCount; i++) {
			if (Arg(i).Equals(name, StringComparison.OrdinalIgnoreCase))
				return (i + 1) < argCount ? Arg(i + 1) : "";
		}
		return null;
	}
}
