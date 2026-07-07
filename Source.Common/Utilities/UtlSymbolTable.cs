using System;
using System.Collections.Concurrent;


namespace Source.Common.Utilities;

public interface ISymbolTable
{
	UtlSymbol AddString(ReadOnlySpan<char> str);
	UtlSymbol Find(ReadOnlySpan<char> str);
	string? String(UtlSymId_t symbol);
	nint GetNumStrings();
	void RemoveAll();
}

public class UtlSymbolTable(bool caseInsensitive = false) : ISymbolTable
{
	readonly Dictionary<UtlSymId_t, string> Symbols = [];

	public int Count => Symbols.Count;
	public void Clear() => Symbols.Clear();

	public unsafe UtlSymbol AddString(ReadOnlySpan<char> str) {
		str = str.SliceNullTerminatedString();

		ReadOnlySpan<char> hashme;
		if (caseInsensitive) {
			Span<char> lowerBuffer = stackalloc char[str.Length];
			str.ToLowerInvariant(lowerBuffer);
#pragma warning disable CS9080 // Use of variable in this context may expose referenced variables outside of their declaration scope
			hashme = lowerBuffer;
#pragma warning restore CS9080 // Use of variable in this context may expose referenced variables outside of their declaration scope
		}
		else
			hashme = str;

		UtlSymId_t hash = hashme.Hash();
		if (!Symbols.ContainsKey(hash))
			Symbols[hash] = new(hashme);
		return new(hash);
	}

	public UtlSymbol Find(ReadOnlySpan<char> str) {
		str = str.SliceNullTerminatedString();

		UtlSymId_t hash = str.Hash(invariant: caseInsensitive);
		if (Symbols.ContainsKey(hash))
			return new(hash);
		return new(UTL_INVAL_SYMBOL);
	}

	public string? String(UtlSymId_t symbol) {
		if (Symbols.TryGetValue(symbol, out string? str))
			return str;
		return null;
	}

	public virtual nint GetNumStrings() => Symbols.Count;
	public virtual void RemoveAll() => Symbols.Clear();
}

public class UtlSymbolTableMT(bool caseInsensitive = false) : ISymbolTable
{
	readonly ConcurrentDictionary<UtlSymId_t, string> Symbols = [];

	public UtlSymbol AddString(ReadOnlySpan<char> str) {
		str = str.SliceNullTerminatedString();

		UtlSymId_t hash = str.Hash(invariant: caseInsensitive);
		if (!Symbols.ContainsKey(hash))
			Symbols[hash] = new(str);
		return new(hash);
	}

	public UtlSymbol Find(ReadOnlySpan<char> str) {
		str = str.SliceNullTerminatedString();

		UtlSymId_t hash = str.Hash(invariant: caseInsensitive);
		if (Symbols.ContainsKey(hash))
			return new(hash);
		return new(UTL_INVAL_SYMBOL);
	}

	public string? String(UtlSymId_t symbol) {
		if (Symbols.TryGetValue(symbol, out string? str))
			return str;
		return null;
	}
	public nint GetNumStrings() => Symbols.Count;
	public void RemoveAll() => Symbols.Clear();
}
