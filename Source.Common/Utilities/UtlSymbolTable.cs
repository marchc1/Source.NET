using System.Collections.Concurrent;


namespace Source.Common.Utilities;

public interface ISymbolTable {
	UtlSymId_t AddString(ReadOnlySpan<char> str);
	UtlSymId_t Find(ReadOnlySpan<char> str);
	string? String(UtlSymId_t symbol);
	nint GetNumStrings();
	void RemoveAll();
}

public class UtlSymbolTable(bool caseInsensitive = false) : ISymbolTable
{
	readonly Dictionary<UtlSymId_t, string> Symbols = [];

	public int Count => Symbols.Count;
	public void Clear() => Symbols.Clear();

	public UtlSymId_t AddString(ReadOnlySpan<char> str) {
		UtlSymId_t hash = str.Hash(invariant: caseInsensitive);
		if (!Symbols.ContainsKey(hash))
			Symbols[hash] = new(str);
		return hash;
	}

	public UtlSymId_t Find(ReadOnlySpan<char> str) {
		UtlSymId_t hash = str.Hash(invariant: caseInsensitive);
		if (Symbols.ContainsKey(hash))
			return hash;
		return UTL_INVAL_SYMBOL;
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

	public UtlSymId_t AddString(ReadOnlySpan<char> str) {
		UtlSymId_t hash = str.Hash(invariant: caseInsensitive);
		if (!Symbols.ContainsKey(hash))
			Symbols[hash] = new(str);
		return hash;
	}

	public UtlSymId_t Find(ReadOnlySpan<char> str) {
		UtlSymId_t hash = str.Hash(invariant: caseInsensitive);
		if (Symbols.ContainsKey(hash))
			return hash;
		return 0;
	}

	public string? String(UtlSymId_t symbol) {
		if (Symbols.TryGetValue(symbol, out string? str))
			return str;
		return null;
	}
	public nint GetNumStrings() => Symbols.Count;
	public void RemoveAll() => Symbols.Clear();
}
