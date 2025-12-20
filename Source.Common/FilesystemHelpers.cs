using System;
using System.Collections.Generic;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Text;

namespace Source.Common;

public static class FilesystemHelpers
{
	public readonly static CharacterSet BreakSet = new("{}()");
	public readonly static CharacterSet BreakSetIncludingColons = new("{}()':");
	public static bool com_ignorecolons = false;
	public static ReadOnlySpan<byte> ParseFileInternal(ReadOnlySpan<byte> fileBytes, Span<char> tokenOut, out bool wasQuoted, CharacterSet? charSet = null, int maxTokenLen = 0) {
		tokenOut[0] = '\0';
		wasQuoted = false;

		if (fileBytes.IsEmpty)
			return default;

		CharacterSet breaks = charSet != null
			? charSet
			: (com_ignorecolons ? BreakSet : BreakSetIncludingColons);

		byte c;
		uint len = 0;

	skipwhite:
		// skip whitespace
		while (true) {
			if (fileBytes.IsEmpty)
				return default;

			c = fileBytes[0];
			if (c > ' ')
				break;

			if (c == 0)
				return default;

			fileBytes = fileBytes[1..];
		}

		// skip // comments
		if (c == '/' && fileBytes.Length > 1 && fileBytes[1] == '/') {
			while (!fileBytes.IsEmpty && fileBytes[0] != 0 && fileBytes[0] != '\n')
				fileBytes = fileBytes[1..];
			goto skipwhite;
		}

		// skip /* */ comments
		if (c == '/' && fileBytes.Length > 1 && fileBytes[1] == '*') {
			fileBytes = fileBytes[2..];

			while (!fileBytes.IsEmpty) {
				if (fileBytes.Length > 1 && fileBytes[0] == '*' && fileBytes[1] == '/') {
					fileBytes = fileBytes[2..];
					break;
				}

				fileBytes = fileBytes[1..];
			}

			goto skipwhite;
		}

		// handle quoted strings
		if (c == '"') {
			wasQuoted = true;
			fileBytes = fileBytes[1..];

			while (true) {
				if (fileBytes.IsEmpty)
					break;

				c = fileBytes[0];
				fileBytes = fileBytes[1..];

				if (c == '"' || c == 0) {
					tokenOut[(int)len] = '\0';
					return fileBytes;
				}

				tokenOut[(int)len] = (char)c;
				if (len < maxTokenLen - 1)
					len++;
			}
		}

		// parse single-character tokens
		if (breaks.Contains((char)c)) {
			tokenOut[(int)len] = (char)c;
			if (len < maxTokenLen - 1)
				len++;
			tokenOut[(int)len] = '\0';
			return fileBytes[1..];
		}

		// parse regular word
		do {
			tokenOut[(int)len] = (char)c;
			fileBytes = fileBytes[1..];
			if (len < maxTokenLen - 1)
				len++;

			if (fileBytes.IsEmpty)
				break;

			c = fileBytes[0];
			if (breaks.Contains((char)c))
				break;

		} while (c > 32);

		tokenOut[(int)len] = '\0';
		return fileBytes;
	}
}
