namespace Source.Common.Engine;

public delegate void StringChangedDelegate(
	object? context,
	INetworkStringTable stringTable,
	int stringNumber,
	ReadOnlySpan<char> newString,
	ReadOnlySpan<byte> newData
);

public delegate void FullUpdateCallback(
	INetworkStringTable stringTable
);

public interface INetworkStringTable
{
	public const int INVALID_STRING_TABLE = -1;
	public const ushort INVALID_STRING_INDEX = ushort.MaxValue;
	public const uint MAX_TABLES = 32;

	ReadOnlySpan<char> GetTableName();
	int GetTableId();
	int GetNumStrings();
	int GetMaxStrings();
	int GetEntryBits();

	void SetTick(int tick);
	bool ChangedSinceTick(int tick);

	int AddString(bool isServer, ReadOnlySpan<char> value, int length = -1, ReadOnlySpan<byte> userData = default);

	ReadOnlySpan<char> GetString(int stringNumber);
	void SetStringUserData(int stringNumber, int length, ReadOnlySpan<byte> userData);
	byte[]? GetStringUserData(int stringNumber);
	int FindStringIndex(ReadOnlySpan<char> value);

	void SetStringChangedCallback(object? context, StringChangedDelegate callback);
	void SetFullUpdateCallback(FullUpdateCallback changeFunc);
	StringChangedDelegate? GetCallback();
	FullUpdateCallback? GetFullUpdateCallback();

	void DeleteAllStrings(bool reNetwork = false);
}

public interface INetworkStringTableContainer
{
	INetworkStringTable? CreateStringTable(
		ReadOnlySpan<char> tableName,
		int maxEntries,
		int userDataFixedSize = 0,
		int userDataNetworkBits = 0
	);
	INetworkStringTable? CreateStringTableEx(
		ReadOnlySpan<char> tableName,
		int maxEntries,
		int userDataFixedSize = 0,
		int userDataNetworkBits = 0,
		bool isFilenames = false
	);
	void RemoveAllTables();
	INetworkStringTable? FindTable(ReadOnlySpan<char> tableName);
	INetworkStringTable? GetTable(int tableId);
	int GetNumTables();
	void SetAllowClientSideAddString(INetworkStringTable table, bool allowClientSideAddString);
}
